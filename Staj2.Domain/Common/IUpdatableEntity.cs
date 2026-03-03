using System;

namespace Staj2.Domain.Common
{
    public interface IUpdatableEntity
    {
        DateTime? UpdatedAt { get; set; }
        int? UpdatedBy { get; set; }
    }
}