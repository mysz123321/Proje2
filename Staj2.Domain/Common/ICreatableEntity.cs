using System;

namespace Staj2.Domain.Common
{
    public interface ICreatableEntity
    {
        DateTime CreatedAt { get; set; }
        int? CreatedBy { get; set; }
    }
}