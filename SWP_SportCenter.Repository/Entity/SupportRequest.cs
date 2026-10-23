using SWP_SportCenter.Repository.Abstraction;

namespace SWP_SportCenter.Repository.Entity;

// FIX B31: yêu cầu hỗ trợ tách khỏi thông báo, giữ người xử lý và thời gian.
public class SupportRequest : BaseEntity, IAuditableEntity
{
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public SupportRequestStatus Status { get; set; }
    public Guid? HandledByAccountId { get; set; }
    public Account? HandledByAccount { get; set; }
    public string? Resolution { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public enum SupportRequestStatus { Open = 1, InProgress = 2, Resolved = 3, Cancelled = 4 }
