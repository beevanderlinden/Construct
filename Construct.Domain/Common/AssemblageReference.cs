using Construct.Domain.Entities;

namespace Construct.Domain.Common
{
    /// <summary>
    /// Concrete implementatie van EntityReference voor AssemblageEntity.
    /// Beheert referenties naar assemblages (Ligger, Bordes, SteekTrap, Kolom).
    /// 
    /// Gebruikt voor:
    /// - Trap-referenties in BordesEntity
    /// - Cross-assemblage koppelungen
    /// </summary>
    public class AssemblageReference : EntityReference<AssemblageEntity>
    {
        /// <summary>
        /// Haalt de Guid van een assemblage op via de Id-property.
        /// </summary>
        protected override Guid GetEntityId(AssemblageEntity entity)
            => entity.Id;
    }
}
