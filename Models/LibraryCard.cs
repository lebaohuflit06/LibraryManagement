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
    public bool IsActive { get; set; } = true; // thẻ còn hiệu lực hay không
    public List<Loan> Loans { get; set; } = new(); // lịch sử các lần mượn
    public List<Fee> Fees { get; set; } = new(); // sổ phí: bồi thường, trả muộn...
}

