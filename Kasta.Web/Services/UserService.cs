using System.ComponentModel;
using System.Diagnostics;
using CSharpFunctionalExtensions;
using Kasta.Data;
using Kasta.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kasta.Web.Services;

public partial class UserService
{
    private readonly UserManager<UserModel> _userManager;
    private readonly KastaDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<UserService> _logger;

    public UserService(IServiceProvider services, ILogger<UserService> logger)
    {
        _logger = logger;
        _userManager =  services.GetRequiredService<UserManager<UserModel>>();
        _db = services.GetRequiredService<KastaDbContext>();
        _httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();
    }

    public Task<UserModel?> GetCurrentUser() => GetCurrentUser(_httpContextAccessor.HttpContext);
    public async Task<UserModel?> GetCurrentUser(HttpContext? httpContext)
    {
        if (httpContext == null) return null;
        return await GetCurrentUserViaApiKey(httpContext)
            ?? await _userManager.GetUserAsync(httpContext.User);
    }

    public Task<UserApiKeyModel?> GetCurrentApiKey() => GetCurrentApiKey(_httpContextAccessor.HttpContext);
    public async Task<UserApiKeyModel?> GetCurrentApiKey(HttpContext? httpContext)
    {
        if (httpContext == null) return null;
        
        var apiKeyValue = httpContext.Request.Query
           .Where(e => string.Equals(e.Key, "apikey", StringComparison.OrdinalIgnoreCase))
           .Select(e => e.Value.ToString())
           .FirstOrDefault(e => !string.IsNullOrWhiteSpace(e));

        if (string.IsNullOrWhiteSpace(apiKeyValue)) apiKeyValue = null;

        // TODO Add support for "Authorization: Bearer <token>"
        if (httpContext.Request.Headers.Authorization.Count > 0)
        {
            Debugger.Break();
        }

        if (string.IsNullOrWhiteSpace(apiKeyValue)) return null;

        return await GetUserApiKeyByValue(
            apiKeyValue,
            new GetApiKeyOptions(
                IncludeUser: true,
                IncludeDeleted: false,
                RequireUserToExist: true,
                AsNoTracking: true));
    }

    #region GetUserApiKey
    public sealed record GetApiKeyOptions(
        bool IncludeUser = true,
        bool IncludeDeleted = false,
        bool RequireUserToExist = false,
        bool AsNoTracking = true);

    public async Task<UserApiKeyModel?> GetUserApiKeyById(
        string keyId,
        GetApiKeyOptions options)
    {
        return await Apply(options, _db.UserApiKeys.Where(e => e.Id == keyId))
           .FirstOrDefaultAsync();
    }
    
    public async Task<UserApiKeyModel?> GetUserApiKeyByValue(
        string value,
        GetApiKeyOptions options)
    {
        return await Apply(options, _db.UserApiKeys.Where(e => e.Token == value))
           .FirstOrDefaultAsync();
    }

    public async Task<bool> IsUserApiKeyValid(string apiKey)
    {
        return await _db.UserApiKeys
           .Where(e => e.Token == apiKey)
           .Where(e => !e.IsDeleted)
           .Where(e => e.User != null)
           .AnyAsync();
    }
    public async Task<UserModel?> GetUserForApiKey(string apiKey)
    {
        return await _db.UserApiKeys
           .Include(e => e.User)
           .Where(e => e.Token == apiKey && !e.IsDeleted && e.User != null)
           .Select(e => e.User)
           .FirstOrDefaultAsync();
    }

    private static IQueryable<UserApiKeyModel> Apply(GetApiKeyOptions options, IQueryable<UserApiKeyModel> q)
    {
        if (options.AsNoTracking) q = q.AsNoTracking();
        if (!options.IncludeDeleted) q = q.Where(e => !e.IsDeleted);
        if (options.IncludeUser) q = q.Include(e => e.User);
        if (options.RequireUserToExist) q = q.Where(e => e.User != null);
        return q;
    }
    #endregion

    #region GetCurrentUserViaApiKey
    private Task<UserModel?> GetCurrentUserViaApiKey() => GetCurrentUserViaApiKey(_httpContextAccessor.HttpContext);
    private async Task<UserModel?> GetCurrentUserViaApiKey(HttpContext? httpContext)
    {
        var apiKey = await GetCurrentApiKey(httpContext);
        if (apiKey == null) return null;

        await MarkApiKeyAsUsed(apiKey);
        return apiKey.User;
    }

    private async Task MarkApiKeyAsUsed(UserApiKeyModel apiKey)
    {
        await using var trans = await _db.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;
            await _db.UserApiKeys
                .Where(e => e.Id == apiKey.Id && !e.IsDeleted)
                .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.LastUsed, now));
            await _db.SaveChangesAsync();
            await trans.CommitAsync();

            var userEmail = await _db.Users.Where(e => e.Id == apiKey.UserId).Select(e => e.NormalizedUserName).FirstOrDefaultAsync();
            _logger.LogInformation("Used ApiKey {ApiKeyId} for User {Username} ({UserId})", apiKey.Id, userEmail, apiKey.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark Api Key {ApiKeyId} that it was used", apiKey.Id);
            await trans.RollbackAsync();
            throw;
        }
    }
    #endregion

    public async Task<bool> IsAuthorized(HttpContext? httpContext, bool allowApiKey = true)
    {
        httpContext ??= _httpContextAccessor.HttpContext;
        if (httpContext?.User.Identity?.IsAuthenticated != true) return false;

        var userModel = await _userManager.GetUserAsync(httpContext.User);
        if (userModel != null && !await _userManager.IsLockedOutAsync(userModel)) return true;
        
        return allowApiKey && await GetCurrentApiKey(httpContext) != null;
    }

    #region CreateApiKeyAsync
    public async Task<Result<UserApiKeyModel, CreateUserApiKeyErrorKind>> CreateApiKeyAsync(CreateUserApiKeyOptions options)
    {
        var httpContext = options.HttpContext ?? _httpContextAccessor.HttpContext;
        var currentUser = await GetCurrentUser(httpContext);

        if (currentUser == null || (currentUser.LockoutEnd.HasValue && currentUser.LockoutEnd > DateTimeOffset.UtcNow))
        {
            return CreateUserApiKeyErrorKind.NotLoggedIn;
        }

        var targetUser = await _db.Users.FindAsync(options.UserId);
        
        if (targetUser == null)
        {
            return CreateUserApiKeyErrorKind.UserNotFound;
        }

        if (!await _userManager.IsInRoleAsync(currentUser, RoleKind.Administrator)
            && targetUser.Id != currentUser.Id)
        {
            _logger.LogWarning("User {CurrentUsername} failed to create an ApiKey for User {TargetUsername} but is missing permission", currentUser.NormalizedUserName, targetUser.NormalizedUserName);
            return CreateUserApiKeyErrorKind.CannotCreateTokenForOtherUsers;
        }

        var now = DateTime.UtcNow;
        var purpose = string.IsNullOrWhiteSpace(options.Purpose?.Trim()) ? null : options.Purpose.Trim();
        var userAgent = GetUserAgentFromContext(httpContext);
        var userIp = httpContext?.Connection.RemoteIpAddress?.ToString();
        var model = new UserApiKeyModel
        {
            UserId = targetUser.Id,
            CreatedByUserId = currentUser.Id,
            Purpose = purpose,
            CreatedAt = now,
            CreatedByUserAgent = userAgent,
            CreatedByIpAddress = userIp
        };
        
        await using var trans = await _db.Database.BeginTransactionAsync();
        try
        {
            await _db.UserApiKeys.AddAsync(model);
            await _db.SaveChangesAsync();
            await trans.CommitAsync();

            LogApiKeyCreated(
                model.Id,
                currentUsername: currentUser.NormalizedUserName ?? currentUser.Id,
                targetUsername: targetUser.NormalizedUserName ?? targetUser.Id,
                purpose);
        }
        catch (Exception ex)
        {
            LogApiKeyCreateFailure(
                ex,
                currentUsername: currentUser.NormalizedUserName ?? currentUser.Id,
                targetUsername: targetUser.NormalizedUserName ?? targetUser.Id,
                purpose);
            await trans.RollbackAsync();
            throw;
        }

        return await _db.UserApiKeys.AsNoTracking().SingleAsync(e => e.Id == model.Id);
    }

    [LoggerMessage(LogLevel.Information, "ApiKey {ApiKeyId} was created by User {CurrentUsername} for User {TargetUsername} with purpose: {Purpose}")]
    partial void LogApiKeyCreated(string apiKeyId, string currentUsername, string targetUsername, string? purpose);

    [LoggerMessage(LogLevel.Error, "User {CurrentUsername} failed to create Api Key for {TargetUsername} with purpose {Purpose}")]
    partial void LogApiKeyCreateFailure(Exception exception, string currentUsername, string targetUsername, string? purpose);
    
    public enum CreateUserApiKeyErrorKind
    {
        [Description("Not Authorized - You are not logged in.")]
        NotLoggedIn,
        [Description("Could not find the target user to create an Api Key for.")]
        UserNotFound,
        [Description("Not Authorized - You do not have permission to create Api Keys for other users.")]
        CannotCreateTokenForOtherUsers,
    }

    public class CreateUserApiKeyOptions
    {
        public required string UserId { get; init; }
        public string? Purpose { get; init; }
        public HttpContext? HttpContext { get; init; }
    }
    #endregion

    #region Delete API Key(s)
    public Task<UnitResult<DeleteAllApiKeysForUserError>> DeleteAllApiKeysAsync()
        => DeleteAllApiKeysAsync(_httpContextAccessor.HttpContext);
    public async Task<UnitResult<DeleteAllApiKeysForUserError>> DeleteAllApiKeysAsync(HttpContext? httpContext)
    {
        var user = await GetCurrentUser(httpContext);
        if (user == null) return DeleteAllApiKeysForUserError.NotLoggedIn;
        return await DeleteAllApiKeysForUser(
            httpContext,
            deletedByUser: null,
            user: user);
    }

    public async Task<UnitResult<DeleteApiKeyError>> DeleteApiKey(
        string apiKey,
        Maybe<UserModel> currentUserValue,
        Maybe<HttpContext> httpContextValue)
    {
        httpContextValue = httpContextValue.Or(_httpContextAccessor.HttpContext ?? Maybe<HttpContext>.None);
        if (httpContextValue.HasNoValue)
        {
            return DeleteApiKeyError.InternalErrorMissingHttpContext;
        }

        var httpContext = httpContextValue.Value;
        var userAgent = GetUserAgentFromContext(httpContext);
        var userIp = httpContext.Connection.RemoteIpAddress?.ToString();
        var now = DateTime.UtcNow;
        

        currentUserValue = currentUserValue.Or(await GetCurrentUser(httpContext) ?? Maybe<UserModel>.None);
        if (currentUserValue.HasNoValue)
        {
            return DeleteApiKeyError.NotLoggedIn;
        }

        var currentUser = currentUserValue.Value;

        var apiKeyModel = await _db.UserApiKeys
            .Include(e => e.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == apiKey && !e.IsDeleted);
        if (apiKeyModel == null)
        {
            return DeleteApiKeyError.ApiKeyNotFound;
        }

        var targetUser = await _db.Users.FindAsync(apiKeyModel.UserId);

        if (!await _userManager.IsInRoleAsync(currentUser, RoleKind.Administrator)
            && apiKeyModel.UserId !=  currentUser.Id)
        {
            return DeleteApiKeyError.NotAuthorizedToDeleteOtherPeoplesKeys;
        }
        
        await using var trans = await _db.Database.BeginTransactionAsync();
        try
        {
            await _db.UserApiKeys.Where(e => e.Id == apiKeyModel.Id && !e.IsDeleted)
                .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.IsDeleted, true)
                    .SetProperty(p => p.DeletedByUserId, currentUser.Id)
                    .SetProperty(p => p.DeletedAt, now)
                    .SetProperty(p => p.DeletedByUserAgent, userAgent)
                    .SetProperty(p => p.DeletedByIpAddress, userIp));
            
            await _db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            LogDeleteApiKeyFailure(
                ex,
                currentUsername: currentUser.NormalizedUserName ?? currentUser.Id,
                apiKeyModel.Id,
                targetUsername: targetUser?.NormalizedUserName ?? targetUser?.Id ?? apiKeyModel.UserId);
            await trans.RollbackAsync();
            throw;
        }

        LogDeleteApiKey(
            apiKeyModel.Id,
            targetUsername: targetUser?.NormalizedUserName ?? targetUser?.Id ?? apiKeyModel.UserId,
            currentUsername: currentUser.NormalizedUserName ?? currentUser.Id);
        return UnitResult.Success<DeleteApiKeyError>();
    }

    [LoggerMessage(LogLevel.Error, "User {CurrentUsername} failed to delete ApiKey {ApiKeyId} which is for User {TargetUsername}")]
    partial void LogDeleteApiKeyFailure(Exception ex, string currentUsername, string apiKeyId, string targetUsername);

    [LoggerMessage(LogLevel.Information, "ApiKey {ApiKeyId} for {TargetUsername} was deleted by {CurrentUsername}")]
    partial void LogDeleteApiKey(string apiKeyId, string targetUsername, string currentUsername);

    public enum DeleteApiKeyError
    {
        [Description("Internal Error (missing HttpContext)")]
        InternalErrorMissingHttpContext,
        [Description("Not Authorized - You are not logged in.")]
        NotLoggedIn,
        [Description("Could not find Api Key")]
        ApiKeyNotFound,
        [Description("You do not have permission to delete other users Api Keys")]
        NotAuthorizedToDeleteOtherPeoplesKeys
    }
    
    public async Task<UnitResult<DeleteAllApiKeysForUserError>> DeleteAllApiKeysForUser(
        HttpContext? httpContext,
        UserModel? deletedByUser,
        UserModel user)
    {
        httpContext ??= _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return DeleteAllApiKeysForUserError.InternalErrorMissingHttpContext;
        }
        deletedByUser ??= await GetCurrentUser(httpContext);
        if (deletedByUser == null)
        {
            return DeleteAllApiKeysForUserError.NotAuthorizedToDeleteOtherPeoplesKeys;
        }
        
        var userAgent = GetUserAgentFromContext(httpContext);
        var userIp = httpContext.Connection.RemoteIpAddress?.ToString();
        var now = DateTime.UtcNow;

        await using var trans = await _db.Database.BeginTransactionAsync();
        try
        {
            await _db.UserApiKeys.Where(e => e.UserId == user.Id && !e.IsDeleted)
                .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.IsDeleted, true)
                    .SetProperty(p => p.DeletedByUserId, deletedByUser?.Id)
                    .SetProperty(p => p.DeletedAt, now)
                    .SetProperty(p => p.DeletedByUserAgent, userAgent)
                    .SetProperty(p => p.DeletedByIpAddress, userIp));
            
            await _db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "User {Username} ({UserId}) failed to delete all Api Keys for {TargetUsername} ({TargetUserId})",
                deletedByUser,
                deletedByUser?.Id,
                user,
                user.Id);
            await trans.RollbackAsync();
            throw;
        }

        return UnitResult.Success<DeleteAllApiKeysForUserError>();
    }
    #endregion

    private static string? GetUserAgentFromContext(Maybe<HttpContext> httpContext)
        => httpContext.Match<string?, HttpContext>(
            ctx => string.Join("\n", ctx.Request.Headers.UserAgent.Select(e => e ?? string.Empty)),
            () => null);

    public enum DeleteAllApiKeysForUserError
    {
        [Description("Not Authorized - You are not logged in.")]
        NotLoggedIn,
        [Description("Internal Error (missing HttpContext)")]
        InternalErrorMissingHttpContext,
        [Description("You do not have permission to delete other users Api Keys")]
        NotAuthorizedToDeleteOtherPeoplesKeys
    }
}
