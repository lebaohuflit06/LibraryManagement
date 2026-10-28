using LibraryManagement.Data;
using LibraryManagement.Models;

namespace LibraryManagement.Helpers;

// ghi nhật ký. Các hàm chỉ Add vào DbContext, nơi gọi tự SaveChanges cùng lúc với thay đổi chính
public static class Logs
{
    public static void Copy(AppDbContext db, BookCopy copy, BookStatus? from, BookStatus to,
        string action, string? actor, int? loanId = null, string? note = null)
    {
        db.CopyLogs.Add(new CopyLog
        {
            Copy = copy,
            At = DateTime.Now,
            FromStatus = from,
            ToStatus = to,
            Action = action,
            Actor = string.IsNullOrWhiteSpace(actor) ? "Hệ thống" : actor,
            LoanId = loanId,
            Note = string.IsNullOrWhiteSpace(note) ? null : note
        });
    }

    // tên đối tượng ghi trong nhật ký, bắt đầu bằng "#id " để lọc được theo từng tài khoản
    public static string UserTarget(AppUser u) => $"#{u.Id} {u.FullName} ({u.Email})";

    public static void Audit(AppDbContext db, string? actor, string action, string target, string? detail = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            At = DateTime.Now,
            Actor = string.IsNullOrWhiteSpace(actor) ? "Hệ thống" : actor,
            Action = action,
            Target = target,
            Detail = detail
        });
    }
}
