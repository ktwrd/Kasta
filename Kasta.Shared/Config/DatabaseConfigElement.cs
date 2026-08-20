using System.Xml.Serialization;

namespace Kasta.Shared;

public class DatabaseConfigElement
{
    [XmlAttribute("Provider")]
    public string? ProviderXmlValue { get; set; }

    public DatabaseProviderKind? ParseProvider()
    {
        if (!string.IsNullOrWhiteSpace(ProviderXmlValue) &&
            Enum.TryParse<DatabaseProviderKind>(ProviderXmlValue, out var enumValue))
            return enumValue;
        return null;
    }
    
    public DatabaseProviderKind GetProvider()
    {
        if (!string.IsNullOrWhiteSpace(ProviderXmlValue) &&
            Enum.TryParse<DatabaseProviderKind>(ProviderXmlValue, out var enumValue))
            return enumValue;
        if (UseLegacyPostgresSettings())
            return DatabaseProviderKind.Postgres;
        return DatabaseProviderKind.Sqlite;
    }
    
    [XmlElement("Sqlite")]
    public SqliteDatabaseConfig? Sqlite { get; set; }
    
    [XmlElement("Postgres")]
    public PostgresDatabaseConfig? Postgres { get; set; }
    
    #region Legacy Postgres Values
    [XmlAttribute("Host")]
    public string? LegacyPgHost { get; set; }
    [XmlAttribute("Port")]
    public string? LegacyPgPortValue { get; set; }

    [XmlIgnore]
    public int? LegacyPgPort
    {
        get => int.TryParse(LegacyPgPortValue, out var v) ? v : null;
        set => LegacyPgPortValue = value?.ToString("D");
    }
    [XmlAttribute("Name")]
    public string? LegacyPgName { get; set; }
    [XmlElement("Username")]
    public string? LegacyPgUsername { get; set; }
    [XmlElement("Password")]
    public string? LegacyPgPassword { get; set; }
    #endregion

    /// <summary>
    /// Returns false if <see cref="ProviderXmlValue"/> isn't <see cref="DatabaseProviderKind.Postgres"/>
    /// or if <see cref="Postgres"/> isn't null
    /// or all of the following properties are null:
    /// <list type="bullet">
    /// <item><see cref="LegacyPgHost"/></item>
    /// <item><see cref="LegacyPgPort"/></item>
    /// <item><see cref="LegacyPgName"/></item>
    /// <item><see cref="LegacyPgUsername"/></item>
    /// <item><see cref="LegacyPgPassword"/></item>
    /// </list>
    /// </summary>
    public bool UseLegacyPostgresSettings()
    {
        var provider = ParseProvider();
        if (provider.HasValue && provider.Value != DatabaseProviderKind.Postgres) return false;
        if (Postgres != null) return false;
        return LegacyPgHost != null
               || LegacyPgPort != null
               || LegacyPgName != null
               || LegacyPgUsername != null
               || LegacyPgPassword != null;
    }

    public SqliteDatabaseConfig GetSqlite()
        => Sqlite ?? new();
    
    public PostgresDatabaseConfig GetPostgres()
    {
        var i = Postgres ?? new();
        if (!UseLegacyPostgresSettings()) return i;
        i.Host = LegacyPgHost ?? i.Host;
        i.Port = LegacyPgPort ?? i.Port;
        i.Name = LegacyPgName ?? i.Name;
        i.Username = LegacyPgUsername ?? i.Username;
        i.Password = LegacyPgPassword ?? i.Password;
        return i;
    }

    public enum DatabaseProviderKind
    {
        [XmlEnum("Postgres")]
        Postgres,
        [XmlEnum("Sqlite")]
        Sqlite
    }
}
