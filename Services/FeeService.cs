using LibraryManagement.Data;
using LibraryManagement.Helpers;
using LibraryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Services;

public class SettleResult
{
    public bool CardReactivated { get; set; }      // thẻ được mở lại vì hết nợ bồi thường
    public bool CardStillBlocked { get; set; }     // thẻ vẫn bị khóa vì còn khoản bồi thường khác
}

// nghiệp vụ phí: tạo phí khi trả sách, thu tiền, miễn phí
public class FeeService
{
    private readonly AppDbContext _db;
    public FeeService(AppDbContext db) => _db = db;

    // tạo phí khi trả sách: mỗi cuốn mất/hỏng 1 khoản bồi thường + 1 khoản phí trả muộn cho cả phiếu.
    // Chỉ Add vào DbContext, nơi gọi SaveChanges. Trả về các khoản phí vừa tạo.
    public List<Fee> CreateReturnFees(Loan loan, DateTime returnedAt)
    {
        var fees = new List<Fee>();

        foreach (var it in loan.Items.Where(i => i.Compensation > 0))
        {
            var lost = it.ReturnStatus == BookStatus.Lost;
            fees.Add(new Fee
            {
                CardId = loan.CardId,
                LoanId = loan.Id,
                LoanItem = it,
                Type = lost ? FeeType.Lost : FeeType.Damaged,
                Amount = it.Compensation,
                Description = $"Bồi thường {(lost ? "mất" : "hỏng")}: {it.Copy?.Book?.Title} (KS {it.Copy?.ControlText})",
                CreatedAt = returnedAt
            });
        }

        // phí trả muộn: sách bị mất đã tính bồi thường 100% nên không tính thêm phí muộn
        var lateDays = LibraryRules.LateDays(loan.DueDate, returnedAt);
        if (lateDays > 0)
        {
            var lateItems = loan.Items.Where(i => i.ReturnStatus != BookStatus.Lost).ToList();
            var total = lateItems.Sum(i => LibraryRules.LateFeeForItem(lateDays, i.Copy?.Book?.Price ?? 0));
            if (total > 0)
            {
                fees.Add(new Fee
                {
                    CardId = loan.CardId,
                    LoanId = loan.Id,
                    Type = FeeType.Late,
                    Amount = total,
                    Description = $"Trả muộn {lateDays} ngày, {lateItems.Count} cuốn × {LibraryRules.Money(LibraryRules.LateFeePerDay)}/ngày (phiếu #{loan.Id})",
                    CreatedAt = returnedAt
                });
            }
        }

        _db.Fees.AddRange(fees);
        return fees;
    }

    // phí trả muộn dự kiến của phiếu đang mượn nhưng đã quá hạn (chưa ghi vào sổ phí)
    public static int AccruedLateFee(Loan loan, DateTime now)
    {
        if (loan.ReturnedAt != null) return 0;
        var days = LibraryRules.LateDays(loan.DueDate, now);
        if (days <= 0) return 0;
        return loan.Items.Sum(i => LibraryRules.LateFeeForItem(days, i.Copy?.Book?.Price ?? 0));
    }

    // thu tiền một hoặc nhiều khoản phí cùng 1 phiếu thu. Trả về số phiếu thu
    public async Task<(string receiptNo, SettleResult result)> PayAsync(
        IReadOnlyList<Fee> fees, PaymentMethod method, string? note, string by)
    {
        var now = DateTime.Now;
        var receiptNo = $"PT{now:yyMMdd}-{fees.Min(f => f.Id):D5}";
        foreach (var f in fees)
        {
            f.Status = FeeStatus.Paid;
            f.ProcessedAt = now;
            f.Method = method;
            f.ReceiptNo = receiptNo;
            f.ProcessedBy = by;
            f.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        }
        var result = await SettleAsync(fees, waived: false);
        await _db.SaveChangesAsync();
        return (receiptNo, result);
    }

    public async Task<SettleResult> WaiveAsync(Fee fee, string reason, string by)
    {
        fee.Status = FeeStatus.Waived;
        fee.ProcessedAt = DateTime.Now;
        fee.ProcessedBy = by;
        fee.Note = reason.Trim();
        var result = await SettleAsync(new[] { fee }, waived: true);
        await _db.SaveChangesAsync();
        return result;
    }

    // đồng bộ LoanItem (sprint 2) và mở lại thẻ nếu hết nợ bồi thường. Không SaveChanges
    private async Task<SettleResult> SettleAsync(IEnumerable<Fee> fees, bool waived)
    {
        var result = new SettleResult();
        var feeList = fees.ToList();

        foreach (var f in feeList.Where(f => f.LoanItemId != null))
        {
            var item = await _db.LoanItems.FirstOrDefaultAsync(i => i.Id == f.LoanItemId);
            if (item == null) continue;
            item.CompensationPaid = true;
            if (waived) item.Compensation = 0; // miễn thì không còn khoản bồi thường
        }

        foreach (var cardId in feeList.Where(f => f.IsCompensation).Select(f => f.CardId).Distinct())
        {
            var ids = feeList.Select(f => f.Id).ToList();
            var otherOpen = await _db.Fees.AnyAsync(f => f.CardId == cardId && !ids.Contains(f.Id)
                && f.Status == FeeStatus.Unpaid && (f.Type == FeeType.Damaged || f.Type == FeeType.Lost));
            var card = await _db.Cards.FirstOrDefaultAsync(c => c.Id == cardId);
            if (card == null) continue;
            if (otherOpen) result.CardStillBlocked = !card.IsActive;
            else if (!card.IsActive) { card.IsActive = true; result.CardReactivated = true; }
        }
        return result;
    }
}
