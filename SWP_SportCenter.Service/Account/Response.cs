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

        // FIX 2026-10-22: trả phone để FE hiển thị ở trang Accounts.
        public string Phone { get; set; } = string.Empty;
        // FIX 2026-10-22: FE biết tài khoản đã có hồ sơ tương ứng hay chưa.
        public bool HasProfile { get; set; }
        public AccountRole Role { get; set; }
        public AccountStatus Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
