using SWP_SportCenter.Repository.Abstraction;

namespace SWP_SportCenter.Repository.Entity;

public class Receptionist: BaseEntity, IAuditableEntity
{
    public string FullName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string WorkingShift { get; set; } = string.Empty;

    // Navigation
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;

    public ICollection<Invoice> Invoices { get; set; }
        = new List<Invoice>();
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}