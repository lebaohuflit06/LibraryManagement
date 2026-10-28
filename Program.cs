using System.Security.Claims;
using LibraryManagement.Data;
using LibraryManagement.Filters;
using LibraryManagement.Helpers;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

// Tự tìm thư mục project (chứa .csproj) để luôn nạp được wwwroot, dù chạy từ bin\Debug hay dotnet run
var dir = new DirectoryInfo(AppContext.BaseDirectory);
while (dir != null && !File.Exists(Path.Combine(dir.FullName, "LibraryManagement.csproj"))) dir = dir.Parent;
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = dir?.FullName ?? AppContext.BaseDirectory
});

builder.Services.AddControllersWithViews(o => o.Filters.Add<RequirePasswordChangeFilter>());
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<FeeService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.AccessDeniedPath = "/Account/AccessDenied";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);

        // Mỗi request kiểm tra lại tài khoản trong CSDL: tài khoản bị khóa / bị xóa thì đăng xuất ngay,
        // đổi vai trò (phân quyền) hoặc tên có hiệu lực ngay mà không cần đăng nhập lại.
        o.Events.OnValidatePrincipal = async ctx =>
        {
            var principal = ctx.Principal;
            if (principal == null || !int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var uid))
            {
                ctx.RejectPrincipal();
                await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == uid);
            if (user == null || user.IsLocked)
            {
                ctx.RejectPrincipal();
                await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            var changed = principal.FindFirstValue(ClaimTypes.Role) != user.Role
                || principal.FindFirstValue(ClaimTypes.Name) != user.FullName
                || principal.FindFirstValue(ClaimTypes.Email) != user.Email
                || (principal.FindFirstValue(AuthHelper.MustChangePasswordClaim) == "1") != user.MustChangePassword;
            if (changed)
            {
                ctx.ReplacePrincipal(AuthHelper.BuildPrincipal(user));
                ctx.ShouldRenew = true;
            }
        };
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    DbUpgrader.Upgrade(db);   // thêm bảng/cột sprint 3 vào CSDL cũ (nếu cần)
    DbSeeder.SeedAdmin(db); // tạo tài khoản admin mặc định nếu chưa có
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
