using SWP_SportCenter.Repository.Abstraction;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Repository.Entity;

public class Account: BaseEntity, IAuditableEntity
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public AccountRole Role { get; set; }

    public AccountStatus Status { get; set; }

    // Navigation properties
    public Member? Member { get; set; }

    public Coach? Coach { get; set; }

    public Receptionist? Receptionist { get; set; }

    public CenterManager? CenterManager { get; set; }

    public ICollection<Notification> Notifications { get; set; }
        = new List<Notification>();

    public ICollection<AuditLog> AuditLogs { get; set; }
        = new List<AuditLog>();
    
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}