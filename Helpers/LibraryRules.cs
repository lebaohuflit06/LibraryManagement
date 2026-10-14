using System.Globalization;
using LibraryManagement.Models;

namespace LibraryManagement.Helpers
{
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Member = "Member";
    }

    // các quy định của thư viện, ví dụ: số lượng sách mượn tối đa, thời hạn mượn tối đa, phí bồi thường...
    public static class LibraryRules
    {
        // nếu muốn đổi quy định thì chỉ cần đổi ở đây
        public const int LoanDays = 15; // thời hạn mượn tối đa, tính theo ngày
        public const int MaxBooksPerLoan = 3; // số lượng sách mượn tối đa trong 1 lần mượn
        public const int DamagedPercent = 50; // bồi thường 50% giá sách hư hỏng
        public const int LostPercent = 100; // bồi thường 100% giá sách bị mất

        public static int CalculateCompensation(BookStatus status, int bookPrice)
        {
            switch (status)
            {
                case BookStatus.Damaged: return bookPrice * DamagedPercent / 100;
                case BookStatus.Lost: return bookPrice * LostPercent / 100;
                default: return 0;
            }
        }

        public static string StatusText(BookStatus? s)
        {
            switch (s)
            {
                case BookStatus.Available: return "Còn";
                case BookStatus.Borrowed: return "Cho mượn";
                case BookStatus.Lost: return "Mất";
                case BookStatus.Damaged: return "Hỏng";
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
                default: return "b-bad";
            }
        }

        // tự định dạng tiền để ko phụ thuộc vào ngôn ngữ cài đặt của máy
        public static string Money(int amount) => amount.ToString("N0", CultureInfo.InvariantCulture).Replace(',', '.') + "đ";


        // tạo số thẻ: TV + 2 số cuối của năm + ID 5 chữ số, ví dụ: TV2300001
        public static string MakeCardNumber(int cardId) => $"TV{DateTime.Now:yy}{cardId:D5}";
    }
}
