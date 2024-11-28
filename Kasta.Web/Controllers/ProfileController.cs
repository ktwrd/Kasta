using Kasta.Web.Data;
using Kasta.Web.Data.Models;
using Kasta.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kasta.Web.Controllers;

public class ProfileController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<UserModel> _userManager;

    public ProfileController(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _userManager = services.GetRequiredService<UserManager<UserModel>>();
    }
    
    [AuthRequired]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        return View("Index", user);
    }

    [AuthRequired]
    public async Task<IActionResult> Save(
        [FromForm] UserProfileSaveParams data)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            throw new InvalidOperationException(
                $"User returned null from {typeof(UserManager<UserModel>)} (method: {nameof(_userManager.GetUserAsync)})");
        }


        using var ctx = _db.CreateSession();
        var transaction = await ctx.Database.BeginTransactionAsync();
        try
        {
            var userModel = await ctx.Users.Where(e => e.Id == currentUser.Id).FirstAsync();
            if (userModel == null)
            {
                throw new InvalidOperationException(
                    $"User returned null from {_db.Users.GetType()} (where Id = {currentUser.Id})");
            }

            userModel.ThemeName = data.ThemeName;
            
            await ctx.SaveChangesAsync();
            await transaction.CommitAsync();
            await ctx.SaveChangesAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        
        return new RedirectToActionResult(nameof(Index), "Profile", null);
    }
}