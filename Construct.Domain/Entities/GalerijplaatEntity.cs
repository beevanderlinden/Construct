namespace Construct.Domain.Entities
{
    /// <summary>
    /// Een galerijplaat, afgeleid van <see cref="PlaatEntity"/>.
    /// Standaard 6000×1500 mm, vrij opgelegd op Links en Rechts.
    /// Lijnlast Boven: gevelwandlast 4,5 kN/m (permanent, BG1).
    /// Lijnlast Onder: heklast 0,7 kN/m (veranderlijk, BG2).
    /// </summary>
    public class GalerijplaatEntity : PlaatEntity
    {
        public override double Lengte { get; set; } = 6000;
        public override double Breedte { get; set; } = 1500;

        public override void Bijwerken()
        {
            PasOpleggingPresetToe(PlaatOpleggingPreset.VrijOpgelegd2Randen);

            if (!Lijnlasten.Any(ll => ll.Naam == "gevel"))
                Lijnlasten.Add(new PlaatRandLijnlast
                {
                    Rand = PlaatRand.Boven,
                    Naam = "gevel",
                    MagnitudeA = -4.5,
                    MagnitudeB = -4.5,
                    BelastingGevalNr = 1,
                });

            if (!Lijnlasten.Any(ll => ll.Naam == "hek"))
                Lijnlasten.Add(new PlaatRandLijnlast
                {
                    Rand = PlaatRand.Onder,
                    Naam = "hek",
                    MagnitudeA = -0.7,
                    MagnitudeB = -0.7,
                    BelastingGevalNr = 1,
                });

            base.Bijwerken();
        }

        public override void Init(ProjectInfoEntity projectInfo)
        {
            if (string.IsNullOrEmpty(Merk))
                Merk = "GP-01";

            base.Init(projectInfo);
        }
    }
}
