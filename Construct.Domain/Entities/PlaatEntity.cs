using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Construct.Domain.Entities.Parts;
using Eurocode.BetonConstructies;
using Mechanica.SimpleBeam;
namespace Construct.Domain.Entities
{
    /// <summary>
    /// Een plaat. 2D vloerplaat bijvoovoorbeeld galerij, balkon, dakplaat, etc.
    /// </summary>
    public class PlaatEntity : BetonAssemblageEntity
    {
        // Standaard afmetingen voor een plaat
        public override double Lengte { get; set; } = 6000;
        public override double Breedte { get; set; } = 1500;

        [JsonIgnore]
        public SlabPart? MainSlab => MainPart as SlabPart;

        private SlabPart CreateMainPart()
        {
            return new SlabPart
            {
                Name = "Plaat",
                Material = this.Materiaal,
                ParentAssemblage = this
            };
        }

      


        /// <summary>
        /// Eigen gewicht berekend op basis van plaat dikte en betongewicht (25 kN/m³).
        /// </summary>
        protected override double GetEigenGewichtBerekend()
        {
            double dikte = MainSlab?.Dikte ?? 200; // mm
            return Math.Round(dikte / 1000.0 * 25.0, 2);  // kN/m²
        }

        /// <summary>
        /// Override zodat de plaat-specifieke milieuklassen en DekkingToe-standaarden
        /// direct bij initialisatie worden gezet (en niet overschreven door de basisklasse).
        /// </summary>
        protected override void InitializePlaatDekking()
        {
            base.InitializePlaatDekking(); // aanmaken + referenties

            if (PlaatDekking.Boven != null)
            {
                PlaatDekking.Boven.SelectedMilieuklassen = [MilieuklasseEnum.XC4, MilieuklasseEnum.XD3, MilieuklasseEnum.XF4];
                // Zet DekkingToe alleen op standaard als het nog de basiswaarde (20) is
                if (PlaatDekking.Boven.DekkingToe <= 20)
                    PlaatDekking.Boven.DekkingToe = 30;
            }

            if (PlaatDekking.Onder != null)
            {
                PlaatDekking.Onder.SelectedMilieuklassen = [MilieuklasseEnum.XC4];
                if (PlaatDekking.Onder.DekkingToe <= 20)
                    PlaatDekking.Onder.DekkingToe = 25;
            }

            // geef door dat initialisatie is gedaan
            PlaatDekking.IsInitialized = true;
        }

        public override void Bijwerken()
        {
            // Zorg dat MainPart een SlabPart is
            if (MainPart is not SlabPart)
                MainPart = CreateMainPart();

            // Zorg dat SlabPart materiaal synchroon is en stel standaard ReiEis in
            if (MainPart is SlabPart slab)
            {
                slab.Material = this.Materiaal;

                // Initialiseer PlaatWapening indien nog niet ingesteld
                if (slab.PlaatWapening == null)
                {
                    slab.PlaatWapening = new PlaatWapening
                    {
                        Onder = new PlaatWapeningGroep
                        {
                            Heading = "plaatwapening onder",
                            ReferentieVlak = ReferentieVlakEnum.Onder,
                            BasisWapening = new WapeningContext
                            {
                                Tekst = "r8-150",
                                ReferentieVlak = ReferentieVlakEnum.Onder,
                                ReferentieLengte = 1000,
                            },
                            VerdeelWapening = new WapeningContext
                            {
                                Tekst = "r8-200",
                                ReferentieVlak = ReferentieVlakEnum.Onder,
                                ReferentieLengte = 1000,
                            },
                        },
                        Boven = new PlaatWapeningGroep
                        {
                            Heading = "plaatwapening boven",
                            ReferentieVlak = ReferentieVlakEnum.Boven,
                            BasisWapening = new WapeningContext
                            {
                                Tekst = "r8-150",
                                ReferentieVlak = ReferentieVlakEnum.Boven,
                                ReferentieLengte = 1000,
                            },
                            VerdeelWapening = new WapeningContext
                            {
                                Tekst = "r8-200",
                                ReferentieVlak = ReferentieVlakEnum.Boven,
                                ReferentieLengte = 1000,
                            },
                        },
                    };
                }

                slab.UpdateRei();
            }

            // Initialiseer PlaatDekking (roept de override aan die ook milieuklassen/DekkingToe instelt)
            if (!PlaatDekking.IsInitialized)
            {
                InitializePlaatDekking();
            }


            //bool bovenWasNull = PlaatDekking.Boven == null;
            //bool onderWasNull = PlaatDekking.Onder == null;
            //if (bovenWasNull || onderWasNull) 
            //{ 
            //    InitializePlaatDekking();
            //}

            // Zorg dat IsPlaatGeometrie / IsKwaliteitsBeheersing altijd gezet zijn
            if (PlaatDekking.Boven != null)
            {
                PlaatDekking.Boven.IsPlaatGeometrie = true;
                PlaatDekking.Boven.IsKwaliteitsBeheersing = true;
            }

            if (PlaatDekking.Onder != null)
            {
                PlaatDekking.Onder.IsPlaatGeometrie = true;
                PlaatDekking.Onder.IsKwaliteitsBeheersing = true;
            }

            PlaatDekking.BerekenEnValideer();

            base.Bijwerken();

            // Herstel BelastingGeval-referenties op lijnlasten vóór BerekenStroken,
            // zodat de randstroken de juiste BelastingGeval koppeling krijgen.
            foreach (var ll in Lijnlasten)
            {
                ll.BelastingGeval = Belastingen.BelastingGevallen
                    .FirstOrDefault(bg => bg.Nr == ll.BelastingGevalNr);
                ll.BerekenPunten(Lengte, Breedte);
            }

            // Herstel BelastingGeval-referenties op puntlasten
            foreach (var pl in Puntlasten)
            {
                pl.BelastingGeval = Belastingen.BelastingGevallen
                    .FirstOrDefault(bg => bg.Nr == pl.BelastingGevalNr);
            }

            BerekenStroken();
        }





        //public override double Breedte { get => base.Breedte; set => base.Breedte = value; }

        /// <summary>
        /// Opleggingen van de plaat. Kan bestaan uit rand- en puntopleggingen.
        /// </summary>
        public List<PlaatOplegging> Opleggingen { get; set; } = [];

        /// <summary>
        /// Lijnlasten op de plaat (rand- of vrije lijnlasten).
        /// </summary>
        public List<PlaatRandLijnlast> Lijnlasten { get; set; } = [];

        /// <summary>
        /// Puntlasten op de plaat.
        /// </summary>
        public List<PlaatPuntlast> Puntlasten { get; set; } = [];

        /// <summary>
        /// Past een opleggingspreset toe. Vervangt alle bestaande opleggingen.
        /// Puntopleggingen worden geplaatst op 1/4 en 3/4 van de randbreedte
        /// zodat ze symmetrisch verdeeld zijn.
        /// </summary>
        public void PasOpleggingPresetToe(PlaatOpleggingPreset preset)
        {
            Opleggingen.Clear();

            switch (preset)
            {
                case PlaatOpleggingPreset.Geen:
                    // Geen opleggingen
                    break;
                case PlaatOpleggingPreset.Rondom:
                    // Rondom opgelegde plaat: spanning over beide assen
                    Opleggingen.Add(new PlaatRandOplegging { Rand = PlaatRand.Links,  Conditie = PlaatOpleggingConditie.VrijInRotatie });
                    Opleggingen.Add(new PlaatRandOplegging { Rand = PlaatRand.Rechts, Conditie = PlaatOpleggingConditie.VrijInRotatie });
                    Opleggingen.Add(new PlaatRandOplegging { Rand = PlaatRand.Boven,  Conditie = PlaatOpleggingConditie.VrijInRotatie });
                    Opleggingen.Add(new PlaatRandOplegging { Rand = PlaatRand.Onder,  Conditie = PlaatOpleggingConditie.VrijInRotatie });
                    break;


                case PlaatOpleggingPreset.VrijOpgelegd2Randen:
                    // Vrij opgelegde plaat: spanning over de Lengte-as
                    Opleggingen.Add(new PlaatRandOplegging { Rand = PlaatRand.Links,  Conditie = PlaatOpleggingConditie.VrijInRotatie });
                    Opleggingen.Add(new PlaatRandOplegging { Rand = PlaatRand.Rechts, Conditie = PlaatOpleggingConditie.VrijInRotatie });
                    break;

                case PlaatOpleggingPreset.VrijOpgelegdBreedte:
                    // Vrij opgelegde plaat: spanning over de Breedte-as
                    Opleggingen.Add(new PlaatRandOplegging { Rand = PlaatRand.Boven, Conditie = PlaatOpleggingConditie.VrijInRotatie });
                    Opleggingen.Add(new PlaatRandOplegging { Rand = PlaatRand.Onder, Conditie = PlaatOpleggingConditie.VrijInRotatie });
                    break;

                case PlaatOpleggingPreset.Uitkraging1Rand:
                    // Uitkraging: inklemming op de bovenkant
                    Opleggingen.Add(new PlaatRandOplegging { Rand = PlaatRand.Boven, Conditie = PlaatOpleggingConditie.Ingeklemd });
                    break;

                case PlaatOpleggingPreset.VierPunten2x2:
                    // 2× Links en 2× Rechts op 1/4 en 3/4 van de Breedte-as
                    Opleggingen.Add(new PlaatPuntOplegging { Rand = PlaatRand.Links,  PositieOpRand = Breedte * 0.25, Breedte = 200 });
                    Opleggingen.Add(new PlaatPuntOplegging { Rand = PlaatRand.Links,  PositieOpRand = Breedte * 0.75, Breedte = 200 });
                    Opleggingen.Add(new PlaatPuntOplegging { Rand = PlaatRand.Rechts, PositieOpRand = Breedte * 0.25, Breedte = 200 });
                    Opleggingen.Add(new PlaatPuntOplegging { Rand = PlaatRand.Rechts, PositieOpRand = Breedte * 0.75, Breedte = 200 });
                    break;

                case PlaatOpleggingPreset.VierPunten1x1x2:
                    // 1× Links en 1× Rechts in het midden, 2× Boven op 1/4 en 3/4 van de Lengte-as
                    Opleggingen.Add(new PlaatPuntOplegging { Rand = PlaatRand.Links,  PositieOpRand = Breedte * 0.50, Breedte = 200 });
                    Opleggingen.Add(new PlaatPuntOplegging { Rand = PlaatRand.Rechts, PositieOpRand = Breedte * 0.50, Breedte = 200 });
                    Opleggingen.Add(new PlaatPuntOplegging { Rand = PlaatRand.Boven,  PositieOpRand = Lengte  * 0.25, Breedte = 200 });
                    Opleggingen.Add(new PlaatPuntOplegging { Rand = PlaatRand.Boven,  PositieOpRand = Lengte  * 0.75, Breedte = 200 });
                    break;

                case PlaatOpleggingPreset.UitkragingTweePunten:
                    // 2× ingeklemd op de bovenkant, elk 1000 mm breed, op 1/4 en 3/4 van de Lengte-as
                    Opleggingen.Add(new PlaatPuntOplegging { Rand = PlaatRand.Boven, Conditie = PlaatOpleggingConditie.Ingeklemd, PositieOpRand = Lengte * 0.25, Breedte = 1000 });
                    Opleggingen.Add(new PlaatPuntOplegging { Rand = PlaatRand.Boven, Conditie = PlaatOpleggingConditie.Ingeklemd, PositieOpRand = Lengte * 0.75, Breedte = 1000 });
                    break;
            }
        }

        // ----------------------------------------------------------------
        //  Stroken
        // ----------------------------------------------------------------

        /// <summary>
        /// Standaard breedte (mm) van een randstrook die langs een Boven- of Onderrand
        /// wordt aangemaakt als er een lijnlast op die rand staat.
        /// </summary>
        public double RandStrookBreedteMm { get; set; } = 500.0;

        /// <summary>
        /// Berekeningsstroken afgeleid uit <see cref="Opleggingen"/>.
        /// Wordt elke keer opnieuw opgebouwd via <see cref="BerekenStroken"/>.
        /// Niet geserialiseerd.
        /// </summary>
        [JsonIgnore]
        public List<PlaatStrook> PlaatStroken { get; private set; } = [];

        /// <summary>
        /// Leidt stroken af uit de huidige <see cref="Opleggingen"/>.
        /// Genereert één strook per unieke dwarspositie per richtingpaar.
        /// Roep aan na elke geometrie- of opleggingswijziging.
        /// </summary>
        public void BerekenStroken()
        {
            PlaatStroken.Clear();
            ClearStroken();

            int teller = 1;

            foreach (var (randA, randB, richting) in (ValueTuple<PlaatRand, PlaatRand, PlaatStrookRichting>[])
            [
                (PlaatRand.Links, PlaatRand.Rechts, PlaatStrookRichting.LangsLengte),
                (PlaatRand.Boven, PlaatRand.Onder,  PlaatStrookRichting.LangsBreedte),
            ])
            {
                var nieuw = MaakStrokenVoorPaar(randA, randB, richting, ref teller);
                PlaatStroken.AddRange(nieuw);
            }

            foreach (var ps in PlaatStroken)
            {
                AddStrook(ps.Strook);
                ps.Strook.BerekenStrook();
            }

            // Extra stroken langs randen met lijnlasten
            var randStroken = MaakRandStrokenVoorLijnlasten(ref teller);
            PlaatStroken.AddRange(randStroken);
            foreach (var ps in randStroken)
            {
                AddStrook(ps.Strook);
                ps.Strook.BerekenStrook();
            }

            // Extra stroken tpv van puntlasten (optioneel, afhankelijk van ontwerpkeuze)
            var puntStroken = MaakStrokenVoorPuntlasten(ref teller);
            PlaatStroken.AddRange(puntStroken);
            foreach (var ps in puntStroken)
            {
                AddStrook(ps.Strook);
                ps.Strook.BerekenStrook();
            }

        }

        private List<PlaatStrook> MaakStrokenVoorPaar(
            PlaatRand randA,
            PlaatRand randB,
            PlaatStrookRichting richting,
            ref int teller)
        {
            var stroken = new List<PlaatStrook>();

            var opleggA = Opleggingen.Where(o => o.Rand == randA).ToList();
            var opleggB = Opleggingen.Where(o => o.Rand == randB).ToList();

            if (opleggA.Count == 0 && opleggB.Count == 0) return stroken;

            // Spanmaat en dwarsmax voor deze richting
            double spanMax  = richting == PlaatStrookRichting.LangsLengte ? Lengte  : Breedte;
            double dwarsMax = richting == PlaatStrookRichting.LangsLengte ? Breedte : Lengte;

            // Unieke dwarspositie(s) per kant
            var positiesA = GetDwarsPosities(opleggA, dwarsMax);
            var positiesB = GetDwarsPosities(opleggB, dwarsMax);

            // Alle unieke kandidaat-posities samen
            var allePosities = positiesA.Union(positiesB).OrderBy(p => p).Distinct().ToList();

            foreach (double dwarsPos in allePosities)
            {
                var oplA = ZoekNabijeOplegging(opleggA, dwarsPos, dwarsMax);
                var oplB = ZoekNabijeOplegging(opleggB, dwarsPos, dwarsMax);

                var conditieStart = oplA?.Conditie ?? PlaatOpleggingConditie.VrijInRotatie;
                var conditieEind  = oplB?.Conditie ?? PlaatOpleggingConditie.VrijInRotatie;

                double invlBreedte = BerekenInvloedsBreedteVoor(dwarsPos, allePosities, dwarsMax);

                var ps = PlaatStrook.Maak(
                    naam:              $"S{teller++}",
                    richting:          richting,
                    positieDwars:      dwarsPos,
                    startMm:           0,
                    eindMm:            spanMax,
                    invloedsBreedteMm: invlBreedte,
                    conditieStart:     conditieStart,
                    conditieEind:      conditieEind);

                // Beam: lengte (mm → m) en schematisering
                ps.Strook.Beam.Length = ps.Overspanning / 1000.0;
                ps.Strook.Beam.Schematisering = (oplA == null || oplB == null)
                    ? SchemaType.Uitkraging   // uitkraging: één vrije rand
                    : SchemaType.VrijOpgelegd; // overruled hieronder indien ingeklemd

                // Verfijning: inklemming aan begin en/of einde
                if (conditieStart == PlaatOpleggingConditie.Ingeklemd)
                    ps.Strook.Beam.StartSupport = SupportType.Fixed;
                if (conditieEind == PlaatOpleggingConditie.Ingeklemd)
                    ps.Strook.Beam.EndSupport = SupportType.Fixed;
                if (oplB == null)
                    ps.Strook.Beam.EndSupport = SupportType.None;  // vrije rand

                // Vader instellen zodat BerekenStrook() de LoadContext en het Materiaal kan ophalen
                ps.Strook.Father = this;

                // Profiel: 1000 mm referentiebreedte × plaat dikte
                double dikte = MainSlab?.Dikte ?? 200;
                ps.Strook.Profiel = new Profielen.Beton.BetonProfiel(1000, dikte);
                ps.Strook.Beam.Profiel = ps.Strook.Profiel;

                // PlaatWapening van de plaat doorzetten op de beam (nodig voor buigtoets in BeamResultsHelper)
                if (MainSlab?.PlaatWapening != null)
                {
                    ps.Strook.PlaatWapening = MainSlab.PlaatWapening;
                    ps.Strook.Beam.PlaatWapening = MainSlab.PlaatWapening;
                }

                // Lasten per strekkende meter (1 m referentiestrook)
                ps.Strook.Beam.Loads.Clear();
                double L = ps.Strook.Beam.Length;
                var bg1 = Belastingen.BelastingGevallen.ElementAtOrDefault(0);
                var bg2 = Belastingen.BelastingGevallen.ElementAtOrDefault(1);
                double qGk = PermanenteBelastingPerM2;
                string qGkOmschrijving = $"permanent";

                double qQk = VeranderlijkeBelastingPerM2;
                string qQkOmschrijving = $"veranderlijk";

                if (bg1 != null && Math.Abs(qGk) > 1e-9)
                    ps.Strook.Beam.Loads.Add(new DistributedLoad(bg1, "LG1", 0, L, -qGk) { Description = qGkOmschrijving});
                
                
                
                if (bg2 != null && Math.Abs(qQk) > 1e-9)
                    ps.Strook.Beam.Loads.Add(new DistributedLoad(bg2, "LQ1", 0, L, -qQk) { Description = qQkOmschrijving});

                stroken.Add(ps);
            }

            return stroken;
        }

        /// <summary>
        /// Maakt extra berekeningsstroken langs de Boven- en Onderrand aan
        /// voor elke rand waarop een of meer <see cref="PlaatRandLijnlast">lijnlasten</see> staan.
        /// Elke strook loopt langs de Lengte-as (LangsLengte) en heeft een breedte van
        /// <see cref="RandStrookBreedteMm"/> mm (standaard 500 mm).
        /// </summary>
        private List<PlaatStrook> MaakRandStrokenVoorLijnlasten(ref int teller)
        {
            var stroken = new List<PlaatStrook>();

            var randen = Lijnlasten
                .Where(ll => ll.Rand == PlaatRand.Boven || ll.Rand == PlaatRand.Onder)
                .GroupBy(ll => ll.Rand);

            foreach (var groep in randen)
            {
                PlaatRand rand = groep.Key;

                // Steunpunten voor de LangsLengte-richting liggen op Links en Rechts
                var oplLinks  = Opleggingen.FirstOrDefault(o => o.Rand == PlaatRand.Links);
                var oplRechts = Opleggingen.FirstOrDefault(o => o.Rand == PlaatRand.Rechts);

                if (oplLinks == null && oplRechts == null) continue;

                double positieDwars = rand == PlaatRand.Boven
                    ? RandStrookBreedteMm / 2.0
                    : Breedte - RandStrookBreedteMm / 2.0;

                var conditieStart = oplLinks?.Conditie  ?? PlaatOpleggingConditie.VrijInRotatie;
                var conditieEind  = oplRechts?.Conditie ?? PlaatOpleggingConditie.VrijInRotatie;

                var ps = PlaatStrook.Maak(
                    naam:              $"S{teller++} ({rand})",
                    richting:          PlaatStrookRichting.LangsLengte,
                    positieDwars:      positieDwars,
                    startMm:           0,
                    eindMm:            Lengte,
                    invloedsBreedteMm: RandStrookBreedteMm,
                    conditieStart:     conditieStart,
                    conditieEind:      conditieEind);

                double beamL = Lengte / 1000.0;
                ps.Strook.Beam.Length = beamL;
                ps.Strook.Beam.Schematisering = (oplLinks == null || oplRechts == null)
                    ? SchemaType.Uitkraging
                    : SchemaType.VrijOpgelegd;

                if (conditieStart == PlaatOpleggingConditie.Ingeklemd)
                    ps.Strook.Beam.StartSupport = SupportType.Fixed;
                if (conditieEind == PlaatOpleggingConditie.Ingeklemd)
                    ps.Strook.Beam.EndSupport = SupportType.Fixed;
                if (oplRechts == null)
                    ps.Strook.Beam.EndSupport = SupportType.None;

                ps.Strook.Father = this;

                // Profiel: randstrookbreedte × plaat dikte
                double randDikte = MainSlab?.Dikte ?? 200;
                ps.Strook.Profiel = new Profielen.Beton.BetonProfiel((int)RandStrookBreedteMm, randDikte);
                ps.Strook.Beam.Profiel = ps.Strook.Profiel;

                ps.Strook.Beam.Loads.Clear();

                // Basis belastingen: oppervlaktelast × strookbreedte → kN/m op de balk
                double strookBreedteM = RandStrookBreedteMm / 1000.0;
                var bg1 = Belastingen.BelastingGevallen.ElementAtOrDefault(0);
                var bg2 = Belastingen.BelastingGevallen.ElementAtOrDefault(1);
                double qGk = PermanenteBelastingPerM2 * strookBreedteM;
                double qQk = VeranderlijkeBelastingPerM2 * strookBreedteM;
                if (bg1 != null && Math.Abs(qGk) > 1e-9)
                    ps.Strook.Beam.Loads.Add(new DistributedLoad(bg1, "LG1", 0, beamL, -qGk) { Description = "e.g. + afw."});
                if (bg2 != null && Math.Abs(qQk) > 1e-9)
                    ps.Strook.Beam.Loads.Add(new DistributedLoad(bg2, "LQ1", 0, beamL, -qQk) { Description = "opgelegde belasting"});

                foreach (var ll in groep)
                {
                    if (ll.BelastingGeval == null) continue;

                    // Lijnlastpositie (lx in mm) omrekenen naar beampositie (m)
                    double xStart = Math.Max(0, Math.Min(beamL, ll.S.Lx / 1000.0));
                    double xEind  = Math.Max(0, Math.Min(beamL, ll.E.Lx / 1000.0));
                    if (xEind <= xStart) { xStart = 0; xEind = beamL; }

                    ps.Strook.Beam.Loads.Add(new DistributedLoad(
                        ll.BelastingGeval, ll.Naam,
                        xStart, xEind, ll.MagnitudeA)
                    { Description = ll.Omschrijving});
                }

                stroken.Add(ps);
            }

            return stroken;
        }

        /// <summary>
        /// Maakt extra berekeningsstroken aan voor elke <see cref="PlaatPuntlast"/>.
        /// De strook loopt door de puntlast en wordt gekoppeld aan de dichtstbijzijnde opleggingen.
        /// Richting wordt bepaald op basis van de dichtste opleggingsparen.
        /// </summary>
        private List<PlaatStrook> MaakStrokenVoorPuntlasten(ref int teller)
        {
            var stroken = new List<PlaatStrook>();

            foreach (var pl in Puntlasten)
            {
                // Bepaal beste richting: kies de richting met de kortste overspanning
                var (richting, positieDwars, spanMax, dwarsMax) = BepaalBesteRichtingVoorPuntlast(pl);

                // Zoek opleggingen aan beide kanten in de gekozen richting
                var (randStart, randEind) = richting == PlaatStrookRichting.LangsLengte
                    ? (PlaatRand.Links, PlaatRand.Rechts)
                    : (PlaatRand.Boven, PlaatRand.Onder);

                var oplStart = Opleggingen.Where(o => o.Rand == randStart).ToList();
                var oplEind = Opleggingen.Where(o => o.Rand == randEind).ToList();

                if (oplStart.Count == 0 && oplEind.Count == 0) continue;

                var conditieStart = ZoekNabijeOplegging(oplStart, positieDwars, dwarsMax)?.Conditie
                    ?? PlaatOpleggingConditie.VrijInRotatie;
                var conditieEind = ZoekNabijeOplegging(oplEind, positieDwars, dwarsMax)?.Conditie
                    ?? PlaatOpleggingConditie.VrijInRotatie;

                // Invloedbreedte: standaard 500mm of helft afstand tot naburige puntlasten
                double invlBreedte = BerekenInvloedsBreedteVoorPuntlast(pl, richting);

                var ps = PlaatStrook.Maak(
                    naam:              $"S{teller++} (P:{pl.Naam})",
                    richting:          richting,
                    positieDwars:      positieDwars,
                    startMm:           0,
                    eindMm:            spanMax,
                    invloedsBreedteMm: invlBreedte,
                    conditieStart:     conditieStart,
                    conditieEind:      conditieEind);

                double beamL = spanMax / 1000.0;
                ps.Strook.Beam.Length = beamL;
                ps.Strook.Beam.Schematisering = (oplStart.Count == 0 || oplEind.Count == 0)
                    ? SchemaType.Uitkraging
                    : SchemaType.VrijOpgelegd;

                if (conditieStart == PlaatOpleggingConditie.Ingeklemd)
                    ps.Strook.Beam.StartSupport = SupportType.Fixed;
                if (conditieEind == PlaatOpleggingConditie.Ingeklemd)
                    ps.Strook.Beam.EndSupport = SupportType.Fixed;
                if (oplEind.Count == 0)
                    ps.Strook.Beam.EndSupport = SupportType.None;

                ps.Strook.Father = this;

                // Profiel: invloedbreedte × plaat dikte
                double dikte = MainSlab?.Dikte ?? 200;
                ps.Strook.Profiel = new Profielen.Beton.BetonProfiel((int)invlBreedte, dikte);
                ps.Strook.Beam.Profiel = ps.Strook.Profiel;

                // PlaatWapening doorzetten
                if (MainSlab?.PlaatWapening != null)
                {
                    ps.Strook.PlaatWapening = MainSlab.PlaatWapening;
                    ps.Strook.Beam.PlaatWapening = MainSlab.PlaatWapening;
                }

                ps.Strook.Beam.Loads.Clear();

                // Basis belastingen: oppervlaktelast × strookbreedte → kN/m
                double strookBreedteM = invlBreedte / 1000.0;
                var bg1 = Belastingen.BelastingGevallen.ElementAtOrDefault(0);
                var bg2 = Belastingen.BelastingGevallen.ElementAtOrDefault(1);
                double qGk = PermanenteBelastingPerM2 * strookBreedteM;
                double qQk = VeranderlijkeBelastingPerM2 * strookBreedteM;

                if (bg1 != null && Math.Abs(qGk) > 1e-9)
                    ps.Strook.Beam.Loads.Add(new DistributedLoad(bg1, "LG1", 0, beamL, -qGk) { Description= "e.g. + afw."});
                if (bg2 != null && Math.Abs(qQk) > 1e-9)
                    ps.Strook.Beam.Loads.Add(new DistributedLoad(bg2, "LQ1", 0, beamL, -qQk) { Description= "opgelegde belasting"});

                // Puntlast toevoegen
                if (pl.BelastingGeval != null)
                {
                    // Positie van puntlast langs de strook (in meters)
                    double xPos = richting == PlaatStrookRichting.LangsLengte
                        ? pl.PosX / 1000.0
                        : pl.PosY / 1000.0;

                    xPos = Math.Max(0, Math.Min(beamL, xPos));

                    ps.Strook.Beam.Loads.Add(new PointLoad(pl.Magnitude, pl.BelastingGeval)
                    {
                        Position = xPos,
                        Name = pl.Naam,
                        Description = pl.Omschrijving
                    });
                }

                stroken.Add(ps);
            }

            return stroken;
        }

        /// <summary>
        /// Bepaalt de beste richting voor een strook door een puntlast,
        /// op basis van de dichtstbijzijnde opleggingen.
        /// </summary>
        private (PlaatStrookRichting richting, double positieDwars, double spanMax, double dwarsMax) 
            BepaalBesteRichtingVoorPuntlast(PlaatPuntlast pl)
        {
            // Controleer beide richtingen en kies de richting met de kortste overspanning
            var oplLinks  = Opleggingen.Any(o => o.Rand == PlaatRand.Links);
            var oplRechts = Opleggingen.Any(o => o.Rand == PlaatRand.Rechts);
            var oplBoven  = Opleggingen.Any(o => o.Rand == PlaatRand.Boven);
            var oplOnder  = Opleggingen.Any(o => o.Rand == PlaatRand.Onder);

            bool heeftLengte  = oplLinks && oplRechts;
            bool heeftBreedte = oplBoven && oplOnder;

            // Default: LangsLengte
            if (heeftLengte && !heeftBreedte)
                return (PlaatStrookRichting.LangsLengte, pl.PosY, Lengte, Breedte);

            if (heeftBreedte && !heeftLengte)
                return (PlaatStrookRichting.LangsBreedte, pl.PosX, Breedte, Lengte);

            // Beide richtingen mogelijk: kies de kortste overspanning
            if (heeftLengte && heeftBreedte)
            {
                return Lengte <= Breedte
                    ? (PlaatStrookRichting.LangsLengte, pl.PosY, Lengte, Breedte)
                    : (PlaatStrookRichting.LangsBreedte, pl.PosX, Breedte, Lengte);
            }

            // Fallback: LangsLengte
            return (PlaatStrookRichting.LangsLengte, pl.PosY, Lengte, Breedte);
        }

        /// <summary>
        /// Berekent de invloedbreedte voor een puntlast-strook.
        /// Standaard 500mm, of kleiner indien er dichtbij andere puntlasten zijn.
        /// </summary>
        private double BerekenInvloedsBreedteVoorPuntlast(PlaatPuntlast pl, PlaatStrookRichting richting)
        {
            const double standaardBreedte = 500.0; // mm

            // Vind afstand tot dichtstbijzijnde puntlast in dwarsrichting
            var anderePuntlasten = Puntlasten.Where(p => p != pl).ToList();
            if (anderePuntlasten.Count == 0)
                return standaardBreedte;

            double minAfstand = anderePuntlasten
                .Select(p =>
                {
                    double dx = p.PosX - pl.PosX;
                    double dy = p.PosY - pl.PosY;
                    // Bereken afstand haaks op de strookrichting
                    return richting == PlaatStrookRichting.LangsLengte
                        ? Math.Abs(dy)  // dwarsrichting is Y
                        : Math.Abs(dx); // dwarsrichting is X
                })
                .Min();

            // Invloedbreedte = helft van afstand tot naburige puntlast, maar minimaal 200mm en maximaal standaardBreedte
            double breedte = Math.Min(standaardBreedte, minAfstand);
            return Math.Max(200, breedte);
        }

        private List<double> GetDwarsPosities(List<PlaatOplegging> opleggingen, double dwarsMax)
        {
            var result = new List<double>();
            foreach (var opl in opleggingen)
            {
                result.Add(opl is PlaatPuntOplegging punt
                    ? punt.PositieOpRand
                    : dwarsMax / 2); // PlaatRandOplegging: representatief middelpunt
            }
            return [.. result.Distinct()];
        }

        private static PlaatOplegging? ZoekNabijeOplegging(
            List<PlaatOplegging> opleggingen, double dwarsPos, double dwarsMax)
        {
            if (opleggingen.Count == 0) return null;
            return opleggingen.MinBy(opl =>
                Math.Abs((opl is PlaatPuntOplegging p ? p.PositieOpRand : dwarsMax / 2) - dwarsPos));
        }

        private static double BerekenInvloedsBreedteVoor(
            double dwarsPos, List<double> allePosities, double dwarsMax)
        {
            int idx  = allePosities.IndexOf(dwarsPos);
            double voor = idx > 0
                ? (dwarsPos - allePosities[idx - 1]) / 2
                : dwarsPos;
            double na = idx < allePosities.Count - 1
                ? (allePosities[idx + 1] - dwarsPos) / 2
                : dwarsMax - dwarsPos;
            return voor + na;
        }
    }
}
