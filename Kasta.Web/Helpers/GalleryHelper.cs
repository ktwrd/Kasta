using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Data.Models.Gallery;
using Microsoft.AspNetCore.Identity;

namespace Kasta.Web.Helpers;

public static class GalleryHelper
{
    public static async Task<bool> CanAccessGalleryAsync(
        this IGalleryController controller,
        UserModel? user,
        GalleryModel gallery)
    {
        if (gallery.Public) return true;
        if (user == null) return false;

        if (gallery.CreatedByUserId?.Equals(user.Id, StringComparison.InvariantCultureIgnoreCase) ?? false)
            return true;
        
        if (await controller.UserManager.IsInRoleAsync(user, RoleKind.Administrator))
            return true;
        if (await controller.UserManager.IsInRoleAsync(user, RoleKind.GalleryViewOverride))
            return true;
        if (await controller.UserManager.IsInRoleAsync(user, RoleKind.GalleryAdmin))
            return true;
        
        return false;
    }
    
    public static async Task<bool> CanEditGalleryAsync(
        this IGalleryController controller,
        UserModel? user,
        GalleryModel gallery)
    {
        if (user == null) return false;

        if (gallery.CreatedByUserId?.Equals(user.Id, StringComparison.InvariantCultureIgnoreCase) ?? false)
            return true;
        
        if (await controller.UserManager.IsInRoleAsync(user, RoleKind.Administrator))
            return true;
        if (await controller.UserManager.IsInRoleAsync(user, RoleKind.GalleryViewOverride))
            return true;
        if (await controller.UserManager.IsInRoleAsync(user, RoleKind.GalleryAdmin))
            return true;
        
        return false;
    }
}

public interface IGalleryController
{
    public UserManager<UserModel> UserManager { get; }
    public ApplicationDbContext Database { get; }
}