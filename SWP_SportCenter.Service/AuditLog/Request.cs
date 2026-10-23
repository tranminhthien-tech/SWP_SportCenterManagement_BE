using System;
using System.ComponentModel.DataAnnotations;

namespace SWP_SportCenter.Service.AuditLog;

public class Request
{
    public class CreateAuditLogRequest
    {
        // CODE CŨ: [Required] public Guid AccountId { get; set; }
        // FIX B33: bỏ AccountId khỏi DTO, danh tính người ghi lấy từ JWT.

        [Required(ErrorMessage = "Loại hành động không được để trống")]
        [MaxLength(100)]
        public string ActionType { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }
}
