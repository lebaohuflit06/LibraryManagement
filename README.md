# Thư viện Sách Việt (ASP.NET Core MVC, .NET 8, SQLite)

Chạy: mở LibraryManagement.csproj bằng Visual Studio 2022 -> nhấn F5.
CSDL `library.db` được tự tạo cùng dữ liệu mẫu ở lần chạy đầu tiên.

## Sprint 3 (chức năng 14, 15, 16, 18, 19)

Chạy lần đầu, `DbUpgrader` tự thêm bảng/cột mới vào `library.db` cũ (không mất dữ liệu). Nếu muốn dữ liệu sạch, xóa `library.db*` rồi chạy lại.

Tài khoản mẫu (CSDL mới): `admin@gmail.com / Admin@123` (Quản trị viên), `thuthu@gmail.com / Thuthu@123` (Thủ thư).

| # | Chức năng | Vào ở đâu |
|---|-----------|-----------|
| 14 | Báo cáo thanh lý sách cũ/rác: đề xuất (mất, hỏng, mượn ≥ 15 lượt, xuất bản ≥ 10 năm), lập biên bản, báo cáo năm, in, xuất CSV | Báo cáo → Thanh lý |
| 15 | Phí: bồi thường mất/hỏng + phí trả muộn (1.000đ/cuốn/ngày) tự ghi khi trả sách; thu tiền, phiếu thu, miễn phí (Admin), phí khác, danh sách quá hạn; thành viên xem "Phí của tôi"; còn nợ phí thì không mượn được | Quản lý → Phí & bồi thường |
| 16 | Theo dõi tình trạng sách: tổng quan + lọc, người đang mượn, quá hạn, lịch sử từng cuốn | Quản lý → Theo dõi tình trạng sách |
| 18 | Tài khoản: tìm kiếm, tạo, sửa, khóa/mở khóa (đăng xuất ngay), đặt lại mật khẩu tạm, xóa; hồ sơ + đổi mật khẩu | Hệ thống → Tài khoản |
| 19 | Phân quyền 3 vai trò (Admin / Thủ thư / Thành viên), bảng quyền, đổi vai trò có ràng buộc, nhật ký thao tác | Hệ thống → Phân quyền (Admin) |

Các quy định chỉnh ở `Helpers/LibraryRules.cs`.
