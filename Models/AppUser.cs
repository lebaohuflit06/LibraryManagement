namespace LibraryManagement.Models;

public class AppUser
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Member"; // "Admin", "Librarian" hoặc "Member"
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsLocked { get; set; }            // tài khoản bị khóa thì không đăng nhập được
    public string? LockReason { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool MustChangePassword { get; set; }  // true sau khi admin đặt lại mật khẩu
    public LibraryCard? Card { get; set; }
}
