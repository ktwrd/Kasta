using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Data.Models.Audit;
using NLog;

namespace Kasta.Web.Services;

public class AuditService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly ApplicationDbContext _db;

    public AuditService(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
    }

    public class AuditCollectionItem
    {
        public AuditModel Model { get; set; }
        public List<AuditEntryModel> Entries { get; set; } = [];

        public AuditCollectionItem(AuditModel model)
        {
            Model = model;
        }
    }
    public List<AuditCollectionItem> GenerateDeleteAudit<T>(UserModel user, IEnumerable<T> data, Func<T, string> pkSelect, string tableName)
    {
        var result = new List<AuditCollectionItem>();
        foreach (var i in data)
        {
            result.Add(GenerateDeleteAudit(user, i, pkSelect, tableName));
        }
        return result;
    }
    public AuditCollectionItem GenerateDeleteAudit<T>(UserModel user, T obj, Func<T, string> pkSelect, string tableName)
    {
        var auditModel = new AuditModel()
        {
            CreatedBy = user.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            Kind = AuditEventKind.Delete,
            EntityName = tableName,
            PrimaryKey = pkSelect(obj)
        };
        var entries = new List<AuditEntryModel>();
        foreach (var prop in typeof(T).GetProperties())
        {
            var propTypeStr = prop.PropertyType.ToString();
            if (propTypeStr.StartsWith("System.Collections")
             || propTypeStr.StartsWith("Kasta.Data.Models")
             || propTypeStr.StartsWith("Kasta.Web.Models")
             || propTypeStr.StartsWith(nameof(NpgsqlTypes)))
                continue;
            var value = prop.GetValue(obj);

            string? stringValue = null;
            if (prop.PropertyType == typeof(string)
            || prop.PropertyType == typeof(char)

            || prop.PropertyType == typeof(sbyte)
            || prop.PropertyType == typeof(byte)
            || prop.PropertyType == typeof(short)
            || prop.PropertyType == typeof(ushort)
            || prop.PropertyType == typeof(int)
            || prop.PropertyType == typeof(uint)
            || prop.PropertyType == typeof(long)
            || prop.PropertyType == typeof(ulong)
            || prop.PropertyType == typeof(nint)
            || prop.PropertyType == typeof(nuint)

            || prop.PropertyType == typeof(float)
            || prop.PropertyType == typeof(double)
            || prop.PropertyType == typeof(decimal)
            || prop.PropertyType == typeof(bool))
            {
                stringValue = value?.ToString();
            }
            else if (value is DateTime dt)
            {
                stringValue = dt.ToString("R");
            }
            else if (value is DateTimeOffset dto)
            {
                stringValue = dto.ToString("R");
            }
            else if (prop.PropertyType.IsEnum)
            {
                stringValue = value?.ToString();
            }
            else if (value == null)
            {
                stringValue = null;
            }
            else
            {
                throw new InvalidOperationException($"Unknown type {prop.PropertyType}");
            }
            entries.Add(new()
            {
                AuditId = auditModel.Id,
                PropertyName = prop.Name,
                Value = stringValue
            });
        }
        return new AuditCollectionItem(auditModel)
        {
            Entries = entries
        };
    }
    public async Task InsertAuditData(ApplicationDbContext db, AuditCollectionItem data)
    {
        await db.Audit.AddAsync(data.Model);
        await db.AuditEntries.AddRangeAsync(data.Entries.ToArray());
    }
    public async Task InsertAuditData(ApplicationDbContext db, List<AuditCollectionItem> data)
    {
        foreach (var i in data)
        {
            await InsertAuditData(db, i);
        }
    }
}