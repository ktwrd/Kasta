using System.Text;

namespace Kasta.Web.Services;

public class ShortUrlService
{
    private const string AlphaNumericUpperLower = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string AlphaNumericLower = "0123456789abcdefghijklmnopqrstuvwxyz";

    public string Generate(int length = 8)
        => GenerateInternal(length, AlphaNumericUpperLower);

    public string GenerateForLinkShortener(int length = 8)
        => GenerateInternal(length, AlphaNumericLower.ToUpper());
    
    private static string GenerateInternal(
        int length,
        string chars)
    {
        if (length < 1)
            throw new ArgumentException("Value must be greater than zero", nameof(length));
        var res = new StringBuilder();
        var rnd = new Random();
        while (0 < length--)
        {
            res.Append(chars[rnd.Next(chars.Length)]);
        }
        return res.ToString();
    }
}