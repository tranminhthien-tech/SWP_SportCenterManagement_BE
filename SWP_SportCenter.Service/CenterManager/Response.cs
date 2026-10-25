using System;

namespace SWP_SportCenter.Service.CenterManager;

public class Response
{
    public class CenterManagerResponse
    {
        public string? Avatar { get; set; } // FIX B22
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
