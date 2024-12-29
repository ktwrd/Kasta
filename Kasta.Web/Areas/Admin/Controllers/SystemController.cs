using System.Text.Json;
using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Web.Areas.Admin.Models.System;
using Kasta.Web.Helpers;
using Kasta.Web.Models;
using Kasta.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace Kasta.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("~/Admin/[controller]")]
[Authorize(Roles = RoleKind.Administrator)]
public class SystemController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<SystemController> _logger;
    private readonly FileService _fileService;

    public SystemController(IServiceProvider services, ILogger<SystemController> logger)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _fileService = services.GetRequiredService<FileService>();

        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var model = new IndexViewModel();

        model.UserCount = _db.Users.Count();

        var spaceUsedValue = _db.UserLimits.Select(e => e.SpaceUsed).Sum();
        model.TotalSpaceUsed = SizeHelper.BytesToString(spaceUsedValue);

        var previewSpaceUsedValue = _db.UserLimits.Select(e => e.PreviewSpaceUsed).Sum();
        model.TotalPreviewSpaceUsed = SizeHelper.BytesToString(previewSpaceUsedValue);

        model.OrphanFileCount = _db.Files.Where(e => e.CreatedByUser == null).Include(e => e.CreatedByUser).Count();

        model.FileCount = _db.Files.Count();

        model.LinkCount = _db.ShortLinks.Count();
        
        return View("Index", model);
    }

    [HttpGet("MetricsComponent")]
    public IActionResult GetMetricsComponent()
    {
        var vm = GetMetricsComponentViewModel();
        return PartialView("MetricsComponent", vm);
    }
    public MetricsComponentViewModel GetMetricsComponentViewModel()
    {
        var vm = new MetricsComponentViewModel();
        vm.UserCount = _db.Users.Count();
        var spaceUsedValue = _db.UserLimits.Select(e => e.SpaceUsed).Sum();
        vm.TotalSpaceUsed = SizeHelper.BytesToString(spaceUsedValue);
        var previewSpaceUsedValue = _db.UserLimits.Select(e => e.PreviewSpaceUsed).Sum();
        vm.TotalPreviewSpaceUsed = SizeHelper.BytesToString(previewSpaceUsedValue);
        vm.OrphanFileCount = _db.Files.Where(e => e.CreatedByUser == null).Include(e => e.CreatedByUser).Count();
        vm.FileCount = _db.Files.Count();
        vm.LinkCount = _db.ShortLinks.Count();
        return vm;
    }

    [HttpGet("SettingsComponent")]
    public IActionResult GetSettingsComponent()
    {
        var settings = _db.GetSystemSettings();
        var vm = new SettingsComponentViewModel()
        {
            SystemSettings = settings
        };
        return PartialView("SettingsComponent", vm);
    }

    [HttpPost("SettingsComponent")]
    public async Task<IActionResult> SaveSettingsComponent(
        [FromForm] SystemSettingsParams data)
    {
        using var ctx = _db.CreateSession();
        using var transaction = await ctx.Database.BeginTransactionAsync();

        var currentSettings = ctx.GetSystemSettings();
        bool refresh = false;
        try
        {
            if (currentSettings.EnableGeoIP != data.EnableGeoIP || currentSettings.GeoIPDatabaseLocation != data.GeoIPDatabaseLocation)
            {
                refresh = true;
            }
            data.InsertOrUpdate(ctx);
            await ctx.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        if (refresh)
        {
            try
            {
                _logger.LogDebug($"Refreshing Database for {nameof(TimeZoneService)}");
                TimeZoneService.OnRefreshDatabase();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to refresh database for {nameof(TimeZoneService)} (via {nameof(TimeZoneService)}.{nameof(TimeZoneService.OnRefreshDatabase)})");
            }
        }

        var vm = new SettingsComponentViewModel()
        {
            SystemSettings = currentSettings,
            AlertContent = "Saved Successfully",
            AlertType = "success"
        };
        return PartialView("SettingsComponent", vm);
    }

    [HttpGet("RecalculateStorage")]
    public async Task<IActionResult> RecalculateStorage(
        [FromQuery] string? resultComponent = null)
    {
        var taskList = new List<Task>();
        long fileCount = 0;
        foreach (var user in _db.Users.ToList())
        {
            taskList.Add(new Task(delegate
            {
                fileCount += _fileService.RecalculateSpaceUsed(user).Result;
            }));
        }
        foreach (var t in taskList)
            t.Start();
        await Task.WhenAll(taskList);

        var alertViewModel = new BaseAlertViewModel()
        {
            AlertContent = $"Recalculated storage space ({fileCount} files)",
            AlertType = "success",
            AlertIsSmall = true
        };
        return GenerateActionResultForTaskComponent(resultComponent, alertViewModel);
    }

    private IActionResult GenerateActionResultForTaskComponent(string? resultComponent, BaseAlertViewModel alert)
    {
        var resultComponentTrim = resultComponent?.Trim()?.ToLower();
        switch (resultComponentTrim)
        {
            case "metrics":
                var metricViewModel = GetMetricsComponentViewModel();
                metricViewModel.AlertContent = alert.AlertContent;
                metricViewModel.AlertType = alert.AlertType;
                metricViewModel.AlertIsSmall = alert.AlertIsSmall;
                return PartialView("MetricsComponent", metricViewModel);
            case "alert":
                return PartialView("Components/Alert/Default", alert);
            default:
                return new RedirectToActionResult("Index", "System", new {area = "Admin"});
        }
    }

    [HttpGet("GenerateFileMetadata")]
    public async Task<IActionResult> GenerateFileMetadata(
        [FromQuery] string? resultComponent = null,
        [FromQuery] bool force = false)
    {
        var files = await _db.Files.Where(e => e.MimeType != null && e.MimeType.StartsWith("image/")).ToListAsync();
        if (force)
        {
            var fileIds = files.Select(e => e.Id).ToList();
            var imageInfos = await _db.FileImageInfos.Where(e => fileIds.Contains(e.Id)).ToListAsync();
            var imageInfoIds = imageInfos.Select(e => e.Id).ToList();
            files = files.Where(e => imageInfoIds.Contains(e.Id) == false).ToList();
        }
        _logger.LogInformation($"Generating Metadata for {files.Count} files. (force: {force})");
        var thread = new Thread((workingFilesObj =>
        {
            _logger.LogInformation($"ThreadStart");
            if (!(workingFilesObj is List<FileModel> workingFiles))
            {
                _logger.LogDebug($"Could not run thread since parameter isn't List<FileModel>");
                return;
            }
            _logger.LogInformation($"Thread | Processing {workingFiles} files (force: {force})");
            
            Parallel.ForEach(workingFiles, f =>
            {
                var logger = LogManager.GetCurrentClassLogger();
                logger.Properties["Request"] = nameof(GenerateFileMetadata);
                try
                {
                    _fileService.GenerateFileMetadataNow(f).Wait();
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to generate file metadata for {f.Id}");
                    SentrySdk.CaptureException(
                        ex, scope =>
                        {
                            scope.SetExtra(
                                "File", JsonSerializer.Serialize(
                                    f, new JsonSerializerOptions()
                                    {
                                        WriteIndented = true
                                    }));
                        });
                }
                
            });
        }));
        thread.Start(files);
        
        
        var alertViewModel = new BaseAlertViewModel()
        {
            AlertContent = $"Generating metadata for {files.Count} files (this might take a while)",
            AlertType = "success",
            AlertIsSmall = true
        };
        return GenerateActionResultForTaskComponent(resultComponent, alertViewModel);
    }
}