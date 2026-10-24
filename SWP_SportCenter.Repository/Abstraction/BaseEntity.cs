using System;

namespace SWP_SportCenter.Repository.Abstraction;

public abstract class BaseEntity
{
    // FIX B35: sinh Guid ở ứng dụng trước khi Add để entityId trong log Added luôn là ID thật.
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool IsDeleted { get; set; }
}
