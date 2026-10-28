using LibraryManagement.Models;

namespace LibraryManagement.Helpers;

// chữ hiển thị + màu nhãn cho các enum của sprint 3
public static class Labels
{
    public static string FeeType(FeeType t) => t switch
    {
        Models.FeeType.Late => "Trả muộn",
        Models.FeeType.Damaged => "Bồi thường hỏng",
        Models.FeeType.Lost => "Bồi thường mất",
        _ => "Phí khác"
    };

    public static string FeeStatus(FeeStatus s) => s switch
    {
        Models.FeeStatus.Paid => "Đã thu",
        Models.FeeStatus.Waived => "Đã miễn",
        _ => "Chưa thu"
    };

    public static string FeeStatusCss(FeeStatus s) => s switch
    {
        Models.FeeStatus.Paid => "b-ok",
        Models.FeeStatus.Waived => "b-muted",
        _ => "b-bad"
    };

    public static string Method(PaymentMethod? m) => m switch
    {
        PaymentMethod.Cash => "Tiền mặt",
        PaymentMethod.Transfer => "Chuyển khoản",
        _ => "-"
    };

    public static string DisposalReason(DisposalReason r) => r switch
    {
        Models.DisposalReason.Damaged => "Hỏng",
        Models.DisposalReason.Lost => "Mất",
        Models.DisposalReason.Worn => "Cũ rách (mượn nhiều)",
        _ => "Cũ (xuất bản lâu)"
    };

    public static string DisposalMethod(DisposalMethod m) => m switch
    {
        Models.DisposalMethod.Destroy => "Tiêu hủy",
        Models.DisposalMethod.Sell => "Bán thanh lý",
        Models.DisposalMethod.Donate => "Tặng / cho",
        _ => "Ghi giảm (sách mất)"
    };
}
