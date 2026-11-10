# Thư viện Sách Việt (ASP.NET Core MVC, .NET 8, SQLite)

Chạy: mở `LibraryManagement.sln` bằng Visual Studio 2022 -> nhấn F5.
CSDL `library.db` được tự tạo cùng dữ liệu mẫu ở lần chạy đầu tiên
(nếu đã có file `library.db` cũ từ phiên bản trước thì hãy xóa đi để tạo lại đúng cấu trúc mới).

Tài khoản quản trị mặc định: `admin@gmail.com` / `Admin@123`

## Chức năng
Khách / thành viên: xem trang chủ, xem thông tin sách, tìm kiếm sách, đăng ký tài khoản, đăng nhập, đăng xuất,
đăng ký thẻ thư viện, xem thông tin thẻ, đăng ký mượn sách, trả sách.

Quản trị viên (menu "Quản trị"):
- Đăng nhập tài khoản Admin
- Quản lý đầu sách (mã sách + từng cuốn)
- Quản lý thẻ thư viện các thành viên
- Quản lý tài khoản thành viên (thêm, sửa, đặt lại mật khẩu, khóa / mở khóa, xóa)
- Quản lý phân quyền (Admin / Member)
- Theo dõi tình trạng sách (từng cuốn, người đang mượn, phiếu quá hạn)
- Xử lý các khoản phí (thu tiền bồi thường, mở lại thẻ)
- Báo cáo tình trạng sách và bồi thường (theo năm)
- Báo cáo thanh lý sách cũ / rách / mất (có nút thanh lý)
