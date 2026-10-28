using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace LibraryManagement.Models;

// ============================ sprint 3 ====================================================================

// ---------- tài khoản của tôi ----------
public class ProfileViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên"), StringLength(100, ErrorMessage = "Tối đa 100 ký tự")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = "";

    [BindNever] public string Email { get; set; } = "";
    [BindNever] public string Role { get; set; } = "";
    [BindNever] public DateTime CreatedAt { get; set; }
    [BindNever] public DateTime? LastLoginAt { get; set; }
    [BindNever] public string? CardNumber { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
    [DataType(DataType.Password), Display(Name = "Mật khẩu hiện tại")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới"), MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [DataType(DataType.Password), Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = "";

    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu nhập lại không khớp")]
    [DataType(DataType.Password), Display(Name = "Nhập lại mật khẩu mới")]
    public string ConfirmPassword { get; set; } = "";

    [BindNever] public bool Forced { get; set; } // true nếu bị buộc đổi (admin vừa đặt lại mật khẩu)
}

// ---------- quản lý tài khoản (admin / thủ thư) ----------
public class UserListViewModel
{
    public string? Q { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; } // active | locked
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int Total { get; set; }
    public int MemberCount { get; set; }
    public int LockedCount { get; set; }
    public List<AppUser> Users { get; set; } = new();
}

public class UserCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên"), StringLength(100, ErrorMessage = "Tối đa 100 ký tự")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu"), MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [DataType(DataType.Password), Display(Name = "Mật khẩu")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn vai trò")]
    [Display(Name = "Vai trò")]
    public string Role { get; set; } = "Member";

    [Display(Name = "Buộc đổi mật khẩu ở lần đăng nhập đầu")]
    public bool MustChangePassword { get; set; } = true;

    [BindNever] public List<string> AllowedRoles { get; set; } = new();
}

public class UserEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên"), StringLength(100, ErrorMessage = "Tối đa 100 ký tự")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = "";

    [BindNever] public string Role { get; set; } = "";
}

public class UserLockViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do khóa"), StringLength(300, ErrorMessage = "Tối đa 300 ký tự")]
    [Display(Name = "Lý do khóa")]
    public string Reason { get; set; } = "";

    [BindNever] public string FullName { get; set; } = "";
    [BindNever] public string Email { get; set; } = "";
}

public class UserDetailsViewModel
{
    public AppUser User { get; set; } = null!;
    public LibraryCard? Card { get; set; }
    public int LoanCount { get; set; }
    public int BorrowingCount { get; set; }
    public int UnpaidAmount { get; set; }
    public List<AuditLog> History { get; set; } = new();
    public bool CanManage { get; set; }   // người đang đăng nhập có quyền sửa / khóa / đặt lại mật khẩu tài khoản này
    public bool IsSelf { get; set; }
}

// ---------- phân quyền ----------
public class RoleIndexViewModel
{
    public string? Q { get; set; }
    public string? Role { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int AdminCount { get; set; }
    public int LibrarianCount { get; set; }
    public int MemberCount { get; set; }
    public List<AppUser> Users { get; set; } = new();
    public int CurrentUserId { get; set; }
}

public class AuditViewModel
{
    public string? Q { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public List<AuditLog> Logs { get; set; } = new();
}

// ---------- phí ----------
public class FeeListViewModel
{
    public string? Status { get; set; } = "unpaid"; // unpaid | paid | waived | all
    public string? Type { get; set; }               // late | damaged | lost | other
    public string? Q { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int Total { get; set; }
    public int UnpaidAmount { get; set; }
    public int UnpaidCount { get; set; }
    public int PaidThisMonth { get; set; }
    public int WaivedAmount { get; set; }
    public int OverdueLoans { get; set; }
    public List<Fee> Fees { get; set; } = new();
}

public class FeePayViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn hình thức thanh toán")]
    [Display(Name = "Hình thức thanh toán")]
    public PaymentMethod? Method { get; set; } = PaymentMethod.Cash;

    [StringLength(300, ErrorMessage = "Ghi chú tối đa 300 ký tự")]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    [Display(Name = "Thu luôn các khoản chưa thu khác của thẻ này")]
    public bool IncludeOthers { get; set; }

    [BindNever] public Fee? Fee { get; set; }
    [BindNever] public List<Fee> Others { get; set; } = new();
}

public class FeeWaiveViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do miễn"), StringLength(300, ErrorMessage = "Tối đa 300 ký tự")]
    [Display(Name = "Lý do miễn")]
    public string Reason { get; set; } = "";

    [BindNever] public Fee? Fee { get; set; }
}

public class FeeCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn thẻ")]
    [Display(Name = "Thẻ thư viện")]
    public int? CardId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số tiền")]
    [Range(1000, 10000000, ErrorMessage = "Số tiền từ 1.000 - 10.000.000 đ")]
    [Display(Name = "Số tiền (VNĐ)")]
    public int? Amount { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nội dung khoản phí"), StringLength(300, ErrorMessage = "Tối đa 300 ký tự")]
    [Display(Name = "Nội dung")]
    public string Description { get; set; } = "";

    [BindNever] public List<SelectListItem> Cards { get; set; } = new();
}

public class OverdueRow
{
    public Loan Loan { get; set; } = null!;
    public int Days { get; set; }
    public int Accrued { get; set; }
}

public class OverdueViewModel
{
    public List<OverdueRow> Rows { get; set; } = new();
    public int TotalAccrued { get; set; }
}

public class MyFeesViewModel
{
    public LibraryCard Card { get; set; } = null!;
    public List<Fee> Fees { get; set; } = new();
    public int UnpaidAmount { get; set; }
}

public class ReceiptViewModel
{
    public string ReceiptNo { get; set; } = "";
    public List<Fee> Fees { get; set; } = new();
    public LibraryCard Card { get; set; } = null!;
    public int Total { get; set; }
}

// ---------- theo dõi tình trạng sách ----------
public class TrackingRow
{
    public BookCopy Copy { get; set; } = null!;
    public Loan? CurrentLoan { get; set; } // phiếu đang mượn (nếu cuốn đang cho mượn)
    public int LoanCount { get; set; }     // số lượt đã được mượn
    public bool IsOverdue => CurrentLoan != null && CurrentLoan.IsOverdue;
}

public class TrackingViewModel
{
    public string? Q { get; set; }
    public string? Status { get; set; }   // available | borrowed | damaged | lost | disposed | overdue
    public string? Category { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalRows { get; set; }
    public List<string> Categories { get; set; } = new();
    public List<TrackingRow> Rows { get; set; } = new();

    public int All { get; set; }
    public int Available { get; set; }
    public int Borrowed { get; set; }
    public int Overdue { get; set; }
    public int Damaged { get; set; }
    public int Lost { get; set; }
    public int Disposed { get; set; }
}

public class CopyHistoryViewModel
{
    public BookCopy Copy { get; set; } = null!;
    public List<CopyLog> Logs { get; set; } = new();
    public List<LoanItem> Loans { get; set; } = new();
    public DisposalItem? Disposal { get; set; }
}

// ---------- thanh lý sách ----------
public class DisposalIndexViewModel
{
    public int Year { get; set; }
    public List<int> Years { get; set; } = new();
    public List<DisposalReport> Reports { get; set; } = new();
    public int ItemCount { get; set; }
    public int BookValue { get; set; }
    public int Recovered { get; set; }
    public Dictionary<DisposalReason, int> ByReason { get; set; } = new();
    public Dictionary<DisposalMethod, int> ByMethod { get; set; } = new();
    public int CandidateCount { get; set; }
}

public class CandidateRow
{
    public BookCopy Copy { get; set; } = null!;
    public DisposalReason Reason { get; set; }          // lý do chính
    public List<DisposalReason> Reasons { get; set; } = new();
    public int LoanCount { get; set; }
}

public class DisposalCandidatesViewModel
{
    // bộ lọc (GET)
    public string? Reason { get; set; }   // damaged | lost | worn | old
    public string? Q { get; set; }

    // biểu mẫu lập biên bản (POST)
    [Required(ErrorMessage = "Vui lòng tích chọn ít nhất 1 cuốn để thanh lý")]
    [MinLength(1, ErrorMessage = "Vui lòng tích chọn ít nhất 1 cuốn để thanh lý")]
    public List<int> CopyIds { get; set; } = new();

    [Required(ErrorMessage = "Vui lòng chọn hình thức thanh lý")]
    [Display(Name = "Hình thức thanh lý")]
    public DisposalMethod? Method { get; set; } = DisposalMethod.Destroy;

    [Range(0, 100000000, ErrorMessage = "Số tiền từ 0 - 100.000.000 đ")]
    [Display(Name = "Tiền thu hồi (VNĐ)")]
    public int? RecoveredAmount { get; set; }

    [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự")]
    [Display(Name = "Ghi chú / căn cứ thanh lý")]
    public string? Note { get; set; }

    [BindNever] public List<CandidateRow> Rows { get; set; } = new();
    [BindNever] public int WornCount { get; set; }
    [BindNever] public int OldCount { get; set; }
    [BindNever] public int DamagedCount { get; set; }
    [BindNever] public int LostCount { get; set; }
}
