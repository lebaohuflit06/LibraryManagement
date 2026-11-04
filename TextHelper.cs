using System.Globalization;
using System.Text;

namespace LibraryManagement.Helpers;

public static class TextHelper
{
    // Bỏ dấu tiếng Việt để gõ "nha gia kim" vẫn tìm ra "Nhà giả kim"
    public static string Normalize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var d = s.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in d)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
