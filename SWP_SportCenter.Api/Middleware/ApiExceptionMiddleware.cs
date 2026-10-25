using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SWP_SportCenter.Service.Exceptions;

namespace SWP_SportCenter.Middleware;

// FIX B6: trả lỗi JSON thống nhất; chỉ log exception kỹ thuật tại server.
public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var status = ex switch
            {
                BusinessException or ArgumentException or System.ComponentModel.DataAnnotations.ValidationException => 400,
                ConflictException => 409,
                // FIX B22: lỗi dịch vụ ảnh ngoài trả 502 JSON, không xuất stack/provider secret.
                HttpRequestException => 502,
                UnauthorizedAccessException => context.User.Identity?.IsAuthenticated == true ? 403 : 401,
                KeyNotFoundException => 404,
                DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } => 409,
                // FIX: yêu cầu đồng thời xung đột transaction được phép thử lại.
                PostgresException { SqlState: PostgresErrorCodes.SerializationFailure } => 409,
                DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.SerializationFailure } } => 409,
                DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } } => 400,
                _ => 500
            };
            var message = status == 500 ? "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau."
                : ex is DbUpdateException or PostgresException ? "Dữ liệu bị trùng, bị tham chiếu hoặc đã được thay đổi bởi yêu cầu khác. Vui lòng kiểm tra và thử lại." : ex.Message;
            if (status == 500) logger.LogError(ex, "Unhandled API error {TraceId}", context.TraceIdentifier);
            context.Response.Clear();
            context.Response.StatusCode = status;
            var problem = new ProblemDetails { Status = status, Title = message, Detail = message };
            problem.Extensions["message"] = message;
            problem.Extensions["traceId"] = context.TraceIdentifier;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(problem,
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)));
        }
    }
}
