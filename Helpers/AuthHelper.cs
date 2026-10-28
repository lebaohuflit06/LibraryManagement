using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.Cookies;
using LibraryManagement.Models;

namespace LibraryManagement.Helpers;

public static class AuthHelper
{
    public const string MustChangePasswordClaim = "mcp";

    public static ClaimsPrincipal BuildPrincipal(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        if (user.MustChangePassword) claims.Add(new Claim(MustChangePasswordClaim, "1"));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    // mật khẩu tạm 10 ký tự, có chữ hoa, chữ thường, số (bỏ ký tự dễ nhầm như 0/O, 1/l/I)
    public static string GeneratePassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ", lower = "abcdefghijkmnpqrstuvwxyz", digits = "23456789";
        var all = upper + lower + digits;
        var chars = new List<char> { Pick(upper), Pick(lower), Pick(digits) };
        while (chars.Count < 10) chars.Add(Pick(all));
        // xáo trộn để ký tự bắt buộc không luôn nằm đầu
        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars.ToArray());
    }

    private static char Pick(string s) => s[RandomNumberGenerator.GetInt32(s.Length)];
}
