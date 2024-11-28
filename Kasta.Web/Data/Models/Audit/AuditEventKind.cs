namespace Kasta.Web.Data.Models.Audit;

public enum AuditEventKind : byte
{
    Insert = 0,
    Update = 1,
    Delete = 2
}