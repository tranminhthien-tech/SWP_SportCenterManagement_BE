namespace SWP_SportCenter.Service.Athu;

public class Request
{
    public class LoginRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
    // Đăng ký Member
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // Thông tin hồ sơ Member
        public string FullName { get; set; } = string.Empty;
        public DateOnly? Dob { get; set; }
        public string? Gender { get; set; }
        public string? Phone { get; set; }
        public string? Avatar { get; set; }
        public string? TrainingGoal { get; set; }
    }

    // Gửi yêu cầu quên mật khẩu
    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    // Đặt lại mật khẩu bằng OTP
    public class ResetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}