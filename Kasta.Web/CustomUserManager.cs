using Kasta.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kasta.Web;
public class CustomUserManager<TUser>
    : UserManager<TUser>
    where TUser : IdentityUser<string>
{
    private readonly ApplicationDbContext _db;
    public CustomUserManager(IUserStore<TUser> store,
        IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<TUser> passwordHasher,
        IEnumerable<IUserValidator<TUser>> userValidators,
        IEnumerable<IPasswordValidator<TUser>> passwordValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        IServiceProvider services,
        ILogger<CustomUserManager<TUser>> logger,
        ApplicationDbContext db)
        : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
    {
        _db = db;
    }
    
    public class UserCreatedEventArgs(string userId, IdentityResult result)
    {
        public string UserId { get; private set; } = userId;
        public IdentityResult Result { get; private set; } = result;
        public bool Success => Result.Succeeded;
    }
    
    /// <summary>
    /// Invoked when <see cref="CreateAsync(TUser)"/> has been called.
    /// </summary>
    public event EventHandler<UserCreatedEventArgs>? UserCreated;
    private void OnUserCreated(UserCreatedEventArgs e)
    {
        UserCreated?.Invoke(this, e);
    }

    /// <summary>
    /// Fetches the current amount of users before calling <see cref="UserManager{TUser}.CreateAsync(TUser)"/>,
    /// and if it's zero (and the user creation succeeded), then that new user is made an administrator.
    /// </summary>
    /// <param name="user"><inheritdoc cref="UserManager{TUser}.CreateAsync(TUser)" path="/param[@name='user']"/></param>
    /// <returns><inheritdoc cref="UserManager{TUser}.CreateAsync(TUser)" path="/returns"/></returns>
    /// <exception cref="InvalidDataException">
    /// Thrown when this method failed to find the Administrator role, even though <see cref="ApplicationDbContext.EnsureInitialRoles"/> worked.
    /// </exception>
    public override async Task<IdentityResult> CreateAsync(TUser user)
    {
        var previousCount = await _db.Users.CountAsync();
        var result = await base.CreateAsync(user);
        var currentCount = await _db.Users.CountAsync();
        if (!result.Succeeded)
        {
            
            var formattedResultErrors = Environment.NewLine + string.Join(Environment.NewLine,
                result.Errors.Select(e => $"{e.Code}: {e.Description}"));
            Logger.LogWarning(
                "Failed to create user {UserEmail} ({UserId}) {FormattedResultErrors}",
                user.Email,
                user.Id,
                formattedResultErrors);
            return result;
        }

        if (!(previousCount == 0 && currentCount == 1))
        {
            OnUserCreated(new(user.Id, result));
            return result;
        }

        // Add user to administrators role.
        await using var ctx = _db.CreateSession();
        ctx.EnsureInitialRoles();

        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var targetNormalizedName = RoleKind.Administrator.ToUpper();
                
            var adminRole = await ctx.Roles
                .Where(e => e.NormalizedName == targetNormalizedName)
                .FirstOrDefaultAsync();

            if (adminRole == null)
            {
                adminRole = new IdentityRole()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = RoleKind.Administrator,
                    NormalizedName = RoleKind.Administrator.ToUpper(),
                    ConcurrencyStamp = null
                };
                await ctx.Roles.AddAsync(adminRole);
                Logger.LogInformation("Created Role: {RoleNormalizedName} ({RoleId})", adminRole.NormalizedName, adminRole.Id);
            }

            if (!await ctx.UserRoles.AnyAsync(e => e.RoleId == adminRole.Id && e.UserId == user.Id))
            {
                // Add the new user role
                await ctx.UserRoles.AddAsync(new IdentityUserRole<string>()
                {
                    UserId = user.Id,
                    RoleId = adminRole.Id
                });
            }
            else
            {
                Logger.LogDebug(
                    "User {UserName} ({UserId}) already has the role {AdminRoleName} ({AdminRoleId}). This is weird since they're the first user to sign up",
                    user.UserName,
                    user.Id,
                    adminRole.Name,
                    adminRole.Id);
            }
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
            Logger.LogInformation(
                "Granted role {AdminRoleName} ({AdminRoleId}) to the first user who signed up, {UserName} ({UserId})",
                adminRole.Name,
                adminRole.Id,
                user.UserName,
                user.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Failed to grant user {UserName} ({UserId}) the \"{Role}\" role",
                user.UserName,
                user.Id,
                RoleKind.Administrator);
            await trans.RollbackAsync();
        }
        OnUserCreated(new(user.Id, result));
        return result;
    }

    public override async Task<bool> IsInRoleAsync(TUser user, string role)
    {
        return await base.IsInRoleAsync(user, RoleKind.Administrator)
            || await base.IsInRoleAsync(user, role);
    }
}