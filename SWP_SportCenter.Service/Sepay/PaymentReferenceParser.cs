using System.Text.RegularExpressions;

namespace SWP_SportCenter.Service.Sepay;

// FIX SEPAY: ngân hàng có thể chèn khoảng trắng trong mã; không khớp mã thiếu hoặc dùng LIKE.
public static class PaymentReferenceParser
{
    private static readonly Regex Incoming = new(@"(?<![A-Z0-9])SWP(?:[ \t\r\n]*[0-9A-F]){32}(?![A-Z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    public static string[] Extract(params string?[] fields)
        // FIX: xét riêng Code và Content, không nối hai trường để vô tình tạo ra mã mới.
        => fields.Where(f => !string.IsNullOrEmpty(f))
            .SelectMany(f => Incoming.Matches(f!).Cast<Match>())
            .Select(m => string.Concat(m.Value.Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant())
            .Distinct(StringComparer.Ordinal).ToArray();
}
