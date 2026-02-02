using Construct.Domain.Common.Interfaces;

namespace Construct.Domain.Common
{
    public abstract class BaseEntity : IEntity
    {
        public int Id { get; set; }
    }
}
