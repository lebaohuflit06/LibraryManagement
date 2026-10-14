namespace LibraryManagement.Models;

public class AppUser
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Member"; // "member" hoặc "admin"
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public LibraryCard? Card { get; set; }
}
