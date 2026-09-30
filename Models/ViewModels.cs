using System.ComponentModel.DataAnnotations;

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
