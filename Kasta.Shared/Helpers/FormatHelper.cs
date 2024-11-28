namespace Kasta.Shared.Helpers;

public static class FormatHelper
{
    public static string ToEmoji(bool value)
    {
        return value ? "✔️" : "❌";
    }
}