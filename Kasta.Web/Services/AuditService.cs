using System.Reflection;
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
    private AuditEntryModel? GetAuditEntryModel(string parentId, object? instance, PropertyInfo prop)
    {
        var propTypeStr = prop.PropertyType.ToString();
        if (propTypeStr.StartsWith("System.Collections")
            || propTypeStr.StartsWith("Kasta.Data.Models")
            || propTypeStr.StartsWith("Kasta.Web.Models")
            || propTypeStr.StartsWith(nameof(NpgsqlTypes))
            || prop.GetCustomAttribute<AuditIgnoreAttribute>() != null)
            return null;
        var value = prop.GetValue(instance);

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
        else if (value is TimeOnly to)
        {
            stringValue = to.ToString("T");
        }
        else if (value is Guid guid)
        {
            stringValue = guid.ToString();
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
        return new()
        {
            AuditId = parentId,
            PropertyName = prop.Name,
            Value = stringValue
        };
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
            var m = GetAuditEntryModel(auditModel.Id, obj, prop);
            if (m != null)
            {
                entries.Add(m);
            }
        }
        return new AuditCollectionItem(auditModel)
        {
            Entries = entries
        };
    }
    
    public AuditCollectionItem GenerateCreateAudit<T>(UserModel user, T obj, Func<T, string> pkSelect, string tableName)
    {
        var auditModel = new AuditModel()
        {
            CreatedBy = user.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            Kind = AuditEventKind.Insert,
            EntityName = tableName,
            PrimaryKey = pkSelect(obj)
        };
        var entries = new List<AuditEntryModel>();
        foreach (var prop in typeof(T).GetProperties())
        {
            var m = GetAuditEntryModel(auditModel.Id, obj, prop);
            if (m != null)
            {
                entries.Add(m);
            }
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