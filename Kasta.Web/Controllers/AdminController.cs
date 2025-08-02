using Kasta.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Kasta.Web.Controllers;

[Route("~/Admin")]
[Authorize(Roles = RoleKind.Administrator)]
public class AdminController : Controller
{
    [HttpGet("Audit")]
    public IActionResult AuditIndex()
    {
        return View("AuditIndex");
    }
}