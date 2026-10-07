using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Accounts;

public class Response
{
    public class AccountResponse
    {
        public Guid AccountId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        
        public string FullName { get; set; } = string.Empty;
        public AccountRole Role { get; set; }
        public AccountStatus Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
