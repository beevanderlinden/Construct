using System.Text.Json.Serialization;

namespace Construct.Domain.Entities
{
    /// <summary>
    /// Een dakplaat, afgeleid van <see cref="PlaatEntity"/>.
    /// Standaard vrij opgelegd op Boven en Onder (spanning langs de Breedte-as).
    /// Alternatief: vrij opgelegd op Links en Rechts (spanning langs de Lengte-as).
    /// </summary>
    public class DakplaatEntity : PlaatEntity
    {
        public override double Lengte { get; set; } = 3000;
        public override double Breedte { get; set; } = 2000;

        public DakplaatRichting Richting { get; set; } = DakplaatRichting.BovenOnder;

        public override void Bijwerken()
        {
            // Stel oplegging in op basis van gekozen richting
            PasOpleggingPresetToe(Richting == DakplaatRichting.LinksRechts
                ? PlaatOpleggingPreset.VrijOpgelegd2Randen
                : PlaatOpleggingPreset.VrijOpgelegdBreedte);

            base.Bijwerken();
        }

        public override void Init(ProjectInfoEntity projectInfo)
        {
            if (string.IsNullOrEmpty(Merk))
                Merk = "DP-01";

            base.Init(projectInfo);
        }
    }

    /// <summary>
    /// De spanrichting van de dakplaat.
    /// </summary>
    public enum DakplaatRichting
    {
        /// <summary>Vrij opgelegd op Boven en Onder (spanning langs Breedte-as). Standaard.</summary>
        BovenOnder,

        /// <summary>Vrij opgelegd op Links en Rechts (spanning langs Lengte-as).</summary>
        LinksRechts,
    }
}
