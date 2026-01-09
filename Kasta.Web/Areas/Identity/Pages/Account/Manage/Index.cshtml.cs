// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kasta.Web.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<UserModel> _userManager;
        private readonly SignInManager<UserModel> _signInManager;

        public IndexModel(
            ApplicationDbContext db,
            UserManager<UserModel> userManager,
            SignInManager<UserModel> signInManager)
        {
            _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            [DefaultValue(false)]
            [Display(Name = "Show File Preview in File List (Home)")]
            public bool ShowFilePreviewInHome { get; set; }

            [Display(Name = "Theme")]
            public UserProfileThemeKind? Theme { get; set; }
        }

        private async Task LoadAsync(UserModel user)
        {
            var userName = await _userManager.GetUserNameAsync(user);

            var settings = await _db.GetUserSettingsAsync(user);
            var themeName = user.ThemeName ?? settings.ThemeName;
            
            Username = userName;

            Input = new InputModel
            {
                ShowFilePreviewInHome = settings.ShowFilePreviewInHome,
                Theme = Enum.GetValues<UserProfileThemeKind>()
                    .FirstOrDefault(kind => kind.ToCodeValue()?.
                        Equals(themeName, StringComparison.OrdinalIgnoreCase) ?? false)
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }
            
            await using var trans = await _db.Database.BeginTransactionAsync();
            try
            {
                if (await _db.UserSettings.AnyAsync(e => e.Id == user.Id))
                {
                    await _db.UserSettings.Where(e => e.Id == user.Id)
                        .ExecuteUpdateAsync(e => e
                            .SetProperty(p => p.ShowFilePreviewInHome, Input.ShowFilePreviewInHome)
                            .SetProperty(p => p.ThemeName, Input.Theme?.ToCodeValue()));
                }
                else
                {
                    await _db.UserSettings.AddAsync(new UserSettingModel()
                    {
                        ShowFilePreviewInHome = Input.ShowFilePreviewInHome,
                        ThemeName = Input.Theme?.ToCodeValue()
                    });
                }
                await _db.Users.Where(e => e.Id == user.Id)
                    .ExecuteUpdateAsync(e => e
                        .SetProperty(p => p.ThemeName, Input.Theme?.ToCodeValue()));
                await _db.SaveChangesAsync();
                await trans.CommitAsync();
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}

public enum UserProfileThemeKind
{
    [Code(null)]
    [Description("Auto")]
    Auto,
    [Code("dark")]
    [Description("Dark")]
    Dark,
    [Code("light")]
    [Description("Light")]
    Light,
    [Code("2010")]
    [Description("2010 Theme")]
    TwentyTen
}