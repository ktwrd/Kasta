using System.ComponentModel;
using System.Diagnostics;
using CSharpFunctionalExtensions;
using Kasta.Data;
using Kasta.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kasta.Web.Services;

public class UserService
{
    private readonly UserManager<UserModel> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<UserService> _logger;

    public UserService(IServiceProvider services, ILogger<UserService> logger)
    {
        _logger = logger;
        _userManager =  services.GetRequiredService<UserManager<UserModel>>();
        _db = services.GetRequiredService<ApplicationDbContext>();
        _httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();
    }

    public Task<UserModel?> GetCurrentUser() => GetCurrentUser(_httpContextAccessor.HttpContext);
    public async Task<UserModel?> GetCurrentUser(HttpContext? httpContext)
    {
        if (httpContext == null) return null;
        return await _userManager.GetUserAsync(httpContext.User)
               ?? await GetCurrentUserViaApiKey(httpContext);
    }

    public Task<UserApiKeyModel?> GetCurrentApiKey() => GetCurrentApiKey(_httpContextAccessor.HttpContext);
    public async Task<UserApiKeyModel?> GetCurrentApiKey(HttpContext? httpContext)
    {
        if (httpContext == null) return null;
        
        string? apiKeyValue = null;
        if (httpContext.Request.Query.TryGetValue("apiKey", out var apiKeyQueryStringValues))
        {
            apiKeyValue ??= apiKeyQueryStringValues.FirstOrDefault();
        }

        if (string.IsNullOrEmpty(apiKeyValue?.Trim())) apiKeyValue = null;

        if (httpContext.Request.Headers.Authorization.Any())
        {
            Debugger.Break();
        }

        var apiKey = await _db.UserApiKeys
            .Include(e => e.User)
            .AsNoTracking()
            .Where(e => e.Token == apiKeyValue && !e.IsDeleted && !e.User.LockoutEnabled)
            .FirstOrDefaultAsync();
        return apiKey?.User == null ? null : apiKey;
    }

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
            await _db.UserApiKeys.Where(e => e.Id == apiKey.Id && !e.IsDeleted)
                .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.LastUsed, DateTimeOffset.UtcNow));
            await _db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark Api Key {ApiKeyId} that it was used", apiKey.Id);
            await trans.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> IsAuthorized(HttpContext? httpContext, bool allowApiKey = true)
    {
        httpContext ??= _httpContextAccessor.HttpContext;
        if (httpContext == null) return false;
        if (httpContext.User.Identity?.IsAuthenticated ?? false) return true;

        var userModel = await _userManager.GetUserAsync(httpContext.User);
        if (userModel != null && !await _userManager.IsLockedOutAsync(userModel)) return true;
        
        return allowApiKey && await GetCurrentApiKey(httpContext) != null;
    }

    public async Task<Result<UserApiKeyModel, CreateUserApiKeyErrorKind>> CreateApiKeyAsync(CreateUserApiKeyOptions options)
    {
        var httpContext = options.HttpContext ?? _httpContextAccessor.HttpContext;
        var currentUser = await GetCurrentUser(httpContext);

        if (currentUser == null || currentUser.LockoutEnabled)
        {
            return CreateUserApiKeyErrorKind.NotLoggedIn;
        }

        if (!await _db.Users.AnyAsync(e => e.Id == options.UserId))
        {
            return CreateUserApiKeyErrorKind.UserNotFound;
        }

        if (!await _userManager.IsInRoleAsync(currentUser, RoleKind.Administrator)
            && options.UserId != currentUser.Id)
        {
            return CreateUserApiKeyErrorKind.CannotCreateTokenForOtherUsers;
        }

        var model = new UserApiKeyModel()
        {
            UserId = currentUser.Id,
            CreatedByUserId = currentUser.Id,
            Purpose = options.Purpose?.Trim()
        };
        await using var trans = await _db.Database.BeginTransactionAsync();
        try
        {
            await _db.UserApiKeys.AddAsync(model);
            await _db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }

        return await _db.UserApiKeys.AsNoTracking().SingleAsync(e => e.Id == model.Id);
    }
    
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
        public required string UserId { get; set; }
        public string? Purpose { get; set; }
        public HttpContext? HttpContext { get; set; }
    }

    public Task<UnitResult<DeleteAllApiKeysForUserError>> DeleteAllApiKeysAsync()
        => DeleteAllApiKeysAsync(_httpContextAccessor.HttpContext);
    public async Task<UnitResult<DeleteAllApiKeysForUserError>> DeleteAllApiKeysAsync(HttpContext? httpContext)
    {
        var user = await GetCurrentUser(httpContext);
        if (user == null) return DeleteAllApiKeysForUserError.NotLoggedIn;
        return await DeleteAllApiKeysForUser(
            httpContext,
            null,
            user);
    }

    public async Task<UnitResult<DeleteApiKeyError>> DeleteApiKey(string apiKey,
        Maybe<UserModel> currentUserValue,
        Maybe<HttpContext> httpContextValue)
    {
        httpContextValue = httpContextValue.Or(_httpContextAccessor.HttpContext ?? Maybe<HttpContext>.None);
        if (httpContextValue.HasNoValue)
        {
            return DeleteApiKeyError.InternalErrorMissingHttpContext;
        }

        var httpContext = httpContextValue.Value;

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


        if (!await _userManager.IsInRoleAsync(currentUser, RoleKind.Administrator)
            && apiKeyModel.UserId !=  currentUser.Id)
        {
            return DeleteApiKeyError.NotAuthorizedToDeleteOtherPeoplesKeys;
        }
        
        await using var trans = await _db.Database.BeginTransactionAsync();
        try
        {
            var userAgent = GetUserAgentFromContext(httpContext);
            var userIp = "";
            var now = DateTimeOffset.UtcNow;

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
            _logger.LogError(ex, "User {Username} ({UserId}) failed to delete Api Key {ApiKeyId}, which belongs to user {TargetUsername} ({TargetUserId})",
                currentUser,
                currentUser.Id,
                apiKeyModel.Id,
                apiKeyModel.User,
                apiKeyModel.User.Id);
            await trans.RollbackAsync();
            throw;
        }

        return UnitResult.Success<DeleteApiKeyError>();
    }

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

        await using var trans = await _db.Database.BeginTransactionAsync();
        try
        {
            var userAgent = GetUserAgentFromContext(httpContext);
            var userIp = "";
            var now = DateTimeOffset.UtcNow;

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

    private static string? GetUserAgentFromContext(Maybe<HttpContext> httpContext)
        => httpContext.Match<string?, HttpContext>(
            ctx => string.Join("\n", ctx.Request.Headers.UserAgent),
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
