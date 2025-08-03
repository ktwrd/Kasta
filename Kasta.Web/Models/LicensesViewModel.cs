using Kasta.Web.Helpers;

namespace Kasta.Web.Models;

public class LicensesViewModel
{
    public required IReadOnlyList<LicenseItem> Licenses { get; init; }
    public required IReadOnlyList<OtherLibraryInfo> OtherLibraries { get; init; }
}