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
        // FIX B3 B17: chặn dữ liệu vượt độ dài DB ngay tại DTO.
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [MinLength(6), MaxLength(128)]
        public string Password { get; set; } = string.Empty;

        // FIX 2026-10-22: FullName phải được nhận ngay khi đăng ký để tạo Member profile.
        [Required(ErrorMessage = "FullName không được để trống")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        // FIX B3 B18: nhận dữ liệu thật từ form đăng ký, không OTP.
        [Required, RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Số điện thoại phải có 10 số và bắt đầu bằng 0.")]
        public string Phone { get; set; } = string.Empty;
        [SWP_SportCenter.Service.Validation.DateOfBirth]
        public DateTime Dob { get; set; }
        [MaxLength(20)]
        public string? Gender { get; set; }
        [MaxLength(500)]
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
