using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Web.Models;
using Kasta.Web.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Kasta.Web.Models.Admin;
using Microsoft.EntityFrameworkCore;
using Kasta.Web.Services;
using NLog;
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