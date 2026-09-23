using System;
using System.ComponentModel.DataAnnotations;

namespace SWP_SportCenter.Service.CenterManager;

public class Request
{
    public class CreateCenterManagerRequest
    {
        [Required(ErrorMessage = "Vui lòng cung cấp Account ID")]
        public Guid AccountId { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;
    }

    public class UpdateCenterManagerRequest
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;
    }
}