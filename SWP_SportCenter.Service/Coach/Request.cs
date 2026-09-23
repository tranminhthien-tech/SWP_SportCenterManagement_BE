using System;
using System.ComponentModel.DataAnnotations;

namespace SWP_SportCenter.Service.Coach;

public class Request
{
    public class CreateCoachRequest
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

        [MaxLength(500)]
        public string? Avatar { get; set; }

        [MaxLength(200)]
        public string? Specialization { get; set; }

        [Range(0, 50, ErrorMessage = "Số năm kinh nghiệm không hợp lệ")]
        public int ExperienceYears { get; set; }
    }

    public class UpdateCoachRequest
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

        [MaxLength(500)]
        public string? Avatar { get; set; }

        [MaxLength(200)]
        public string? Specialization { get; set; }

        [Range(0, 50, ErrorMessage = "Số năm kinh nghiệm không hợp lệ")]
        public int ExperienceYears { get; set; }
    }
}