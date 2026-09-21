using System;

namespace SWP_SportCenter.Repository.Abstraction;

public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
}