using System.Data;
using LibraryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Data;

// EnsureCreated() chỉ tạo CSDL khi chưa có, KHÔNG thêm bảng/cột mới vào CSDL cũ.
// Lớp này nâng cấp CSDL library.db đã dùng ở sprint 1-2 lên sprint 3 mà không mất dữ liệu.
// Chạy nhiều lần vẫn an toàn (idempotent). CSDL tạo mới thì mọi thứ đã có sẵn nên không làm gì.
public static class DbUpgrader
{
    public static void Upgrade(AppDbContext db)
    {
        // --- cột mới của bảng Users ---
        AddColumnIfMissing(db, "Users", "IsLocked", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(db, "Users", "LockReason", "TEXT NULL");
        AddColumnIfMissing(db, "Users", "LastLoginAt", "TEXT NULL");
        AddColumnIfMissing(db, "Users", "MustChangePassword", "INTEGER NOT NULL DEFAULT 0");

        // --- bảng mới ---
        Exec(db, """
            CREATE TABLE IF NOT EXISTS "Fees" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Fees" PRIMARY KEY AUTOINCREMENT,
                "CardId" INTEGER NOT NULL,
                "LoanId" INTEGER NULL,
                "LoanItemId" INTEGER NULL,
                "Type" INTEGER NOT NULL,
                "Amount" INTEGER NOT NULL,
                "Description" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "Status" INTEGER NOT NULL,
                "ProcessedAt" TEXT NULL,
                "Method" INTEGER NULL,
                "ReceiptNo" TEXT NULL,
                "ProcessedBy" TEXT NULL,
                "Note" TEXT NULL,
                CONSTRAINT "FK_Fees_Cards_CardId" FOREIGN KEY ("CardId") REFERENCES "Cards" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_Fees_Loans_LoanId" FOREIGN KEY ("LoanId") REFERENCES "Loans" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_Fees_LoanItems_LoanItemId" FOREIGN KEY ("LoanItemId") REFERENCES "LoanItems" ("Id") ON DELETE RESTRICT
            )
            """);
        Exec(db, """CREATE INDEX IF NOT EXISTS "IX_Fees_CardId" ON "Fees" ("CardId")""");
        Exec(db, """CREATE INDEX IF NOT EXISTS "IX_Fees_LoanId" ON "Fees" ("LoanId")""");
        Exec(db, """CREATE INDEX IF NOT EXISTS "IX_Fees_LoanItemId" ON "Fees" ("LoanItemId")""");

        Exec(db, """
            CREATE TABLE IF NOT EXISTS "CopyLogs" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_CopyLogs" PRIMARY KEY AUTOINCREMENT,
                "CopyId" INTEGER NOT NULL,
                "At" TEXT NOT NULL,
                "FromStatus" INTEGER NULL,
                "ToStatus" INTEGER NOT NULL,
                "Action" TEXT NOT NULL,
                "Actor" TEXT NOT NULL,
                "LoanId" INTEGER NULL,
                "Note" TEXT NULL,
                CONSTRAINT "FK_CopyLogs_BookCopies_CopyId" FOREIGN KEY ("CopyId") REFERENCES "BookCopies" ("Id") ON DELETE CASCADE
            )
            """);
        Exec(db, """CREATE INDEX IF NOT EXISTS "IX_CopyLogs_CopyId" ON "CopyLogs" ("CopyId")""");

        Exec(db, """
            CREATE TABLE IF NOT EXISTS "DisposalReports" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_DisposalReports" PRIMARY KEY AUTOINCREMENT,
                "Code" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "CreatedBy" TEXT NOT NULL,
                "Note" TEXT NULL,
                "RecoveredAmount" INTEGER NOT NULL
            )
            """);
        Exec(db, """CREATE UNIQUE INDEX IF NOT EXISTS "IX_DisposalReports_Code" ON "DisposalReports" ("Code")""");

        Exec(db, """
            CREATE TABLE IF NOT EXISTS "DisposalItems" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_DisposalItems" PRIMARY KEY AUTOINCREMENT,
                "ReportId" INTEGER NOT NULL,
                "CopyId" INTEGER NOT NULL,
                "Reason" INTEGER NOT NULL,
                "Method" INTEGER NOT NULL,
                "BookValue" INTEGER NOT NULL,
                "Note" TEXT NULL,
                CONSTRAINT "FK_DisposalItems_DisposalReports_ReportId" FOREIGN KEY ("ReportId") REFERENCES "DisposalReports" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_DisposalItems_BookCopies_CopyId" FOREIGN KEY ("CopyId") REFERENCES "BookCopies" ("Id") ON DELETE RESTRICT
            )
            """);
        Exec(db, """CREATE INDEX IF NOT EXISTS "IX_DisposalItems_ReportId" ON "DisposalItems" ("ReportId")""");
        Exec(db, """CREATE INDEX IF NOT EXISTS "IX_DisposalItems_CopyId" ON "DisposalItems" ("CopyId")""");

        Exec(db, """
            CREATE TABLE IF NOT EXISTS "AuditLogs" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_AuditLogs" PRIMARY KEY AUTOINCREMENT,
                "At" TEXT NOT NULL,
                "Actor" TEXT NOT NULL,
                "Action" TEXT NOT NULL,
                "Target" TEXT NOT NULL,
                "Detail" TEXT NULL
            )
            """);

        // --- dữ liệu ban đầu cho các bảng mới ---
        BackfillCopyLogs(db);
        BackfillFees(db);
    }

    // mỗi cuốn sách chưa có nhật ký thì tạo 1 dòng "Dữ liệu ban đầu" theo tình trạng hiện tại
    private static void BackfillCopyLogs(AppDbContext db)
    {
        Exec(db, """
            INSERT INTO "CopyLogs" ("CopyId", "At", "FromStatus", "ToStatus", "Action", "Actor", "LoanId", "Note")
            SELECT c."Id", COALESCE(c."StatusChangedAt", datetime('now', 'localtime')), NULL, c."Status",
                   'Dữ liệu ban đầu', 'Hệ thống', NULL, NULL
            FROM "BookCopies" c
            WHERE NOT EXISTS (SELECT 1 FROM "CopyLogs" l WHERE l."CopyId" = c."Id")
            """);
    }

    // các khoản bồi thường ghi nhận ở sprint 2 (nằm trong LoanItems) được chuyển vào sổ phí
    private static void BackfillFees(AppDbContext db)
    {
        var items = db.LoanItems
            .Include(i => i.Loan)
            .Include(i => i.Copy).ThenInclude(c => c!.Book)
            .Where(i => i.Compensation > 0 && !db.Fees.Any(f => f.LoanItemId == i.Id))
            .ToList();
        if (items.Count == 0) return;

        foreach (var it in items)
        {
            var at = it.Loan?.ReturnedAt ?? it.Loan?.BorrowedAt ?? DateTime.Now;
            var title = it.Copy?.Book?.Title ?? "";
            var control = it.Copy?.ControlText ?? "";
            var lost = it.ReturnStatus == BookStatus.Lost;
            db.Fees.Add(new Fee
            {
                CardId = it.Loan!.CardId,
                LoanId = it.LoanId,
                LoanItemId = it.Id,
                Type = lost ? FeeType.Lost : FeeType.Damaged,
                Amount = it.Compensation,
                Description = $"Bồi thường {(lost ? "mất" : "hỏng")}: {title} (KS {control})",
                CreatedAt = at,
                Status = it.CompensationPaid ? FeeStatus.Paid : FeeStatus.Unpaid,
                ProcessedAt = it.CompensationPaid ? at : null,
                Method = it.CompensationPaid ? PaymentMethod.Cash : null,
                ProcessedBy = it.CompensationPaid ? "Dữ liệu cũ" : null
            });
        }
        db.SaveChanges();
    }

    private static void Exec(AppDbContext db, string sql) => db.Database.ExecuteSqlRaw(sql);

    private static void AddColumnIfMissing(AppDbContext db, string table, string column, string definition)
    {
        var conn = db.Database.GetDbConnection();
        var wasClosed = conn.State != ConnectionState.Open;
        if (wasClosed) conn.Open();
        try
        {
            var exists = false;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"PRAGMA table_info(\"{table}\")";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase)) { exists = true; break; }
            }
            if (!exists)
            {
                using var alter = conn.CreateCommand();
                alter.CommandText = $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {definition}";
                alter.ExecuteNonQuery();
            }
        }
        finally
        {
            if (wasClosed) conn.Close();
        }
    }
}
