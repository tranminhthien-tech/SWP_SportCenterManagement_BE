namespace SWP_SportCenter.Service.Exceptions;

// FIX B6: lỗi xung đột nghiệp vụ tách biệt InvalidOperationException của hệ thống.
public sealed class ConflictException(string message) : Exception(message);
