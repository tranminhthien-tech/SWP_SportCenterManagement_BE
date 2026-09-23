using System;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Room;

public class Response
{
    public class RoomResponse
    {
        public Guid Id { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public RoomStatus Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}