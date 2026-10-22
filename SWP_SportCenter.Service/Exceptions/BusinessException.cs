namespace SWP_SportCenter.Service.Exceptions;

// FIX 2026-10-22: phân biệt lỗi nghiệp vụ có thể hiển thị cho người dùng với lỗi hệ thống.
public sealed class BusinessException : Exception
{
    public BusinessException(string message) : base(message)
    {
    }
}
