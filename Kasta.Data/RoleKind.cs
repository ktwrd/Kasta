using System.ComponentModel;
using System.Reflection;

namespace Kasta.Data;

public static class RoleKind
{
    /// <summary>
    /// Administrator, can do literally anything.
    /// </summary>
    [Description("Administrator, can do literally anything.")]
    [RoleKindElement]
    public const string Administrator = nameof(Administrator);

    [RoleKindElement]
    public const string UserAdmin = nameof(UserAdmin);

    public static List<RoleItem> ToList()
    {
		var result = new List<RoleItem>();
        foreach (var field in typeof(RoleKind).GetFields())
        {
            var attr = field.GetCustomAttribute<RoleKindElementAttribute>();
			if (attr == null)
				continue;
			var descAttr = field.GetCustomAttribute<DescriptionAttribute>();
			var item = new RoleItem()
			{
				Name = field.Name,
				Description = string.IsNullOrEmpty(descAttr?.Description) ? null : descAttr?.Description
			};
            result.Add(item);
        }
		return result;
    }
	
	public class RoleItem
	{
		public required string Name { get; init; }
		public string? Description { get; init; }
	}
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class RoleKindElementAttribute : Attribute
{}