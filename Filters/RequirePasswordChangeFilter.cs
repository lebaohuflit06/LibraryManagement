using LibraryManagement.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LibraryManagement.Filters;

// sau khi admin đặt lại mật khẩu, người dùng phải đổi mật khẩu mới trước khi dùng tiếp
public class RequirePasswordChangeFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true) return;
        if (user.FindFirst(AuthHelper.MustChangePasswordClaim)?.Value != "1") return;

        var controller = context.RouteData.Values["controller"]?.ToString();
        if (string.Equals(controller, "Account", StringComparison.OrdinalIgnoreCase)) return; // đổi mật khẩu / đăng xuất

        context.Result = new RedirectToActionResult("ChangePassword", "Account", null);
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
