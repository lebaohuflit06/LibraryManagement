using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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

// ============================ sprint 2 ====================================================================
public class BorrowViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn ít nhất 1 cuốn sách")]
    [Display(Name = "Sách thứ nhất")]
    public int? Book1 { get; set; }
    [Display(Name = "Sách thứ hai")]
    public int? Book2 { get; set; }
    [Display(Name = "Sách thứ ba")]
    public int? Book3 { get; set; }

    [BindNever] // danh sách cho dropdown, ko bind từ form
    public List<SelectListItem> Books { get; set; } = new();
}

// tình trạng của 1 cuốn khi trả
public class ReturnItemInput
{
    public int LoanItemId { get; set; }
    [Required(ErrorMessage = "Vui lòng chọn tình trạng")]
    public BookStatus? Condition { get; set; } // available, lost, damaged
    [StringLength(300, ErrorMessage = "Ghi chú tối đa 300 ký tự")]
    public string? Note { get; set; }
}

// trả cả phiếu mượn, mỗi cuốn 1 tình trạng
public class ReturnViewModel
{
    public int LoanId { get; set; }
    public List<ReturnItemInput> Items { get; set; } = new();
    [BindNever]
    public Loan? Loan { get; set; } // để hiển thị thông tin phiếu mượn
}

// trang "Sách của tôi"

public class MyLoansViewModel
{
    public LibraryCard Card { get; set; } = null!;
    public List<Loan> Loans { get; set; } = new();
    public int UnpaidFees { get; set; } // tổng phí chưa thanh toán (sprint 3)
}

// form thêm / sửa MÃ SÁCH (admin). Số lượng cuốn quản lý ở phần "Đầu sách"
public class BookFormViewModel
{
    public int Id { get; set; } // 0 = thêm mới , >0 = sửa
    [Required(ErrorMessage = "Vui lòng nhập tên sách"), StringLength(200, ErrorMessage = "Tối đa 200 ký tự")]
    [Display(Name = "Tên sách")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập tên tác giả"), StringLength(100, ErrorMessage = "Tối đa 100 ký tự")]
    [Display(Name = "Tác giả")]
    public string Author { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập thể loại"), StringLength(50, ErrorMessage = "Tối đa 50 ký tự")]
    [Display(Name = "Thể loại")]
    public string Category { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập nhà xuất bản"), StringLength(100, ErrorMessage = "Tối đa 100 ký tự")]
    [Display(Name = "Nhà xuất bản")]
    public string Publisher { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập năm xuất bản")]
    [Range(1000, 2100, ErrorMessage = "Năm xuất bản không hợp lệ")]
    [Display(Name = "Năm xuất bản")]
    public int? Year { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số trang")]
    [Range(1, 10000, ErrorMessage = "Số trang từ 1 - 10000")]
    [Display(Name = "Số trang")]
    public int? Pages { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá sách (VNĐ)")]
    [Range(0, 10000000, ErrorMessage = "Giá sách từ 0 - 10.000.000 VNĐ")]
    [Display(Name = "Giá sách (VNĐ)")]
    public int? Price { get; set; }

    [StringLength(2000, ErrorMessage = "Tối đa 2000 ký tự")]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }
}

// admin thêm đầu sách (có thể thêm nhiều cuốn một lúc, ControlNo tự tăng)
public class CopyCreateViewModel
{
    public int BookId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số lượng cuốn sách")]
    [Range(1, 100, ErrorMessage = "Mỗi lần thêm từ 1 - 100 cuốn")]
    [Display(Name = "Số cuốn cần thêm")]
    public int? Count { get; set; }

    [StringLength(300, ErrorMessage = "Ghi chú tối đa 300 ký tự")]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    [BindNever] public string? BookTitle { get; set; } // để hiển thị tên sách khi thêm cuốn
}

// admin sửa đầu sách
public class CopyEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn tình trạng")]
    [Display(Name = "Tình trạng")]
    public BookStatus? Status { get; set; }

    [StringLength(300, ErrorMessage = "Ghi chú tối đa 300 ký tự")]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    [BindNever] public int BookId { get; set; }
    [BindNever] public string? BookTitle { get; set; } // để hiển thị tên sách khi sửa cuốn
    [BindNever] public string? ControlText { get; set; }
}

// admin cấp thẻ cho một thành viên đã có tài khoản
public class AdminCardCreateViewModel : CardRegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn thành viên")]
    [Display(Name = "Thành viên")]
    public int? UserId { get; set; }

    [BindNever]
    public List<SelectListItem> Users { get; set; } = new();
}

// admin sửa thẻ
public class AdminCardEditViewModel : CardRegisterViewModel
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Vui lòng chọn ngày hết hạn")]
    [DataType(DataType.Date), Display(Name = "Ngày hết hạn")]
    public DateTime? ExpiresAt { get; set; }

    [Display(Name = "Tình trạng sử dụng")]
    public bool IsActive { get; set; }

    [BindNever] public string? CardNumber { get; set; } // chỉ hiển thị
    [BindNever] public string? UserName { get; set; }
}

// báo cáo sách mất hoặc hỏng theo năm
public class LostDamagedRow
{
    public BookCopy Copy { get; set; } = null!;
    public LoanItem? Item { get; set; }
    public BookStatus EventStatus { get; set; } // mất hay hỏng (tại thời điểm xảy ra)
    public DateTime EventAt { get; set; }
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