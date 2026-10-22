using System.ComponentModel.DataAnnotations;

namespace SWP_SportCenter.Service.Register;

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
        [Required(ErrorMessage = "Username không được để trống")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        public string Password { get; set; } = string.Empty;

        // FIX 2026-10-22: FullName phải được nhận ngay khi đăng ký để tạo Member profile.
        [Required(ErrorMessage = "FullName không được để trống")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;
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
