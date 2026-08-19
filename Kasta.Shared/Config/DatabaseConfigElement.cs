using System.ComponentModel;
using System.Xml.Serialization;

namespace Kasta.Shared;

public class DatabaseConfigElement
{
    [DefaultValue(DatabaseProviderKind.Postgres)]
    [XmlAttribute("Provider")]
    public DatabaseProviderKind Provider { get; set; } = DatabaseProviderKind.Postgres;
    
    [XmlElement("Sqlite")]
    public SqliteDatabaseConfig? Sqlite { get; set; }
    
    [XmlElement("Postgres")]
    public PostgresDatabaseConfig? Postgres { get; set; }
    
    #region Legacy Postgres Values
    [XmlAttribute("Host")]
    public string? LegacyPgHost { get; set; }
    [XmlAttribute("Port")]
    public int? LegacyPgPort { get; set; }
    [XmlAttribute("Name")]
    public string? LegacyPgName { get; set; }
    [XmlElement("Username")]
    public string? LegacyPgUsername { get; set; }
    [XmlElement("Password")]
    public string? LegacyPgPassword { get; set; }
    #endregion

    /// <summary>
    /// Returns false if <see cref="Provider"/> isn't <see cref="DatabaseProviderKind.Postgres"/>
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
        if (Provider != DatabaseProviderKind.Postgres) return false;
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
