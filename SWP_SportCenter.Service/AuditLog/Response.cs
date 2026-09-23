using System;

namespace SWP_SportCenter.Service.AuditLog;

public class Response
{
    public class AuditLogResponse
    {
        public Guid Id { get; set; }
        
        public Guid AccountId { get; set; }
        public string Username { get; set; } = string.Empty; // Join bảng Account để lấy Username
        
        public string ActionType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }
}