using System;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Attendance;

public class Response
{
    public class AttendanceResponse
    {
        public Guid Id { get; set; }
        
        public Guid SessionId { get; set; }
        public DateTime SessionDate { get; set; }
        public TimeSpan SessionStartTime { get; set; }
        
        public Guid MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        
        public Guid RecordedBy { get; set; }
        
        public DateTimeOffset CheckInTime { get; set; }
        public AttendanceStatus Status { get; set; }
    }
}