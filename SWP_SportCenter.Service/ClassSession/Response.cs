using System;

namespace SWP_SportCenter.Service.ClassSession;

public class Response
{
    public class ClassSessionResponse
    {
        public Guid Id { get; set; }
        
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        
        public Guid RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
}