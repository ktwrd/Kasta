using Kasta.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kasta.Web;
public class CustomUserManager<TUser> : UserManager<TUser>
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
        ILogger<UserManager<TUser>> logger,
        ApplicationDbContext db)
        : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
    {
        _db = db;
    }
    public class UserCreatedEventArgs
    {
        public string UserId {get; private set;}
        public IdentityResult Result {get; private set;}
        public bool Success => Result.Succeeded;
        public UserCreatedEventArgs(string userId, IdentityResult result)
        {
            UserId = userId;
            Result = result;
        }
    }
    /// <summary>
    /// Invoked when <see cref="CreateUser(TUser)"/> has been called.
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
        if (result.Succeeded)
        {
            // Add user to administrators role.
            if (previousCount == 0)
            {
                using (var ctx = _db.CreateSession())
                {
                    ctx.EnsureInitialRoles();

                    var trans = await ctx.Database.BeginTransactionAsync();
                    try
                    {
                        var targetNormalizedName = RoleKind.Administrator.ToUpper();
                        
                        var adminRole = await ctx.Roles
                            .Where(e => e.NormalizedName == targetNormalizedName)
                            .FirstOrDefaultAsync();

                        if (adminRole == null)
                        {
                            throw new InvalidDataException($"Failed to find Role where {nameof(IdentityRole<string>.NormalizedName)} equals {targetNormalizedName}");
                        }
                        if (await ctx.UserRoles.Where(e => e.RoleId == adminRole.Id && e.UserId == user.Id).AnyAsync() == false)
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
                            Logger.LogDebug($"User {user.Email} ({user.Id}) already has the role {adminRole.Name} ({adminRole.Id}). This is weird since they're the first user to sign up!");
                        }
                        await trans.CommitAsync();
                        Logger.LogInformation($"Granted role {adminRole.Name} ({adminRole.Id}) to the first user who signed up, {user.Email} ({user.Id})");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, $"Failed to grant user {user.Email} ({user.Id}) the \"{RoleKind.Administrator}\" role.");
                        await trans.RollbackAsync();
                    }
                }
            }
        }
        else
        {
            Logger.LogWarning($"Failed to creat user {user.Email} ({user.Id})\n" +
                string.Join("\n", result.Errors.Select(e => $"{e.Code}: {e.Description}")));
        }
        OnUserCreated(new(user.Id, result));
        return result;
    }
}