using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibraryManagement.Models;

public class HomeViewModel
{
    public List<Book> NewBooks { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public int BookCount { get; set; }
    public int MemberCount { get; set; }
}

public class BookListViewModel
{
    public string? Q { get; set; }
    public string? Category { get; set; }
    public List<string> Categories { get; set; } = new();
    public List<Book> Books { get; set; } = new();
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên"), StringLength(100)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu"), MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [DataType(DataType.Password), Display(Name = "Mật khẩu")]
    public string Password { get; set; } = "";

    [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp")]
    [DataType(DataType.Password), Display(Name = "Nhập lại mật khẩu")]
    public string ConfirmPassword { get; set; } = "";
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [DataType(DataType.Password), Display(Name = "Mật khẩu")]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class CardRegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 số, bắt đầu bằng 0")]
    [Display(Name = "Số điện thoại")]
    public string Phone { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
    [Display(Name = "Địa chỉ")]
    public string Address { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số CCCD")]
    [RegularExpression(@"^\d{12}$", ErrorMessage = "CCCD gồm 12 chữ số")]
    [Display(Name = "Số CCCD")]
    public string IdNumber { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn ngày sinh")]
    [DataType(DataType.Date), Display(Name = "Ngày sinh")]
    public DateTime? DateOfBirth { get; set; }
}


// ====================== Mượn / trả sách ======================

public class BorrowViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn ít nhất 1 cuốn sách")]
    [Display(Name = "Sách thứ nhất")]
    public int? Book1 { get; set; }

    [Display(Name = "Sách thứ hai (không bắt buộc)")]
    public int? Book2 { get; set; }

    [Display(Name = "Sách thứ ba (không bắt buộc)")]
    public int? Book3 { get; set; }

    public List<SelectListItem> Books { get; set; } = new();
}

public class MyLoansViewModel
{
    public LibraryCard Card { get; set; } = null!;
    public List<Loan> Loans { get; set; } = new();
}

public class ReturnItemInput
{
    public int LoanItemId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn tình trạng")]
    public BookStatus? Condition { get; set; }

    [StringLength(200, ErrorMessage = "Ghi chú tối đa 200 ký tự")]
    public string? Note { get; set; }
}

public class ReturnViewModel
{
    public int LoanId { get; set; }
    public Loan? Loan { get; set; }
    public List<ReturnItemInput> Items { get; set; } = new();
}

// ====================== Quản lý sách / đầu sách ======================

public class BookFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sách"), StringLength(200)]
    [Display(Name = "Tên sách")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập tác giả"), StringLength(150)]
    [Display(Name = "Tác giả")]
    public string Author { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập thể loại"), StringLength(80)]
    [Display(Name = "Thể loại")]
    public string Category { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập nhà xuất bản"), StringLength(150)]
    [Display(Name = "Nhà xuất bản")]
    public string Publisher { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập năm xuất bản")]
    [Range(1000, 2100, ErrorMessage = "Năm xuất bản không hợp lệ")]
    [Display(Name = "Năm xuất bản")]
    public int? Year { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số trang")]
    [Range(1, 10000, ErrorMessage = "Số trang phải từ 1 đến 10000")]
    [Display(Name = "Số trang")]
    public int? Pages { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá sách")]
    [Range(1000, 100000000, ErrorMessage = "Giá sách phải từ 1.000 đến 100.000.000 đồng")]
    [Display(Name = "Giá sách (đồng)")]
    public int? Price { get; set; }

    [StringLength(2000)]
    [Display(Name = "Giới thiệu")]
    public string? Description { get; set; }
}

public class CopyCreateViewModel
{
    public int BookId { get; set; }
    public string? BookTitle { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số lượng")]
    [Range(1, 100, ErrorMessage = "Mỗi lần thêm từ 1 đến 100 cuốn")]
    [Display(Name = "Số cuốn cần thêm")]
    public int? Count { get; set; }

    [StringLength(200)]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }
}

public class CopyEditViewModel
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string? BookTitle { get; set; }
    public string? ControlText { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn tình trạng")]
    [Display(Name = "Tình trạng")]
    public BookStatus? Status { get; set; }

    [StringLength(200)]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }
}

// ====================== Quản lý thẻ ======================

public class AdminCardCreateViewModel : CardRegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn thành viên")]
    [Display(Name = "Thành viên")]
    public int? UserId { get; set; }

    public List<SelectListItem> Users { get; set; } = new();
}

public class AdminCardEditViewModel : CardRegisterViewModel
{
    public int Id { get; set; }
    public string? CardNumber { get; set; }
    public string? UserName { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày hết hạn")]
    [DataType(DataType.Date), Display(Name = "Ngày hết hạn")]
    public DateTime? ExpiresAt { get; set; }

    [Display(Name = "Tình trạng sử dụng")]
    public bool IsActive { get; set; }
}

// ====================== Báo cáo ======================

public class LostDamagedRow
{
    public BookCopy Copy { get; set; } = null!;
    public LoanItem? Item { get; set; }
}

public class LostDamagedReportViewModel
{
    public int Year { get; set; }
    public List<LostDamagedRow> Rows { get; set; } = new();
    public int LostCount { get; set; }
    public int DamagedCount { get; set; }
    public int TotalCompensation { get; set; }
    public int UnpaidCompensation { get; set; }
}

public class LiquidationRow
{
    public BookCopy Copy { get; set; } = null!;
    public LoanItem? Item { get; set; } // lần mượn gây mất / hỏng gần nhất (nếu có)
}

public class LiquidationReportViewModel
{
    public int Year { get; set; }
    public List<LiquidationRow> Candidates { get; set; } = new(); // mất / hỏng, chờ thanh lý
    public List<LiquidationRow> Liquidated { get; set; } = new(); // đã thanh lý trong năm
    public int LiquidatedValue { get; set; } // tổng giá trị sách đã thanh lý (vnd)
    public int CandidateValue { get; set; }  // tổng giá trị sách chờ thanh lý (vnd)
}

// ====================== Theo dõi tình trạng sách ======================

public class TrackingRow
{
    public BookCopy Copy { get; set; } = null!;
    public Loan? ActiveLoan { get; set; } // phiếu mượn đang giữ cuốn này (nếu đang cho mượn)
}

public class TrackingViewModel
{
    public string? Q { get; set; }
    public BookStatus? Status { get; set; }
    public int AvailableCount { get; set; }
    public int BorrowedCount { get; set; }
    public int LostCount { get; set; }
    public int DamagedCount { get; set; }
    public int LiquidatedCount { get; set; }
    public List<TrackingRow> Rows { get; set; } = new();
    public List<Loan> OverdueLoans { get; set; } = new();
}

// ====================== Phí / bồi thường ======================

public class FeesViewModel
{
    public string Filter { get; set; } = "unpaid"; // unpaid | paid | all
    public string? Q { get; set; }
    public List<LoanItem> Items { get; set; } = new();
    public int TotalUnpaid { get; set; }
    public int TotalPaid { get; set; }
}

// ====================== Quản lý tài khoản & phân quyền ======================

public class AdminUserCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên"), StringLength(100)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu"), MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [DataType(DataType.Password), Display(Name = "Mật khẩu")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn quyền")]
    [Display(Name = "Quyền")]
    public string Role { get; set; } = "Member";
}

public class AdminUserEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên"), StringLength(100)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = "";

    [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [DataType(DataType.Password), Display(Name = "Mật khẩu mới (bỏ trống nếu không đổi)")]
    public string? NewPassword { get; set; }
}

public class AdminDashboardViewModel
{
    public int BookTitles { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public int BorrowedCopies { get; set; }
    public int LostDamagedCopies { get; set; }
    public int Members { get; set; }
    public int Cards { get; set; }
    public int ActiveLoans { get; set; }
    public int OverdueLoans { get; set; }
    public int UnpaidFees { get; set; }
}
