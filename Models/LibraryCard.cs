namespace LibraryManagement.Models;

public class LibraryCard
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public AppUser? User { get; set; }
    public string CardNumber { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string IdNumber { get; set; } = "";
    public DateTime DateOfBirth { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.Now;
    public DateTime ExpiresAt { get; set; }
}
