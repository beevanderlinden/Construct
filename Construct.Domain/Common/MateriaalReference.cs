using CommonLibrary.Models;

namespace Construct.Domain.Common
{
    /// <summary>
    /// Concrete implementatie van EntityReference voor BaseMateriaal.
    /// Beheert referenties naar materialen (Beton, Staal, Hout).
    /// </summary>
    public class MateriaalReference : EntityReference<BaseMateriaal>
    {
        /// <summary>
        /// Haalt de Guid van een materiaal op via de Id-property.
        /// </summary>
        protected override Guid GetEntityId(BaseMateriaal entity)
            => entity.Id;
    }
}
