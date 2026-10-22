using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.ComponentModel.DataAnnotations;

namespace SWP_SportCenter.Filters;

// FIX B19: giữ mặc định 10, chặn pageSize ngoài 1..500 và pageIndex không hợp lệ.
public sealed class PaginationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // FIX B13: Range của float cần loại NaN/Infinity dù binder có chấp nhận.
        if (context.ActionArguments.Values.OfType<SWP_SportCenter.Service.TrainingResult.Request.TrainingResultRequest>()
            .Any(r => !float.IsFinite(r.PerformanceScore) || r.PerformanceScore < 0 || r.PerformanceScore > 10))
        {
            context.Result = new BadRequestObjectResult(new { message = "PerformanceScore phải từ 0 đến 10." });
            return;
        }
        var args = context.ActionArguments;
        if ((args.TryGetValue("pageSize", out var size) && size is int s && (s < 1 || s > 500)) ||
            (args.TryGetValue("pageIndex", out var index) && index is int i &&
             (i < 1 || (args.TryGetValue("pageSize", out var ps) && ps is int p && (long)(i - 1) * p > int.MaxValue))))
            context.Result = new BadRequestObjectResult(new { message = "pageSize phải từ 1 đến 500; pageIndex phải dương và không vượt giới hạn phân trang." });
    }
    public void OnActionExecuted(ActionExecutedContext context) { }
}
