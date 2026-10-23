using System;
using System.ComponentModel.DataAnnotations;

namespace SWP_SportCenter.Service.AuditLog;

public class Request
{
    public class CreateAuditLogRequest
    {
        // FIX B35: giữ AccountId (optional) trong DTO để Swagger hiển thị đúng schema và FE gửi kèm không bị cảnh báo schema.
        // Server luôn bỏ qua giá trị này và lấy danh tính thực từ JWT token để đảm bảo an toàn.
        public Guid? AccountId { get; set; }

        [Required(ErrorMessage = "Loại hành động không được để trống")]
        [MaxLength(100)]
        public string ActionType { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }
}
