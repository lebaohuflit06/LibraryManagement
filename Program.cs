using LibraryManagement.Data;
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

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.AccessDeniedPath = "/Account/Login";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
