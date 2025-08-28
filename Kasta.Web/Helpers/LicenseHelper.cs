using Kasta.Shared;
using NLog;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kasta.Web.Helpers;

public static class LicenseHelper
{
    private static List<LicenseResourceNameItem> GetEmbeddedResources()
    {
        var asm = typeof(LicenseHelper).Assembly;

        var identifiers = new HashSet<string>();
        var baseNamespace = "Kasta.Web.ThirdPartyLibraryInfo.";
        foreach (var name in asm.GetManifestResourceNames().Where(e => e.StartsWith(baseNamespace)))
        {
            var nameSplit = name.Split('.');
            if (nameSplit.Length != 6) continue;
            var identifier = nameSplit[^3];
            identifiers.Add(identifier);
        }
        
        var result = new List<LicenseResourceNameItem>();
        foreach (var ident in identifiers)
        {
            result.Add(new()
            {
                Identifier = ident,
                Info = baseNamespace + ident + ".info.json",
                License = baseNamespace + ident + ".license.txt",
            });
        }

        return result;
    }

    private static readonly Lock ParseLock = new();
    private static IReadOnlyList<LicenseItem> _licenses = [];
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions()
    {
        ReferenceHandler = ReferenceHandler.Preserve,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static string? Sanitize(string? value)
    {
        return value?
            .Replace("\r\n", "\n")
            .Replace(">", "&gt;")
            .Replace("<", "&lt;")
            .Replace("\\", "")
            .Trim().Trim("\n".ToCharArray());
    }

    private static IReadOnlyList<LicenseItem> Parse()
    {
        var result = new List<LicenseItem>();
        var asm = typeof(LicenseHelper).Assembly;
        var pkgLicenses = GetPackageLicenses();
        var found = new List<string>();
        foreach (var res in GetEmbeddedResources())
        {
            #region Parse Info
            using var infoStream = asm.GetManifestResourceStream(res.Info);
            if (infoStream == null)
            {
                throw new EmbeddedResourceException(
                    $"Resource stream \"{res.Info}\" for license identifier \"{res.Identifier}\" is null (could not be found)")
                {
                    Assembly = asm,
                    ResourceName = res.Info,
                    ResourceExists = false
                };
            }
            var infoData = JsonSerializer.Deserialize<LibraryInfo>(infoStream, SerializerOptions);
            if (infoData == null)
            {
                throw new EmbeddedResourceException(
                    $"Unable to parse content in resource \"{res.Info}\" with license identifier \"{res.Identifier}\" (JsonSerializer.Deserialize returned null)")
                {
                    Assembly = asm,
                    ResourceName = res.Info,
                    ResourceExists = true
                };
            }

            if (infoData.Kind == LicenseResourceKind.Dotnet)
            {
                if (string.IsNullOrEmpty(infoData.AssemblyName))
                {
                    Log.Warn($"Cannot get assembly information for Library Info \"{res.Identifier}\". Missing \"{nameof(infoData.AssemblyName)}\" property (json property is \"{LibraryInfo.JsonProp_AssemblyName}\")");
                }
                else
                {
                    var infoAsm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(e =>
                        e.GetName().Name?.Equals(infoData.AssemblyName, StringComparison.InvariantCultureIgnoreCase) ??
                        false);
                    if (infoAsm == null)
                    {
                        Log.Warn($"Could not find assembly \"{infoData.AssemblyName}\" for Library Info \"{res.Identifier}\"");
                    }
                    else
                    {
                        var n = infoAsm.GetName();
                        var v = n.Version;
                        infoData.Version = v?.ToString() ?? "<unknown version>";
                        
                        var copyrightAttr = infoAsm.GetCustomAttribute<AssemblyCopyrightAttribute>();
                        if (!string.IsNullOrEmpty(copyrightAttr?.Copyright))
                        {
                            infoData.Copyright = Sanitize(copyrightAttr.Copyright)!;
                        }
                        
                        var descriptionAttr = infoAsm.GetCustomAttribute<AssemblyDescriptionAttribute>();
                        if (!string.IsNullOrEmpty(descriptionAttr?.Description))
                        {
                            infoData.Description = Sanitize(descriptionAttr.Description)!;
                        }
                    }
                }
            }
            #endregion
            
            #region Get License Content
            using var licenseContentStream = asm.GetManifestResourceStream(res.License);
            if (licenseContentStream == null)
            {
                throw new EmbeddedResourceException(
                    $"Resource stream \"{res.License}\" for license identifier \"{res.Identifier}\" is null (could not be found)")
                {
                    Assembly = asm,
                    ResourceName = res.License,
                    ResourceExists = false
                };
            }
            
            using var licenseContentStreamReader = new StreamReader(licenseContentStream);
            var licenseContentLines = new List<string>();
            while (true)
            {
                var line = licenseContentStreamReader.ReadLine();
                if (line == null) break;
                licenseContentLines.Add(line);
            }
            var licenseContent = string.Join('\n', licenseContentLines);
            if (string.IsNullOrEmpty(licenseContent))
            {
                throw new EmbeddedResourceException(
                    $"Resource (as string) has no content (resource name: \"{res.License}\", identifier: \"{res.Identifier}\")")
                {
                    Assembly = asm,
                    ResourceName = res.License,
                    ResourceExists = true
                };
            }
            #endregion

            infoData.Version = infoData.Version.TrimStart('v');
            if (!string.IsNullOrEmpty(infoData.AssemblyName))
            {
                var tgt = pkgLicenses.FirstOrDefault(e => e.PackageId == infoData.AssemblyName);
                if (tgt != null)
                {
                    if (!string.IsNullOrEmpty(tgt.PackageProjectUrl))
                    {
                        infoData.Url = tgt.PackageProjectUrl;
                    }
                    if (!string.IsNullOrEmpty(tgt.PackageVersion))
                    {
                        infoData.Version = tgt.PackageVersion;
                    }
                    infoData.Version = infoData.Version.TrimStart('v');
                    found.Add(tgt.PackageId);
                    result.Add(new LicenseItem
                    {
                        Identifier = res.Identifier,
                        License = tgt.License ?? "Unknown",
                        Info = infoData
                    });
                    continue;
                }
            }
            found.Add(infoData.AssemblyName ?? res.Identifier);
            result.Add(new LicenseItem
            {
                Identifier = res.Identifier,
                License = licenseContent,
                Info = infoData
            });
        }

        foreach (var item in pkgLicenses.Where(e => !found.Contains(e.PackageId)))
        {
            if (string.IsNullOrEmpty(item.License) ||
                string.IsNullOrEmpty(item.PackageVersion))
                continue;
            var info = new LibraryInfo
            {
                Name = item.PackageId,
                Url = item.PackageProjectUrl,
                Version = item.PackageVersion,
                Description = "",
                LicenseType = item.License,
                Copyright = item.Copyright
            };
            if (info.LicenseType.StartsWith("http"))
            {
                info.LicenseType = "(unknown license)";
            }
            info.Version = info.Version.TrimStart('v');
            result.Add(new LicenseItem
            {
                Identifier = item.PackageId,
                License = info.LicenseType,
                Info = info
            });
        }

        return result;
    }



    private static IReadOnlyCollection<AppLicenseFileItem> GetPackageLicenses()
    {
        var resourceName = "Kasta.Web.app-licenses.json";
        var stream = typeof(Program).Assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            return [];
        var data = JsonSerializer.Deserialize<ICollection<AppLicenseFileItem>>(stream, SerializerOptions);
        if (data == null)
            return [];
        return [..data];
    }
    
    /// <summary>
    /// Get all the license information that's stored.
    /// </summary>
    public static IReadOnlyList<LicenseItem> GetLicenses(bool force = false)
    {
        bool doParse;
        lock (_licenses)
        {
            doParse = force || _licenses.Count == 0;
        }
        if (doParse)
        {
            lock (ParseLock)
            {
                // checking again if ParseLock was locked because it was being used.
                // we don't want to parse it again if the user doesn't want to force it to.
                if (doParse || _licenses.Count == 0)
                {
                    _licenses = Parse();
                }
            }
        }
        lock (_licenses)
        {
            return _licenses;
        }
    }

    public static List<OtherLibraryInfo> GetOtherLibraries()
    {
        GetLicenses();
        
        var result = new List<OtherLibraryInfo>();
        lock (ParseLock)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.IsDynamic) continue;
                var name = asm.GetName();
                var ver = name.Version;
                if (string.IsNullOrEmpty(name.Name)) continue;
                bool exists;
                lock (_licenses)
                {
                    exists = _licenses
                        .Any(e =>
                            e.Info.AssemblyName.Equals(name.Name, StringComparison.InvariantCultureIgnoreCase));   
                }
                if (exists) continue;

                result.Add(new  OtherLibraryInfo()
                {
                    Name = name.Name,
                    Version = ver?.ToString() ?? "Unknown Version",
                    Description = Sanitize(asm.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description),
                    Copyright = Sanitize(asm.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright),
                });
            }
        }

        return result;
    }
}

public class OtherLibraryInfo
{
    public required string Name { get; set; }
    public required string Version { get; set; }
    public required string? Description { get; set; }
    public required string? Copyright { get; set; }
}

public class LicenseResourceNameItem
{
    public required string Identifier { get; init; }
    public required string Info { get; init; }
    public required string License { get; init; }
}

public class LicenseItem
{
    public required string Identifier { get; init; }
    public required string License { get; init; }
    public required LibraryInfo Info { get; init; }
}

public class LibraryInfo
{
    [JsonRequired]
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonRequired]
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
    
    [JsonRequired]
    [JsonPropertyName("url")]
    public string? Url { get; set; } = "";
    
    [JsonRequired]
    [JsonPropertyName("license_type")]
    public string LicenseType { get; set; } = "";
    
    [JsonPropertyName("kind")]
    public LicenseResourceKind Kind { get; set; }

    internal const string JsonProp_AssemblyName = "dotnet_asm";
    
    [JsonPropertyName(JsonProp_AssemblyName)]
    public string AssemblyName { get; set; } = "";
    
    [JsonPropertyName("copyright")]
    public string? Copyright { get; set; }
}

public class AppLicenseFileItem
{
    [JsonRequired]
    [JsonPropertyName("PackageId")]
    public string PackageId { get; set; } = "";

    [JsonRequired]
    [JsonPropertyName("PackageVersion")]
    public string PackageVersion { get; set; } = "";

    [JsonPropertyName("PackageProjectUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PackageProjectUrl { get; set; }
    
    [JsonPropertyName("Copyright")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Copyright { get; set; }
    
    [JsonPropertyName("Authors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Authors { get; set; }
    
    [JsonPropertyName("License")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? License { get; set; }
    
    [JsonPropertyName("LicenseUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LicenseUrl { get; set; }

    [JsonPropertyName("LicenseInformationOrigin")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LicenseInformationOrigin { get; set; }

    [JsonPropertyName("ValidationErrors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<AppLicenseFileValidationErrorItem>? ValidationErrors { get; set; }
}
public class AppLicenseFileValidationErrorItem
{
    [JsonPropertyName("Error")]
    public string Error { get; set; } = "";

    [JsonPropertyName("Context")]
    public string Context { get; set; } = "";
}

public enum LicenseResourceKind
{
    Javascript,
    Dotnet
}