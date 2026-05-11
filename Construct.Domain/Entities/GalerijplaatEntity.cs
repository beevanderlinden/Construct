using Construct.Domain.Common;

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
        public double Hoogte2 { get; set; } = 250; // Extra hoogte voor de galerijplaat, standaard 250 mm


        public override void Bijwerken()
        {
            PasOpleggingPresetToe(PlaatOpleggingPreset.VrijOpgelegd2Randen);

            //if (!Lijnlasten.Any(ll => ll.Naam == "gevel"))
            //    Lijnlasten.Add(new PlaatRandLijnlast
            //    {
            //        Rand = PlaatRand.Boven,
            //        Naam = "gevel",
            //        MagnitudeA = -2.0,
            //        MagnitudeB = -2.0,
            //        BelastingGevalNr = 1,
            //    });

            //if (!Lijnlasten.Any(ll => ll.Naam == "hek"))
            //    Lijnlasten.Add(new PlaatRandLijnlast
            //    {
            //        Rand = PlaatRand.Onder,
            //        Naam = "hek",
            //        MagnitudeA = -1.0,
            //        MagnitudeB = -1.0,
            //        BelastingGevalNr = 1,
            //    });

            if (MainSlab?.PlaatWapening?.Onder != null)
                MainSlab.PlaatWapening.Onder.LaagHoofdwapening = 2;
            if (MainSlab?.PlaatWapening?.Boven != null)
                MainSlab.PlaatWapening.Boven.LaagHoofdwapening = 2;

            base.Bijwerken();
        }

        public override void Init(ProjectInfoEntity projectInfo)
        {
            if (string.IsNullOrEmpty(Merk))
                Merk = "GP-01";

            base.Init(projectInfo);

            

            Console.WriteLine("GalerijplaatEntity.Init:");
            Console.WriteLine(MainPart == null);
            Console.WriteLine(MainSlab == null);
            Console.WriteLine(base.MainSlab?.PlaatWapening != null);

            if (MainSlab?.PlaatWapening?.Onder != null)
                MainSlab.PlaatWapening.Onder.LaagHoofdwapening = 2;
            if (MainSlab?.PlaatWapening?.Boven != null)
                MainSlab.PlaatWapening.Boven.LaagHoofdwapening = 2;



        }
    }
}
