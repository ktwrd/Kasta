using Kasta.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kasta.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("~/Admin/[controller]")]
[Authorize(Roles = $"{RoleKind.Administrator}, {RoleKind.FileAdmin}")]
public class FileController : Controller
{
}