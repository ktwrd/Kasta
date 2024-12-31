using Kasta.Shared;
using Npgsql;

namespace Kasta.Data;

public static class DatabaseHelper
{
    public const string EmptyGuid = "00000000-0000-0000-0000-000000000000";
    public const int GuidLength = 36;

    public static string ToConnectionString(this PostgreSQLConfigElement element)
    {
        var b = new NpgsqlConnectionStringBuilder();
        b.Host = element.Host;
        b.Port = element.Port;
        b.Username = element.Username;
        b.Password = element.Password;
        b.Database = element.Name;
        b.ApplicationName = "Kasta";
        return b.ToString();
    }
}