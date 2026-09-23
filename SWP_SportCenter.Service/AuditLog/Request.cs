using System;
using System.ComponentModel.DataAnnotations;

namespace SWP_SportCenter.Service.AuditLog;

public class Request
{
    public class CreateAuditLogRequest
    {
        [Required(ErrorMessage = "Vui lòng cung cấp Account ID")]
        public Guid AccountId { get; set; }

        [Required(ErrorMessage = "Loại hành động không được để trống")]
        [MaxLength(100)]
        public string ActionType { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }
}