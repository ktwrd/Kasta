using System.ComponentModel;
using System.Xml.Serialization;

namespace Kasta.Shared;

public class SqliteDatabaseConfig
{
    /// <summary>
    /// Location to where the SQLite database will be.
    /// If no directory path is specified, then it'll use whatever directory the Kasta executable is in.
    ///
    /// But if you're in a docker container, the default path for where the filename will be (if no directory in location) will be
    /// <c>/config/kasta.db</c>
    /// </summary>
    [XmlElement("Location")]
    [DefaultValue("kasta.db")]
    public string Location { get; set; } = "kasta.db";

    public string GetLocation()
    {
        if (string.IsNullOrWhiteSpace(Location))
            throw new InvalidOperationException(
                "Configuration is invalid, an empty location was provided for the SQLite database configuration.");
        
        return Path.GetFullPath(Location,
            FeatureFlags.RunningInDocker
            ? "/config/"
            : Directory.GetCurrentDirectory());
    }
}