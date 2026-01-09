namespace Kasta.Shared;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class CodeAttribute(string? code) : Attribute
{
    public string? Code { get; } = code;
}