using Construct.Domain.Entities;

namespace Construct.Domain.Common
{
    /// <summary>
    /// Concrete implementatie van EntityReference voor StrookEntity.
    /// Beheert referenties naar stroken (delen van een plaat/bordes).
    /// 
    /// Dit is optioneel, maar handig voor toekomstige cross-strook referenties.
    /// </summary>
    public class StrookReference : EntityReference<StrookEntity>
    {
        /// <summary>
        /// Haalt de Guid van een strook op via de Id-property (geconverteerd van string naar Guid).
        /// </summary>
        protected override Guid GetEntityId(StrookEntity entity)
            => Guid.Parse(entity.Id);
    }
}
