namespace LibraryManagement.Models;

// nhật ký thao tác quan trọng (tài khoản, phân quyền, miễn phí...)
public class AuditLog
{
    public int Id { get; set; }
    public DateTime At { get; set; } = DateTime.Now;
    public string Actor { get; set; } = "";
    public string Action { get; set; } = "";
    public string Target { get; set; } = "";
    public string? Detail { get; set; }
}
