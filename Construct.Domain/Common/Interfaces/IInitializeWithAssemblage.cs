using Construct.Domain.Entities;

namespace Construct.Domain.Common.Interfaces
{
    public interface IInitializableWithAssemblage
    {
        void Init(AssemblageEntity parent);
    }

    public interface IInitializableWithSteekTrap
    {
        void Init(SteekTrapEntity trap);
    }
}
