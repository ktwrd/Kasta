namespace Kasta.Data;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class AuditIgnoreAttribute : Attribute
{}