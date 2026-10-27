using System.ComponentModel.DataAnnotations;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Accounts;

public class Request
{
    public class UpdateAccountRequest
    {
        // FIX B50: null giữ role hiện tại; chuyển role chỉ dành cho Manager.
        [EnumDataType(typeof(AccountRole))]
        public AccountRole? Role { get; set; }
        [Required(ErrorMessage = "Username không được để trống")]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        // CODE CŨ: [Required(ErrorMessage = "FullName không được để trống")]
        // FIX: chỉ yêu cầu thông tin hồ sơ khi role/profile cần dùng, kiểm tra ở Service.
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        // FIX 2026-10-22: nhận phone khi cập nhật account/profile.
        // CODE CŨ: [Required(ErrorMessage = "Phone không được để trống")]
        // FIX: Account nhân sự chưa có profile không cần phone.
        [MaxLength(20, ErrorMessage = "Phone tối đa 20 ký tự")]
        public string Phone { get; set; } = string.Empty;

        public AccountStatus Status { get; set; }


    }
    
    
    public class CreateAccountRequest
    {
        [Required(ErrorMessage = "Username không được để trống")]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        // CODE CŨ: [Required(ErrorMessage = "FullName không được để trống")]
        // FIX: Member vẫn bắt buộc FullName trong Service; nhân sự nhập ở bước tạo hồ sơ.
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        // FIX 2026-10-22: nhận phone khi tạo account/profile.
        // CODE CŨ: [Required(ErrorMessage = "Phone không được để trống")]
        // FIX: chỉ Member cần phone khi tạo Account.
        [MaxLength(20, ErrorMessage = "Phone tối đa 20 ký tự")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public AccountRole Role { get; set; }

        public AccountStatus Status { get; set; } = AccountStatus.Active;
    }


}
