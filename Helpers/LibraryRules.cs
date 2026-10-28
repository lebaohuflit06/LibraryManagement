using System.Globalization;
using LibraryManagement.Models;

namespace LibraryManagement.Helpers
{
    public static class AppRoles
    {
        public const string Admin = "Admin";         // quản trị viên: toàn quyền
        public const string Librarian = "Librarian"; // thủ thư: nghiệp vụ hằng ngày, không quản lý phân quyền
        public const string Member = "Member";       // thành viên (độc giả)

        // dùng cho [Authorize(Roles = AppRoles.Staff)]: Admin HOẶC Librarian
        public const string Staff = Admin + "," + Librarian;

        public static readonly string[] All = { Admin, Librarian, Member };

        public static bool IsStaff(string? role) => role == Admin || role == Librarian;

        public static string Text(string? role) => role switch
        {
            Admin => "Quản trị viên",
            Librarian => "Thủ thư",
            Member => "Thành viên",
            _ => role ?? "-"
        };

        // màu nhãn vai trò (định nghĩa trong site.css)
        public static string Css(string? role) => role switch
        {
            Admin => "b-admin",
            Librarian => "b-info",
            _ => "b-muted"
        };
    }

    // các quy định của thư viện, ví dụ: số lượng sách mượn tối đa, thời hạn mượn tối đa, phí bồi thường...
    public static class LibraryRules
    {
        // nếu muốn đổi quy định thì chỉ cần đổi ở đây
        public const int LoanDays = 15; // thời hạn mượn tối đa, tính theo ngày
        public const int MaxBooksPerLoan = 3; // số lượng sách mượn tối đa trong 1 lần mượn
        public const int DamagedPercent = 50; // bồi thường 50% giá sách hư hỏng
        public const int LostPercent = 100; // bồi thường 100% giá sách bị mất

        // ---- sprint 3 ----
        public const int LateFeePerDay = 1000; // phí trả muộn: đồng / cuốn / ngày (tối đa bằng giá sách)
        public const int OldBookYears = 10; // sách xuất bản từ ngần này năm trở lên được đề xuất thanh lý (sách cũ)
        public const int WornLoanCount = 15; // sách đã được mượn từ ngần này lượt trở lên được đề xuất thanh lý (cũ rách)

        public static int CalculateCompensation(BookStatus status, int bookPrice)
        {
            switch (status)
            {
                case BookStatus.Damaged: return bookPrice * DamagedPercent / 100;
                case BookStatus.Lost: return bookPrice * LostPercent / 100;
                default: return 0;
            }
        }

        // số ngày trả muộn (0 nếu trả đúng hạn)
        public static int LateDays(DateTime dueDate, DateTime returnedAt) => Math.Max(0, (returnedAt.Date - dueDate.Date).Days);

        // phí trả muộn của 1 cuốn: số ngày muộn × đơn giá, nhưng không vượt quá giá sách
        public static int LateFeeForItem(int lateDays, int bookPrice) => (int)Math.Min((long)lateDays * LateFeePerDay, bookPrice);

        public static string StatusText(BookStatus? s)
        {
            switch (s)
            {
                case BookStatus.Available: return "Còn";
                case BookStatus.Borrowed: return "Cho mượn";
                case BookStatus.Lost: return "Mất";
                case BookStatus.Damaged: return "Hỏng";
                case BookStatus.Disposed: return "Đã thanh lý";
                default: return "-";
            }
        }

        // màu tình trạng (định nghĩa trong site.css)
        public static string StatusCss(BookStatus? s)
        {
            switch (s)
            {
                case BookStatus.Available: return "b-ok";
                case BookStatus.Borrowed: return "b-warn";
                case BookStatus.Disposed: return "b-muted";
                default: return "b-bad";
            }
        }

        // tự định dạng tiền để ko phụ thuộc vào ngôn ngữ cài đặt của máy
        public static string Money(int amount) => amount.ToString("N0", CultureInfo.InvariantCulture).Replace(',', '.') + "đ";


        // tạo số thẻ: TV + 2 số cuối của năm + ID 5 chữ số, ví dụ: TV2300001
        public static string MakeCardNumber(int cardId) => $"TV{DateTime.Now:yy}{cardId:D5}";
    }
}
