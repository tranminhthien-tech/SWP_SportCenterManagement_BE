using System.ComponentModel.DataAnnotations;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Accounts;

public class Request
{
    public class UpdateAccountRequest
    {
        [Required(ErrorMessage = "Username không được để trống")]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

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

        [Required(ErrorMessage = "FullName không được để trống")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public AccountRole Role { get; set; }

        public AccountStatus Status { get; set; } = AccountStatus.Active;
    }


}
