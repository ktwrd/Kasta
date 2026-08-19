using Kasta.Shared;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Kasta.Data;

public static class DatabaseHelper
{
    public const string EmptyGuid = "00000000-0000-0000-0000-000000000000";
    public const int GuidLength = 36;

    public static string ToConnectionString(
        this PostgresDatabaseConfig element,
        bool includeErrorDetail = false)
    {
        var b = new NpgsqlConnectionStringBuilder
        {
            Host = element.Host,
            Port = element.Port,
            Username = element.Username,
            Password = element.Password,
            Database = element.Name,
            IncludeErrorDetail = includeErrorDetail,
            ApplicationName = "Kasta"
        };
        return b.ToString();
    }

    public static string ToConnectionString(this SqliteDatabaseConfig element)
    {
        var b = new SqliteConnectionStringBuilder()
        {
            DataSource = element.GetLocation(),
            Pooling = true
        };
        return b.ToString();
    }
}