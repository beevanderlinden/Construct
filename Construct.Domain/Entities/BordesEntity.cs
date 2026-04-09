using CommonLibrary;
using Construct.Domain.Entities.Parts;
using Construct.Domain.Helpers;
using Eurocode.Belastingen;
using Eurocode.BetonConstructies;
using Eurocode.Grondslagen;
using ExportFactory.Extensions;
//using Kaskon.Toolbox.PrefabModels;

//using Mechanica.LiggerSB;
using Mechanica.SimpleBeam;
using Microsoft.AspNetCore.Components.Forms;
using Profielen.Parametrisch;
using Plotly.Blazor.ConfigLib;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Tekla.Common.Geometry;
using Tekla.Structures.Model;
using System.Text.Json.Serialization;

namespace Construct.Domain.Entities
{
    public class BordesEntity : BetonAssemblageEntity
    {
        public BordesEntity()
        {
            this.AssemblageType = AssemblageTypeEnum.BetonAssemblage;
            this.Naam = "Bordes";
            this.Merk = "BD-?";

            // ✅ Zet Materiaal VOOR InitBasisStrook() zodat Beton property beschikbaar is
            Materiaal = new BetonContext("C45/55");

            // ✅ NIEUW: Creëer MainPart (PlatePart) VROEG in constructor
            MainPart = CreateMainPart();

            // ⚠️ OPMERKING: PlaatDekking kan hier NIET geïnitialiseerd worden omdat ProjectInfo nog null is
            // Dit gebeurt later in Init() of AddAssemblage.razor na koppeling aan project

            // ✅ Genereer BelastingCombinaties zodat Beam.LoadContext correct werkt
            Belastingen.GenereerBelastingCombinaties(
                Belastingen,
                Belastingen.BelastingGevallen,
                Belastingen.CombinatiesTypes);

            Trap1 = new(this);
            Trap2 = new(this)
            {
                Gespiegeld = true
            };

            TandOplegging = new() { };
            Tand = new TandOplegging(this, this.TandOplegging) ;


            this.InitBasisStrook();
            if (_basisStrook != null)
                AddToets(_basisStrook);

            


            var qbasis = -10;

            var basis = new StrookEntity
            {
                Father = this,
                Profiel = _basisStrook?.Profiel ?? new()
            };
            basis.Beam.Length = this.Lengte * 1e-3;
            basis.Beam.StartSupport = Mechanica.SimpleBeam.SupportType.Pin;
            basis.Beam.EndSupport = Mechanica.SimpleBeam.SupportType.Pin;

            var dl1 = new DistributedLoad(0, basis.Beam.Length, qbasis)
            {
                LoadCase = Belastingen.BelastingGevallen[0]
            };


            basis.Beam.Loads.Add(dl1);
            basis.Naam = "basisstrook";

            this.AddStrook(basis);

            var vs = new StrookEntity
            {
                Naam = "versterkte strook",
                Father = this,
                Profiel = new(Breedte, Hoogte)
            };
            vs.Beam.Length = this.Lengte * 1e-3;
            var q1 = vs.Profiel.B / 1000 * qbasis;
            vs.Beam.Loads.Add(new DistributedLoad(0, vs.Beam.Length, q1, q1));
            
            if (this.Trap1.AansluitendElement != null)
            {
                var c = this.Trap1;
                var rd = this.Trap1.Reacties.G + this.Trap1.Reacties.Q; // automatisch maken
                vs.Beam.Loads.Add(
                    new DistributedLoad(
                        c.Randafstand / 1000.0, 
                        (c.Randafstand + c.Lengte)/1000.0, -rd, -rd ));
            }

            if (this.Trap2.AansluitendElement != null)
            {
                var c = this.Trap2;
                var rd = this.Trap2.Reacties.G + this.Trap2.Reacties.Q;
                vs.Beam.Loads.Add(
                    new DistributedLoad(
                        vs.Beam.Length - (c.Randafstand + c.Lengte)/1000.0,
                        vs.Beam.Length - (c.Randafstand/1000.0),
                        -rd, -rd
                        )
                    );
            }

            this.AddStrook(vs);


            // referentie-toetsen
            
        }

        public override void RestoreReferencesAfterDeserialization(ProjectEntity project)
        {
            // ✅ EERST: Algemeen deel (Belastingen, ProjectInfo, Materiaal, etc.)
            base.RestoreReferencesAfterDeserialization(project);

            // ✅ NIEUW: Migreer oude data naar MainPart indien MainPart null is
            if (MainPart == null && _dikte > 0)
            {
                Console.WriteLine($"✅ [BordesEntity] Migreer oude data naar MainPart");
                MainPart = CreateMainPart();
            }

            // ✅ DAN: Bordes-SPECIFIEKE dingen
            
            // Herstel bidirectionele Father-relaties in VerbindingAansluitendElement
            Trap1.Father = this;
            Trap2.Father = this;

            // ✅ NIEUW: Herstel Father voor Tand (TandOplegging)
            if (Tand != null)
            {
                Tand.Father = this;
                Tand.Initialize(Tand.Oplegging);
            }

            // ✅ NIEUW: Herstel trap-referenties
            Trap1.RestoreAssemblageReference(project);
            Trap2.RestoreAssemblageReference(project);
            
            // ✅ Gebruik centrale methode om PlaatDekking te initialiseren
            InitializePlaatDekking();

            // ✅ FIX: Controleer of Boven en Onder BasisWapening dezelfde referentie delen (na deserialisatie)
            // Dit kan gebeuren door ReferenceHandler.IgnoreCycles bij JSON serialisatie
            if (PlaatWapening?.Boven?.BasisWapening != null && 
                PlaatWapening?.Onder?.BasisWapening != null &&
                ReferenceEquals(PlaatWapening.Boven.BasisWapening, PlaatWapening.Onder.BasisWapening))
            {
                Console.WriteLine($"⚠️ [BordesEntity] PROBLEEM: Boven.BasisWapening en Onder.BasisWapening zijn hetzelfde object! Maak kopie...");
                // Maak een diepe kopie van Onder.BasisWapening
                PlaatWapening.Onder.BasisWapening = PlaatWapening.Onder.BasisWapening.Clone();
                Console.WriteLine($"✅ [BordesEntity] Onder.BasisWapening gekloond. Nu zijn het verschillende objecten.");
            }

            // ✅ FIX: Hetzelfde voor VerdeelWapening
            if (PlaatWapening?.Boven?.VerdeelWapening != null && 
                PlaatWapening?.Onder?.VerdeelWapening != null &&
                ReferenceEquals(PlaatWapening.Boven.VerdeelWapening, PlaatWapening.Onder.VerdeelWapening))
            {
                Console.WriteLine($"⚠️ [BordesEntity] PROBLEEM: Boven.VerdeelWapening en Onder.VerdeelWapening zijn hetzelfde object! Maak kopie...");
                PlaatWapening.Onder.VerdeelWapening = PlaatWapening.Onder.VerdeelWapening.Clone();
                Console.WriteLine($"✅ [BordesEntity] Onder.VerdeelWapening gekloond.");
            }

            // Create basiswapening without direct call to the ReferentieDekking setter (use reflection)
            var hoofdwapOnder = new WapeningContext()
            {
                Tekst = "r8-150+r6-500",
                ReferentieVlak = ReferentieVlakEnum.Onder,
                ReferentieLengte = 1000,
            };
            var verdeelwapOnder = new WapeningContext()
            {
                Tekst = "r6-150",
                ReferentieVlak = ReferentieVlakEnum.Onder,
                ReferentieLengte = 1000,
            };

            var hoofdwapBoven = new WapeningContext()
            {
                Tekst = "r6-150",
                ReferentieVlak = ReferentieVlakEnum.Boven,
                ReferentieLengte = 1000,
            };
            var verdeelwapBoven = new WapeningContext()
            {
                Tekst = "r6-150",
                ReferentieVlak = ReferentieVlakEnum.Boven,
                ReferentieLengte = 1000,
            };

            // Probeer via reflection te zetten (verschillende versies van Eurocode kunnen andere members hebben)
            try
            {
                var wcType = typeof(WapeningContext);
                var propRef = wcType.GetProperty("ReferentieDekking");
                if (propRef != null && propRef.CanWrite)
                {
                    propRef.SetValue(hoofdwapOnder, PlaatDekking.Onder.DekkingToe);
                }
                else
                {
                    var propDekToeg = wcType.GetProperty("DekkingToegepast");
                    if (propDekToeg != null && propDekToeg.CanWrite)
                    {
                        propDekToeg.SetValue(hoofdwapOnder, PlaatDekking.Onder.DekkingToe);
                    }
                    else
                    {
                        var propDek = wcType.GetProperty("Dekking");
                        if (propDek != null && propDek.CanWrite)
                        {
                            propDek.SetValue(hoofdwapOnder, PlaatDekking.Onder);
                        }
                    }
                }
            }
            catch
            {
                // swallow - we prefer not to throw here to avoid MissingMethodException at runtime
            }

            // ✅ Alleen initialiseren als PlaatWapening nog niet bestaat
            PlaatWapening ??= new PlaatWapening()
            {
                Boven = new PlaatWapeningGroep()
                {
                    Heading = "boven",
                    DekkingBuitensteLaag = PlaatDekking.Boven,
                    BasisWapening = hoofdwapBoven,
                    VerdeelWapening = verdeelwapBoven,
                    LaagHoofdwapening = 2,
                    DiameterVerdeel = 8,
                    
                    

                },
                Onder = new PlaatWapeningGroep()
                {
                    Heading = "plaatwapening onder",
                    DekkingBuitensteLaag = PlaatDekking.Onder,
                    BasisWapening = hoofdwapOnder,
                    VerdeelWapening = verdeelwapOnder,
                    LaagHoofdwapening = 2,
                    DiameterVerdeel = 8,
                },
            };

            // ✅ DEBUG: Controleer of de WapeningContext objecten uniek zijn
            Console.WriteLine($"[BordesEntity.RestoreReferences] hoofdwapBoven HashCode: {hoofdwapBoven.GetHashCode()}");
            Console.WriteLine($"[BordesEntity.RestoreReferences] hoofdwapOnder HashCode: {hoofdwapOnder.GetHashCode()}");
            Console.WriteLine($"[BordesEntity.RestoreReferences] Boven.BasisWapening HashCode: {PlaatWapening?.Boven?.BasisWapening.GetHashCode()}");
            Console.WriteLine($"[BordesEntity.RestoreReferences] Onder.BasisWapening HashCode: {PlaatWapening?.Onder?.BasisWapening.GetHashCode()}");
            Console.WriteLine($"[BordesEntity.RestoreReferences] Zijn ze hetzelfde? {ReferenceEquals(PlaatWapening?.Boven?.BasisWapening, PlaatWapening?.Onder?.BasisWapening)}");
        }

        /// <summary>
        /// ✅ Initialiseer bordes met ProjectInfo en koppel PlaatDekking aan project eigenschappen.
        /// Roep deze methode aan na het toevoegen van een bordes aan een project.
        /// </summary>
        /// <param name="projectInfo">ProjectInfo met Grondslagen</param>
        public override void Init(ProjectInfoEntity projectInfo)
        {
            ProjectInfo = projectInfo;

            // ✅ Herstel Belastingen.Grondslagen referentie
            if (Belastingen != null)
            {
                Belastingen.Grondslagen = ProjectInfo.Grondslagen;
            }

            // ✅ Initialiseer PlaatDekking met project eigenschappen (synct ook MainPart)
            InitializePlaatDekking();

            // ✅ NIEUW: Koppel MainPart expliciet aan assemblage eigenschappen
            if (MainSlab != null)
            {
                MainSlab.Material = this.Materiaal; // Shared!
                MainSlab.PlaatDekking = this.PlaatDekking; // Shared!
                MainSlab.PlaatWapening = this.PlaatWapening; // Shared!
                MainSlab.Dikte = this.Dikte; // ✅ Sync dikte
                Console.WriteLine($"✅ [BordesEntity] MainPart gekoppeld aan assemblage eigenschappen (Dikte={MainSlab.Dikte}mm)");
            }

            // ✅ Update stroken met materiaal
            if (_basisStrook != null && Beton != null)
            {
                _basisStrook.Beton = Beton;
            }
        }


        private double _lengte = 2200;
        private double _breedte = 1200;
        private double _dikte = 200;
        

        private double _breedteVersterkteStrook = 300;

        private GrondslagenContext? _grondslagen; // todo naar base class?
        
        
        
        private PlaatWapening? _plaatWapening;

        // aansluitende assemblages
        public VerbindingAansluitendElement Trap1 { get; set; }
        public VerbindingAansluitendElement Trap2 { get; set; }
        public double MinimaleTandHoogte
        {
            get
            {
                return PlaatDekking.Onder.DekkingToe + PlaatDekking.Boven.DekkingToe + 7 * PlaatWapening?.Onder?.VerdeelWapening?.GrootsteDiameter ?? 8.0;
            }
        }

        public OpleggingContext TandOplegging = new() { };
        public TandOplegging Tand { get; set; }
        
        /// <summary>
        /// Wapening constraints voor berekeningen.
        /// OBSOLETE: Gebruik WapeningContext.TekstOndergrens in plaats van constraints.
        /// </summary>
        //[Obsolete("Gebruik WapeningContext.TekstOndergrens. Bijvoorbeeld: PlaatWapening.Onder.BasisWapening.TekstOndergrens = \"8-150\"")]
        //public WapeningConstraint BijlegBovenConstraint { get; set; } = new(8, 2, null);
        
        //[Obsolete("Gebruik WapeningContext.TekstOndergrens. Bijvoorbeeld: PlaatWapening.Onder.BasisWapening.TekstOndergrens = \"8-150\"")]
        //public WapeningConstraint BijlegOnderConstraint { get; set; } = new(8, 2, null);
        
        //[Obsolete("Gebruik WapeningContext.TekstOndergrens. Bijvoorbeeld: Tand.WapeningAlgemeen.TekstOndergrens = \"6-125\"")]
        //public WapeningConstraint DetailWapeningConstraint { get; set; } = new(6, null, 125);
        
        //[Obsolete("Gebruik WapeningContext.TekstOndergrens. Bijvoorbeeld: PlaatWapening.Onder.BasisWapening.TekstOndergrens = \"6-150\"")]
        //public WapeningConstraint OnderHoofdConstraint { get; set; } = new(6, null, 150);
        
        //[Obsolete("Gebruik WapeningContext.TekstOndergrens. Bijvoorbeeld: PlaatWapening.Onder.VerdeelWapening.TekstOndergrens = \"6-250\"")]
        //public WapeningConstraint OnderVerdeelConstraint { get; set; } = new(6, null, 250);
        
        //[Obsolete("Gebruik WapeningContext.TekstOndergrens. Bijvoorbeeld: PlaatWapening.Boven.BasisWapening.TekstOndergrens = \"6-150\"")]
        //public WapeningConstraint BovenHoofdConstraint { get; set; } = new(6, null, 150);
        
        //[Obsolete("Gebruik WapeningContext.TekstOndergrens. Bijvoorbeeld: PlaatWapening.Boven.VerdeelWapening.TekstOndergrens = \"6-250\"")]
        //public WapeningConstraint BovenVerdeelConstraint { get; set; } = new(6, null, 250);
        
        /// <summary>
        /// Berekende/huidige diameter detailwapening
        /// </summary>
        public double? DetailWapeningDiameter => PlaatWapening?.Boven?.VerdeelWapening?.GrootsteDiameter;

        // stroken
        public double VlaklastG => PermanenteBelastingPerM2;
        public override double EigenGewichtPerM2 => Dikte * 0.001 * 25;
        public double LengteM => Lengte * 1e-3;

        // helpers
        private BendingResults? _basisStrook; // todo generieke strook class maken.

        private void UpdateStrook1()
        {
            var strook = this.Stroken.FirstOrDefault(s => s.Naam.StartsWith("basis"));
            if (strook == null) return;
            strook.Beam.Length = this.Lengte * 1e-3;
            strook.Beam.Materiaal = this.Materiaal;
            strook.Beam.Profiel = strook.Profiel;
            strook.PlaatWapening = this.PlaatWapening!; // Geen Clone() nodig. Deze strook gebruikt dezelfde PlaatWapening als de assemblage, dus we willen dat ze dezelfde reference delen. Wijzigingen in de assemblage moeten direct doorwerken in de strook.

            // ✅ NIEUWE AANPAK: Pas ondergrenzen toe op alle wapeningen
            // (Dit vervangt de oude constraint-checking logica)
            if (strook.PlaatWapening != null)
            {
                // Onder - Hoofd
                if (strook.PlaatWapening.Onder?.BasisWapening != null)
                {
                    var wap = strook.PlaatWapening.Onder.BasisWapening;
                    wap.Tekst = string.IsNullOrWhiteSpace(wap.TekstOndergrens) ? "r6-150" : wap.TekstOndergrens;
                    wap.SetZRef();
                }
                
                // Onder - Verdeel
                if (strook.PlaatWapening.Onder?.VerdeelWapening != null)
                {
                    var wap = strook.PlaatWapening.Onder.VerdeelWapening;
                    wap.Tekst = string.IsNullOrWhiteSpace(wap.TekstOndergrens) ? "r6-300" : wap.TekstOndergrens;
                    wap.SetZRef();
                }
                
                // Boven - Hoofd
                if (strook.PlaatWapening.Boven?.BasisWapening != null)
                {
                    var wap = strook.PlaatWapening.Boven.BasisWapening;
                    wap.Tekst = string.IsNullOrWhiteSpace(wap.TekstOndergrens) ? "r6-300" : wap.TekstOndergrens;
                    wap.SetZRef();
                }
                
                // Boven - Verdeel
                if (strook.PlaatWapening.Boven?.VerdeelWapening != null)
                {
                    var wap = strook.PlaatWapening.Boven.VerdeelWapening;
                    wap.Tekst = string.IsNullOrWhiteSpace(wap.TekstOndergrens) ? "r6-300" : wap.TekstOndergrens;
                    wap.SetZRef();
                }
            }
            
            strook.Beam.PlaatWapening = strook.PlaatWapening;
            strook.Beam.EI = 1e-9 * strook.Beam.Profiel.Iy * strook.Beam.Materiaal?.E ?? 1;

            strook.Beam.Loads.Clear();

            if (Belastingen.BelastingGevallen.Count > 0)
            {
                double werkendeBreedte = 1.0;
                var perm = Belastingen.BelastingGevallen[0];
                var dl1g = new DistributedLoad(perm, "L1~Gk~",0, LengteM, -VlaklastG) { Description = $"{-VlaklastG:0.0} kN/m² × {werkendeBreedte:0.0}m" }; ;
                dl1g.Description = $"e.g. ({EigenGewichtPerM2:0.0})";
                if (AfwerkingVlaklast != 0) dl1g.Description += $" + afw. ({AfwerkingVlaklast:0.0})";
                strook.Beam.Loads.Add(dl1g);

                var veranderlijk = Belastingen.BelastingGevallen[1];
                var dl1q = new DistributedLoad(veranderlijk, "L1~Qk~", 0, LengteM, -veranderlijk.OpgelegdeBelastingen.Vlaklast)
                {
                    Description = "opgelegde belasting vlaklast"
                };
                strook.Beam.Loads.Add(dl1q);

                var pl = new MovingPointLoad(LengteM / 2.12, 10 * -veranderlijk.OpgelegdeBelastingen.Puntlast)
                {
                    StartPos = 0.05,
                    EndPos = LengteM - 0.05,
                    Name = "P1",
                    LoadCase = veranderlijk,
                    Description = "opgelegde belasting puntlast"
                };
            }

            if (Belastingen.BelastingGevallen.Count > 2)
            {
                var bg3 = Belastingen.BelastingGevallen[2];
                var p1 = new PointLoad(-bg3.OpgelegdeBelastingen.Puntlast, bg3)
                {
                    Position = LengteM / 2,
                    Name = "P1",
                    Description = "opgelegde belasting puntlast"
                };
                strook.Beam.Loads.Add(p1);
            }

            strook.BerekenStrook();


            // als de basisstrook te weinig wapening heeft verhoog dan de diameter of verlaag de hoh-maat
            var rMin = strook.BendingResults.OrderBy(r => r.Moment).First();
            var asRequired = rMin.AsRequired;
            var asProvided = rMin.AsApplied;

            var instelling = ProjectInfo?.WapeningAfhandeling ?? WapeningAfhandelingEnum.AlleenVerhogen;
            bool wapeningAanpassen = instelling switch
            {
                WapeningAfhandelingEnum.Gebruiker     => false,                   // nooit aanpassen
                WapeningAfhandelingEnum.AlleenVerhogen => asRequired > asProvided, // alleen bij tekort
                WapeningAfhandelingEnum.Optimaliseer   => true,                   // altijd herberekenen
                _ => asRequired > asProvided
            };
            // Als invoer leeg is, altijd automatisch bepalen
            if (string.IsNullOrWhiteSpace(this.PlaatWapening?.Onder?.BasisWapening?.Tekst))
                wapeningAanpassen = true;

            if (wapeningAanpassen && asRequired > 0)
            {
                // Parse huidige wapening
                var parsed = WapeningOptimizer.ParseWapeningTekst(this.PlaatWapening?.Onder?.BasisWapening?.Tekst);
                if (!parsed.HasValue)
                {
                    Console.WriteLine("⚠️ Kan wapening tekst niet parsen, skip optimalisatie");
                    return;
                }
                
                double currentDiameter = parsed.Value.diameter;
                double currentHoh = parsed.Value.hoh;
                
                // ✅ NIEUW: Gebruik WapeningOptimizer.BepaalPlaatWapeningFixedHoh()
                // Hoh blijft FIXED, alleen diameter wordt verhoogd
                var resultaat = WapeningOptimizer.BepaalPlaatWapeningFixedHoh(
                    asRequired,
                    fixedHoh: currentHoh,
                    currentDiameter: currentDiameter,
                    herberekening: (nieuweD) =>
                    {
                        // ✅ Herbereken AsRequired bij diameter wijziging (nuttige hoogte wijzigt!)
                        // Update tijdelijk de wapening om nieuwe As te berekenen
                        this.PlaatWapening!.Onder!.BasisWapening.Tekst = $"r{nieuweD:0}-{currentHoh:0}";
                        this.PlaatWapening.Onder.BasisWapening.SetZRef();
                        strook.Profiel.Hoogte = this.Dikte;
                        strook.BerekenStrook();
                        
                        var rMinNieuw = strook.BendingResults.OrderBy(r => r.Moment).First();
                        return rMinNieuw.AsRequired;
                    }
                );
                
                // Update wapening met resultaat
                this.PlaatWapening!.Onder!.BasisWapening.Tekst = resultaat.tekst;
                this.PlaatWapening.Onder.BasisWapening.SetZRef();
                
                // ✅ Pas ondergrens toe
                var definitieveWapening = WapeningOptimizer.PasOndergrensToe(
                    resultaat.tekst, 
                    this.PlaatWapening.Onder.BasisWapening.TekstOndergrens);
                
                if (definitieveWapening != resultaat.tekst)
                {
                    this.PlaatWapening.Onder.BasisWapening.Tekst = definitieveWapening;
                    this.PlaatWapening.Onder.BasisWapening.SetZRef();
                }
                
                strook.Profiel.Hoogte = this.Dikte;
                strook.BerekenStrook();
                
                rMin = strook.BendingResults.OrderBy(r => r.Moment).First();
                Console.WriteLine($"✅ Basiswapening onder geoptimaliseerd: {definitieveWapening} (As={rMin.AsApplied:0}mm², benodigd={rMin.AsRequired:0}mm²)");
            }
            else
            {
                // Wapening is voldoende of instelling=Gebruiker: geen aanpassing
                Console.WriteLine($"✅ Basiswapening onder: {this.PlaatWapening?.Onder?.BasisWapening?.Tekst} ({instelling}) – geen aanpassing ({asProvided:0}mm² / benodigd {asRequired:0}mm²)");
            }




        }


        private void UpdateTand()
        {
            if (this.TandOplegging == null)
            {
                Console.WriteLine("Geen oplegging");
                return;
            }

            if (this.Tand == null)
            {
                Console.WriteLine("Geen tand");
                return;
            }

            // Haal reacties op van trap1 en trap2
            var (G, Q) = this.Trap1.Reacties;
            var r2 = this.Trap2.Reacties;

            var belastingCombinaties = this.Belastingen.BelastingCombinaties;

            // Bereken maximale fundamentele waarde voor Trap1
            double maxTrap1 = BerekenMaximaleFundamenteleWaarde(G, Q, belastingCombinaties);

            // Bereken maximale fundamentele waarde voor Trap2
            double maxTrap2 = BerekenMaximaleFundamenteleWaarde(r2.G, r2.Q, belastingCombinaties);

            // Neem de grootste van beide
            double maxOplegReactie = Math.Max(maxTrap1, maxTrap2);

            // Zet in Tand.OplegReactie
            this.Tand.OplegReactie = -maxOplegReactie;

            // geometrie
            this.Tand.TandLengte = this.Trap1.Breedte;
            this.Tand.TandHoogte = this.Hoogte - this.Trap1.Hoogte;
            this.Tand.IsOndertand = true;
            
            // ✅ Initialiseer Oplegging context met juiste delegates voor lengte en rekenwaarde
            this.Tand.Initialize(this.TandOplegging);
            
            // ✅ LEES constraints (maar wijzig ze NOOIT!)
            double constraintDiameter = DetailWapeningDiameterMin ?? 8.0;
            double constraintMaxHoh = DetailWapeningHohMax ?? 150;
            
            // Bereken met constraint waarden
            double currentDiameter = constraintDiameter;
            double currentHoh = constraintMaxHoh;
            
            this.Tand.WapeningAlgemeen.Tekst = $"Ø{currentDiameter:0.#}-{currentHoh:0}";
            this.Tand.DekkingAlgemeen = Math.Max(this.PlaatDekking.Boven.DekkingToe, this.PlaatDekking.Onder.DekkingToe);
            this.Tand.WapeningAlgemeen.DekkingToegepast = this.Tand.DekkingAlgemeen;

            if (this.Tand.BuigingTand != null)
            {
                this.Tand.BuigingTand.Wapening = this.Tand.WapeningAlgemeen;
            }

            // bereken
            this.Tand.Bijwerken();

            // controleer wapening met respect voor constraints
            if (this.Tand.BuigingTand?.AsRequired > this.Tand.WapeningAlgemeen.As)
            {
                // Te weinig wapening → pas hoh aan (verkleinen tot minimaal 50mm)
                double minHoh = 50;
                double targetUtilization = 0.95;
                
                // Stap 1: Probeer hoh te verkleinen
                while (currentHoh >= minHoh)
                {
                    currentHoh -= 5;
                    if (currentHoh < minHoh) 
                        currentHoh = minHoh;
                    
                    this.Tand.WapeningAlgemeen.Tekst = $"Ø{currentDiameter:0.#}-{currentHoh:0}";
                    this.Tand.Bijwerken();
                    
                    if (this.Tand.BuigingTand.AsRequired <= this.Tand.BuigingTand.AsApplied * targetUtilization)
                    {
                        // ✅ Pas ondergrens toe
                        var huidigeWapening = $"Ø{currentDiameter:0.#}-{currentHoh:0}";
                        var definitieveWapening = WapeningOptimizer.PasOndergrensToe(
                            huidigeWapening, 
                            this.Tand.WapeningAlgemeen.TekstOndergrens);
                        
                        this.Tand.WapeningAlgemeen.Tekst = definitieveWapening;
                        this.Tand.Bijwerken();
                        
                        Console.WriteLine($"✅ Detailwapening: hoh aangepast naar {currentHoh}mm (Ø{currentDiameter} gehandhaafd, constraint: Ø{constraintDiameter}-{constraintMaxHoh})");
                        return; // Voldoende met kleinere hoh
                    }
                    
                    if (currentHoh <= minHoh)
                        break;
                }
                
                // Stap 2: Als hoh verkleinen niet helpt, verhoog diameter (maar >= constraint)
                List<double> diameters = [6, 8, 10];
                var beschikbareDiameters = diameters.Where(d => d >= constraintDiameter).ToList();
                
                foreach (var d in beschikbareDiameters.Skip(1)) // Skip eerste (is al geprobeerd)
                {
                    this.Tand.WapeningAlgemeen.Tekst = $"Ø{d:0.#}-{minHoh:0}";
                    this.Tand.Bijwerken();
                    
                    if (this.Tand.BuigingTand.AsRequired <= this.Tand.BuigingTand.AsApplied)
                    {
                        // ✅ Pas ondergrens toe
                        var huidigeWapening = $"Ø{d:0.#}-{minHoh:0}";
                        var definitieveWapening = WapeningOptimizer.PasOndergrensToe(
                            huidigeWapening, 
                            this.Tand.WapeningAlgemeen.TekstOndergrens);
                        
                        this.Tand.WapeningAlgemeen.Tekst = definitieveWapening;
                        this.Tand.Bijwerken();
                        
                        Console.WriteLine($"⚠️ Detailwapening aangepast: {definitieveWapening} (constraint was: Ø{constraintDiameter}-{constraintMaxHoh})");
                        return;
                    }
                }
                
                Console.WriteLine($"❌ Detailwapening: geen oplossing gevonden binnen constraints (Ø{constraintDiameter}-{constraintMaxHoh})!");
            }
            else if (this.Tand.BuigingTand?.AsRequired < this.Tand.WapeningAlgemeen.As)
            {
                // Te veel wapening: verhoog hoh (maar niet boven constraint maximum)
                double targetUtilization = 0.90;
                
                while (currentHoh <= constraintMaxHoh)
                {
                    currentHoh += 5;
                    if (currentHoh > constraintMaxHoh) 
                        currentHoh = constraintMaxHoh;
                    
                    this.Tand.WapeningAlgemeen.Tekst = $"Ø{currentDiameter:0.#}-{currentHoh:0}";
                    this.Tand.Bijwerken();
                    
                    if (this.Tand.BuigingTand.AsRequired >= this.Tand.BuigingTand.AsApplied * targetUtilization)
                    {
                        Console.WriteLine($"✅ Detailwapening: hoh geoptimaliseerd naar {currentHoh}mm (benutting ~90%, binnen constraint)");
                        break;
                    }
                    
                    if (currentHoh >= constraintMaxHoh)
                    {
                        Console.WriteLine($"✅ Detailwapening: Ø{currentDiameter}-{currentHoh} (op constraint maximum, lagere benutting)");
                        break;
                    }
                }
            }
            else
            {
                Console.WriteLine($"✅ Detailwapening: Ø{currentDiameter}-{currentHoh} is voldoende (binnen constraints)");
            }
            
            // ✅ VALIDEER de tand zodat IsValidated correct wordt gezet
            this.Tand.BerekenEnValideer();
        }

        /// <summary>
        /// Berekent de maximale fundamentele waarde (Gk × factorG + Qk × factorQ) 
        /// voor alle fundamentele belastingcombinaties (Type A en B).
        /// </summary>
        public static double BerekenMaximaleFundamenteleWaarde(double gk, double qk, List<BelastingCombinatie> combinaties)
        {
            double maxWaarde = 0;

            // Filter alleen fundamentele combinaties (Type A en B)
            var fundamenteleCombinaties = combinaties.Where(bc =>
                bc.Type == BelastingCombinatieTypeEnum.Fundamenteel_A ||
                bc.Type == BelastingCombinatieTypeEnum.Fundamenteel_B).ToList();

            foreach (var combinatie in fundamenteleCombinaties)
            {
                // Voor elke combinatie: som van (Gk × factorG + Qk × factorQ)
                double waarde = 0;

                foreach (var item in combinatie.Items)
                {
                    if (item.Geval.Type == BelastingGeval.BelastingGevalTypeEnum.Permanent)
                    {
                        // Permanent belastinggeval: gebruik Gk × factorG
                        waarde += gk * item.FactorG;
                    }
                    else if (item.Geval.Type == BelastingGeval.BelastingGevalTypeEnum.Veranderlijk)
                    {
                        // Veranderlijk belastinggeval: gebruik Qk × factorNetto (bevat al MomentFactor)
                        waarde += qk * item.FactorNetto;
                    }
                }

                // Bewaar de maximale waarde
                if (waarde > maxWaarde)
                {
                    maxWaarde = waarde;
                }
            }

            return maxWaarde;
        }

        private void UpdateStrook2()
        {
            var strook = this.Stroken.FirstOrDefault(s => s.Naam == ("versterkte strook"));
            if (strook == null) return;

            if (strook.Profiel != null)
            {
                strook.Profiel.Breedte = BreedteVersterkteStrook;
                strook.Profiel.B = BreedteVersterkteStrook;
                
                strook.Profiel.Hoogte = Dikte;
                strook.Profiel.H = Dikte;

                
            }


            var beam = strook.Beam;
            double L = this.Lengte * 1e-3;


            beam.Length = L;
            beam.Materiaal = this.Materiaal;
            beam.Profiel = strook.Profiel;
            beam.StartSupport = SupportType.Pin;
            beam.EndSupport = SupportType.Pin;  

            // begin met een lege 
            beam.Loads.Clear();

            if (Belastingen.BelastingGevallen.Count < 2)
            {
                return; 
            }

            var bg1 = Belastingen.BelastingGevallen[0];
            var bg2 = Belastingen.BelastingGevallen[1];
            
            // vlaklast
            var vlaklastG = this.VlaklastG;
            var belastingBreedteMM = strook.Profiel?.Breedte ?? 1000;
            var werkendeBreedte = belastingBreedteMM * 0.001;
            var q1G = Math.Round(-vlaklastG * werkendeBreedte, 2);
            var q1Q = Math.Round(-bg2.OpgelegdeBelastingen.Vlaklast * werkendeBreedte,2);

            int nr = 1;

            DistributedLoad dl1G = new(bg1, $"L{nr}~Gk~", 0, L, q1G) { Description = $"{-vlaklastG:0.0} kN/m² × {werkendeBreedte:0.0}m" };
            beam.Loads.Add(dl1G);
            DistributedLoad dl1Q = new(bg2, $"L{nr++}~Qk~", 0, L, q1Q) { Description = $"{-bg2.OpgelegdeBelastingen.Vlaklast:0.0} kN/m² × {werkendeBreedte:0.0}m" };
            beam.Loads.Add(dl1Q);



            foreach (var l in GetTrapDistrubutedLoads(this, Trap1, bg1, bg2, $"L{nr++}~Qk~"))
            {
                beam.Loads.Add(l);
            }
            foreach (var l in GetTrapDistrubutedLoads(this, Trap2, bg1, bg2, $"L{nr++}~Qk~"))
            {
                beam.Loads.Add(l);
            }






            strook.PlaatWapening = this.PlaatWapening!.Clone();
            
            // ✅ Herstel referenties naar PlaatDekking (anders blijven oude waarden uit clone staan!)
            if (strook.PlaatWapening.Boven != null)
            {
                strook.PlaatWapening.Boven.DekkingBuitensteLaag = this.PlaatDekking.Boven;
            }
            if (strook.PlaatWapening.Onder != null)
            {
                strook.PlaatWapening.Onder.DekkingBuitensteLaag = this.PlaatDekking.Onder;
            }
            

            
            var strookbreedte = belastingBreedteMM;
            strook.PlaatWapening.Onder!.BasisWapening.ReferentieLengte = strookbreedte;
            
            strook.PlaatWapening.Boven!.BasisWapening.ReferentieLengte = strookbreedte;

            strook.Beam.PlaatWapening = strook.PlaatWapening;
            
            strook.Beam.EI = 1e-9 * strook.Beam.Profiel?.Iy * strook.Beam.Materiaal?.E ?? 1;

            strook.BerekenStrook();


            // ========================================================================
            // BIJLEGWAPENING BEPALEN (met WapeningOptimizer)
            // ========================================================================
            
            var rMin = strook.BendingResults.OrderBy(r => r.Moment).First();
            var asRequired = rMin.AsRequired;
            var asProvided = rMin.AsApplied;
            
            if (asRequired > asProvided)
            {
                var bijlegReqOnder = asRequired - asProvided;
                
                // ✅ NIEUW: Gebruik WapeningOptimizer.BepaalBijlegWapening()
                var resultaatOnder = WapeningOptimizer.BepaalBijlegWapening(
                    bijlegReqOnder,
                    belastingBreedteMM,
                    dMin: 6, // todo parse constraints uit PlaatWapening.Onder.BasisWapening.TekstOndergrens
                    nMin: 2, // todo parse constraints uit PlaatWapening.Onder.BasisWapening.TekstOndergrens
                    dGemiddeld: strook.PlaatWapening.Onder?.BasisWapening.GemiddeldeDiameter ?? 6,
                    dNuttig: rMin.D
                );
                
                // Update WapeningContext
                BijlegWapeningOnder ??= new WapeningContext();
                BijlegWapeningOnder.Tekst = resultaatOnder.tekst;
                
                // ✅ Pas ondergrens toe op bijlegwapening
                var definitieveBijlegOnder = WapeningOptimizer.PasOndergrensToe(
                    resultaatOnder.tekst,
                    BijlegWapeningOnder.TekstOndergrens);
                BijlegWapeningOnder.Tekst = definitieveBijlegOnder;
                
                BijlegWapeningOnder.ReferentieVlak = ReferentieVlakEnum.Onder;
                BijlegWapeningOnder.LaagNummer = PlaatWapening?.Onder?.BasisWapening.LaagNummer ?? 2;
                BijlegWapeningOnder.DekkingToegepast = PlaatWapening?.Onder?.BasisWapening.DekkingToegepast ?? 30;
                BijlegWapeningOnder.ReferentieLengte = belastingBreedteMM;
                
                strook.Beam.PlaatWapening.Onder.BasisWapening.Tekst += $"+{BijlegWapeningOnder.Tekst}";
                
                Console.WriteLine($"✅ Bijlegwapening onder: {definitieveBijlegOnder} (As={resultaatOnder.asProvided:0}mm², benodigd={bijlegReqOnder:0}mm²)");
                
                // ✅ BOVENWAPENING: 50% van onderwapening
                var bijlegReqBoven = bijlegReqOnder * 0.5;
                
                var resultaatBoven = WapeningOptimizer.BepaalBijlegWapening(
                    bijlegReqBoven,
                    belastingBreedteMM,
                    dMin: 8, // todo parse constraints uit PlaatWapening.Boven.BasisWapening.TekstOndergrens
                    nMin: 2, // todo parse constraints uit PlaatWapening.Boven.BasisWapening.TekstOndergrens
                    dGemiddeld: strook.PlaatWapening.Boven?.BasisWapening.GemiddeldeDiameter ?? 6,
                    dNuttig: 170 // Schatting voor boven (minder kritisch)
                );
                
                // Update WapeningContext
                BijlegWapeningBoven ??= new WapeningContext();
                BijlegWapeningBoven.Tekst = resultaatBoven.tekst;
                
                // ✅ Pas ondergrens toe op bijlegwapening boven
                var definitieveBijlegBoven = WapeningOptimizer.PasOndergrensToe(
                    resultaatBoven.tekst,
                    BijlegWapeningBoven.TekstOndergrens);
                BijlegWapeningBoven.Tekst = definitieveBijlegBoven;
                
                BijlegWapeningBoven.ReferentieVlak = ReferentieVlakEnum.Boven;
                BijlegWapeningBoven.LaagNummer = PlaatWapening?.Boven?.BasisWapening.LaagNummer ?? 1;
                BijlegWapeningBoven.DekkingToegepast = PlaatWapening?.Boven?.BasisWapening.DekkingToegepast ?? 30;
                BijlegWapeningBoven.ReferentieLengte = belastingBreedteMM;
                
                strook.Beam.PlaatWapening.Boven.BasisWapening.Tekst += $"+{BijlegWapeningBoven.Tekst}";
                
                //Console.WriteLine($"✅ Bijlegwapening boven: {definitieveBijlegBoven} (As={resultaatBoven.asProvided:0}mm², benodigd={bijlegReqBoven:0}mm²)");
                
                // ✅ HERBEREKEN met bijlegwapening (anders blijft D verkeerd in tabel!)
                strook.BerekenStrook();
                //Console.WriteLine($"[DEBUG] NA bijleg toevoegen en herberekenen - D = {strook.BendingResults.OrderBy(r => r.Moment).First().D:0.#}mm");
            }
            else
            {
                // Geen bijleg nodig
                BijlegWapeningOnder = null;
                BijlegWapeningBoven = null;
                //Console.WriteLine("[INFO] Geen bijlegwapening nodig");
            }



        }


        public static (double G, double Q) GetTrapBelasting(SteekTrapEntity t)
        {
            return (t.ReactieG, t.ReactieQ);
        }

        public static double GetDompFactor(BordesEntity bordes)
        {
            return bordes.Breedte / (bordes.Breedte - bordes.BreedteVersterkteStrook * 0.5);
        }

        public static List<DistributedLoad> GetTrapDistrubutedLoads(BordesEntity bordes, VerbindingAansluitendElement t, BelastingGeval bg1, BelastingGeval bg2, string name)
        {
            if (t == null) 
                return [];

            double dompFactor = GetDompFactor(bordes);
            




            if (t.AansluitendElement is SteekTrapEntity steekTrap)
            {
                double gValue = -steekTrap.ReactieG * dompFactor;
                var g = new DistributedLoad(bg1, name, t.PosM.Start, t.PosM.End, Math.Round(gValue,2));

                double qValue = -steekTrap.ReactieQ * dompFactor;
                var q = new DistributedLoad(bg2, name, t.PosM.Start, t.PosM.End, Math.Round(qValue,2));

                // omschrijving
                g.Description = $"uit {steekTrap.Merk} (R<sub>Gk</sub> = {steekTrap.ReactieG:0.##}) × {dompFactor:0.##} (domp)";
                q.Description = $"uit {steekTrap.Merk} (R<sub>Qk</sub> = {steekTrap.ReactieQ:0.##}) × {dompFactor:0.##} (domp)";

                return [g, q];
            }

            if (t.GebruikEigenOpgave)
            {
                double gValue = -t.Reacties.G * dompFactor;
                var g = new DistributedLoad(bg1, name, t.PosM.Start, t.PosM.End, Math.Round(gValue, 2));

                double qValue = -t.Reacties.Q * dompFactor;
                var q = new DistributedLoad(bg2, name, t.PosM.Start, t.PosM.End, Math.Round(qValue, 2));

                // omschrijving
                g.Description = $"R<sub>Gk</sub> = {t.Reacties.G:0.##} × {dompFactor:0.##} (domp)";
                q.Description = $"R<sub>Qk</sub> = {t.Reacties.Q:0.##} × {dompFactor:0.##} (domp)";
                
                return [g, q];
            }

                return [];
        }

        public double GetTrapQd(SteekTrapEntity t)
        {
            var fundA = ProjectInfo.Grondslagen?.GetFactorFundamenteelA();
            var mom0 = 1.0;
            var bg2 = Belastingen.BelastingGevallen.FirstOrDefault(g => g.Naam == "BG2");
            if (bg2 != null)
            {
                mom0 = bg2.MomentaanFactoren.Mom0;
            }

            var klasse = Gebruiksklasse.GetValueOrDefault();


            var fundB = ProjectInfo.Grondslagen?.GetFactorenFundamenteelB(mom0);
            var qA = t.ReactieG * fundA?.G + t.ReactieQ * fundA?.Q;
            var qB = t.ReactieG * fundB?.G + t.ReactieQ * fundB?.Q;


            var qd = Math.Max(qA ?? 100, qB ?? 100);

            return qd;
        }





        /// <summary>
        /// Past ondergrenzen toe op alle wapeningen in PlaatWapening.
        /// Roep deze methode aan NA optimalisatie/berekening van wapening.
        /// </summary>
        private void PasOndergrenzenToe()
        {
            if (PlaatWapening == null) return;

            // Boven - Hoofd
            if (PlaatWapening.Boven?.BasisWapening != null)
            {
                var wap = PlaatWapening.Boven.BasisWapening;
                var definitief = WapeningOptimizer.PasOndergrensToe(wap.Tekst, wap.TekstOndergrens);
                if (definitief != wap.Tekst)
                {
                    wap.Tekst = definitief;
                    wap.SetZRef();
                }
            }

            // Boven - Verdeel
            if (PlaatWapening.Boven?.VerdeelWapening != null)
            {
                var wap = PlaatWapening.Boven.VerdeelWapening;
                var definitief = WapeningOptimizer.PasOndergrensToe(wap.Tekst, wap.TekstOndergrens);
                if (definitief != wap.Tekst)
                {
                    wap.Tekst = definitief;
                    wap.SetZRef();
                }
            }

            // Onder - Hoofd
            if (PlaatWapening.Onder?.BasisWapening != null)
            {
                var wap = PlaatWapening.Onder.BasisWapening;
                var definitief = WapeningOptimizer.PasOndergrensToe(wap.Tekst, wap.TekstOndergrens);
                if (definitief != wap.Tekst)
                {
                    wap.Tekst = definitief;
                    wap.SetZRef();
                }
            }

            // Onder - Verdeel
            if (PlaatWapening.Onder?.VerdeelWapening != null)
            {
                var wap = PlaatWapening.Onder.VerdeelWapening;
                var definitief = WapeningOptimizer.PasOndergrensToe(wap.Tekst, wap.TekstOndergrens);
                if (definitief != wap.Tekst)
                {
                    wap.Tekst = definitief;
                    wap.SetZRef();
                }
            }

            // Bijlegwapening Onder
            if (BijlegWapeningOnder != null)
            {
                var definitief = WapeningOptimizer.PasOndergrensToe(BijlegWapeningOnder.Tekst, BijlegWapeningOnder.TekstOndergrens);
                if (definitief != BijlegWapeningOnder.Tekst)
                {
                    BijlegWapeningOnder.Tekst = definitief;
                    BijlegWapeningOnder.SetZRef();
                }
            }

            // Bijlegwapening Boven
            if (BijlegWapeningBoven != null)
            {
                var definitief = WapeningOptimizer.PasOndergrensToe(BijlegWapeningBoven.Tekst, BijlegWapeningBoven.TekstOndergrens);
                if (definitief != BijlegWapeningBoven.Tekst)
                {
                    BijlegWapeningBoven.Tekst = definitief;
                    BijlegWapeningBoven.SetZRef();
                }
            }

            // Tand wapening
            if (Tand?.WapeningAlgemeen != null)
            {
                var wap = Tand.WapeningAlgemeen;
                var definitief = WapeningOptimizer.PasOndergrensToe(wap.Tekst, wap.TekstOndergrens);
                if (definitief != wap.Tekst)
                {
                    wap.Tekst = definitief;
                    wap.SetZRef();
                }
            }
        }


        public override void Bijwerken()
        {
            UpdateStrook1();
            UpdateStrook2();
            UpdateTand();
            
            // ✅ Pas ondergrenzen toe op alle wapeningen
            PasOndergrenzenToe();

            // ✅ Brandwerendheid bijwerken
            MainSlab?.UpdateRei();

            // ✅ Roep base aan zodat validatie wordt gemaakt
            base.Bijwerken();

            // Toetsen bijwerken
            ToetsenBijwerken();
            

        }

        private void ToetsenBijwerken()
        {
            ClearToetsen();
            foreach (var strook in Stroken)
            {
                AddToetsen(strook.BendingResults);
                AddToetsen(strook.ScheurwijdteCollectie);
            }

            AddToets(Tand);
            AddToets(Tand.Oplegging);
            AddToets(PlaatDekking);
            AddToets(PlaatDekking.Boven);
            AddToets(PlaatDekking.Onder);
           



        }

        protected override void ValidateAssemblage()
        {
            if (Validation == null) return;

            foreach (var item in Toetsen)
            {
                if (item.Meldingen.Count > 0)
                {
                    foreach (var melding in item.Meldingen)
                    {
                        Console.WriteLine($"{melding.Type} : {item.Heading} : {melding.Bericht}");
                    }
                }
               
            }

            // Check stroken voor dwarskrachtwapening
            foreach (var strook in Stroken)
            {
                if (strook.DwarskrachtCollectie.Any(v => v.IsDwarskrachtWapeningBenodigd()))
                {
                    var vMax = strook.DwarskrachtCollectie.OrderByDescending(v => v.Ved).FirstOrDefault();
                    Validation.AddWarning(
                        $"Strook '{strook.Naam}': dwarskrachtwapening benodigd",
                        $"VEd = {vMax?.Ved:0.#} kN > VRd,c = {vMax?.DwarskrachtWeerstand:0.#} kN"
                    );
                }

                // Check buigwapening via Meldingen van BendingResults
                foreach (var bendingResult in strook.BendingResults)
                {
                    if (bendingResult.Meldingen.Any())
                    {
                        foreach (var melding in bendingResult.Meldingen)
                        {
                            if (melding.Type == MeldingType.Error)
                            {
                                Validation.AddError($"Strook '{strook.Naam}': {melding.Bericht}");
                            }
                            else if (melding.Type == MeldingType.Waarschuwing)
                            {
                                Validation.AddWarning($"Strook '{strook.Naam}': {melding.Bericht}");
                            }
                        }
                    }
                }

                

            }

            // Check tand
            if (Tand != null)
            {
                if (Tand.DwarskrachtTand != null &&
                    Math.Abs(Tand.DwarskrachtTand.Ved) > Tand.DwarskrachtTand.DwarskrachtWeerstandBeton)
                {
                    Validation.AddError("Tand: dwarskrachtwapening nodig (niet toegestaan)");
                }
            }
        }

        



        protected void Bereken()
        {
            double tempM = 0.125 * -10 * Math.Pow(Lengte * 0.001, 2);
            UpdateBasisStrook(tempM, 1000);

            var wap = _basisStrook!.Wapening;

            // Als basiswapening al voldoende is → geen bijleg nodig
            if (_basisStrook.AsRequired <= wap.AsBasis)
            {
                wap.AantalBijlegStaven = 0;
                wap.DiameterBijlegStaven = 0;
                return;
            }

            // Beschikbare diameters (klein → groot)
            List<double> diameters = [8, 10, 12, 16, 20, 25, 32, 40, 50];

            bool gevonden = false;

            // Loop diameters van klein naar groot
            foreach (double d in diameters)
            {
                // Probeer 1 t/m d/4 staven voor déze diameter
                for (int aantal = 1; aantal <= d/4; aantal++)
                {
                    // Stel tijdelijk in (zodat AsBijleg e.d. berekend/gelezen kunnen worden)
                    wap.AantalBijlegStaven = aantal;
                    wap.DiameterBijlegStaven = d;

                    // Bereken AsProvided: basis + bijleg
                    double AsProvided = wap.AsBasis + BerekenAsBijleg(aantal, d);

                    if (AsProvided >= _basisStrook.AsRequired)
                    {
                        // Gevonden: bewaar deze configuratie en stop
                        gevonden = true;
                        break; // break out of for (aantal)
                    }
                }

                if (gevonden)
                {
                    // break out of foreach (diameters)
                    break;
                }
                // anders: ga door naar volgende diameter (groter)
            }

            if (!gevonden)
            {
                // Geen oplossing gevonden met max 4 staven voor alle diameters
                Console.WriteLine("⚠️ Geen voldoende bijleg gevonden met maximaal 4 staven per diameter.");
                // Optioneel: zet aantal=4 van grootste diameter als 'beste poging'
                var laatsteDiameter = diameters.Last();
                wap.DiameterBijlegStaven = laatsteDiameter;
                wap.AantalBijlegStaven = 4;
            }
        }

        // Helper: eenvoudige doorsnede berekening voor bijleg (mm → mm^2)
        private static double BerekenAsBijleg(int aantal, double diameterMm)
        {
            // doorsnede per staaf = π * d^2 / 4
            // diameter wordt in mm gegeven; As in mm^2
            double perStaaf = Math.PI * Math.Pow(diameterMm, 2) / 4.0;
            return aantal * perStaaf;
        }



        protected void SetAndRecalcultate<T>(ref T field, T value)
        {
            if (SetProperty(ref field, value))
                Bereken();

            
        }

        public override double Lengte
        {
            get => _lengte;
            set => SetAndRecalcultate(ref _lengte, value);
            
        }
        
        public override double Breedte
        {
            get => _breedte;
            set
            {
                // ✅ Sync zou hier kunnen met MainPlate.Geometry.Width (toekomstig)
                // Voor nu: alleen backing field
                SetAndRecalcultate(ref _breedte, value);
            }
        }

        public override double Hoogte => Dikte;

        public double BreedteVersterkteStrook
        {
            get => _breedteVersterkteStrook;
            set => SetAndRecalcultate(ref _breedteVersterkteStrook, value);
        }

        /// <summary>
        /// GUID van Trap1.AansluitendElement (voor serialisatie)
        /// </summary>
        public Guid? Trap1AansluitendElementGuid { get; set; }

        /// <summary>
        /// GUID van Trap2.AansluitendElement (voor serialisatie)
        /// </summary>
        public Guid? Trap2AansluitendElementGuid { get; set; }


        public double Dikte
        {
            get => MainSlab?.Dikte ?? _dikte; // ✅ Lees van MainPlate indien beschikbaar
            set
            {
                // ✅ Sync met MainPlate
                if (MainSlab != null)
                {
                    MainSlab.Dikte = value;
                }
                
                // ✅ Update backing field en trigger recalculatie
                SetAndRecalcultate(ref _dikte, value);
            }
        }

        /// <summary>
        /// Via projectinfo gezette grondslagen
        /// </summary>
        [JsonIgnore]
        public GrondslagenContext? Grondslagen
        {
            get => _grondslagen;
            set => SetProperty(ref _grondslagen, value);
        }
        
        
        //public BetonDekkingContext? Dekking
        //{
        //    get => _dekking;
        //    set => SetNestedProperty(ref _dekking, value);
        //}

        public PlaatWapening? PlaatWapening
        {
            get => _plaatWapening;
            set => SetNestedProperty(ref _plaatWapening, value);
        }

        private WapeningContext? _bijlegWapening;
        public WapeningContext? BijlegWapeningOnder
        {
            get => _bijlegWapening;
            set => SetNestedProperty(ref _bijlegWapening, value);
        }

        private WapeningContext? _bijlegWapeningBoven;
        public WapeningContext? BijlegWapeningBoven
        {
            get => _bijlegWapeningBoven;
            set => SetNestedProperty(ref _bijlegWapeningBoven, value);
        }

       

        /// <summary>
        /// Minimale diameters voor plaatwapening constraints - DEPRECATED: gebruik Constraints
        /// </summary>
        [Obsolete("Gebruik BijlegOnderConstraint.DiameterMin")]
        public double? OnderHoofdDiameterMin { get; set; } = 6.0;
        [Obsolete("Gebruik OnderVerdeelConstraint.DiameterMin")]
        public double? OnderVerdeelDiameterMin { get; set; } = 6.0;
        [Obsolete("Gebruik BovenHoofdConstraint.DiameterMin")]
        public double? BovenHoofdDiameterMin { get; set; } = 6.0;
        [Obsolete("Gebruik BovenVerdeelConstraint.DiameterMin")]
        public double? BovenVerdeelDiameterMin { get; set; } = 6.0;

        // Backwards compatibility properties

        public double? _detailWapeningDiameterMin = 6;
        public double? DetailWapeningDiameterMin
        {
            get => _detailWapeningDiameterMin;
            set => _detailWapeningDiameterMin = value;
        }

        public double? _detailWapeningHohMax = 100;
        public double? DetailWapeningHohMax
        {
            get => _detailWapeningHohMax;
            set => _detailWapeningHohMax = value;
        }


        public BendingResults? BasisStrook
        {
            get => _basisStrook;
            set => SetNestedProperty(ref _basisStrook, value);
        }



        public void InitBasisStrook()
        {
            _basisStrook = new BendingResults()
            {
                Beton = Beton ?? new(),
                ConstructiefModel = Schematisering.ConstructiefModelEnum.Plaat,
                Profiel = new()
                {
                    Breedte = 900,
                    Hoogte = this.Dikte
                },
                Name = "Basisstrook",
                Snedekrachten = new() { My = -23.1 },
                Wapening = new() 
                { 
                    Tekst = "r6-150",
                    ReferentieVlak = ReferentieVlakEnum.Onder,
                    ReferentieDekking = 30,
                    ReferentieLengte = 1000,
                },

                


            };
            _basisStrook.BerekenEnValideer();

            _plaatWapening??= new PlaatWapening();
            _plaatWapening.Onder = new PlaatWapeningGroep()
            {
                Heading = "plaatwapening onder",
                DekkingBuitensteLaag = PlaatDekking.Onder,
                BasisWapening = _basisStrook.Wapening.Clone(), // ✅ Clone om gedeelde referentie te voorkomen
                VerdeelWapening = new WapeningContext()
                {
                    Tekst = "r6-250",
                    ReferentieVlak = ReferentieVlakEnum.Onder,
                    ReferentieLengte = 1000,
                },
                LaagHoofdwapening = 2,
                DiameterVerdeel = 8,

            };
            _plaatWapening.Boven = new PlaatWapeningGroep()
            {
                Heading = "plaatwapening boven",
                DekkingBuitensteLaag = PlaatDekking.Boven,
                BasisWapening = _basisStrook.Wapening.Clone(), // ✅ Clone om gedeelde referentie te voorkomen
                VerdeelWapening = new WapeningContext()
                {
                    Tekst = "r6-250",
                    ReferentieVlak = ReferentieVlakEnum.Boven,
                    ReferentieLengte = 1000,
                },
                LaagHoofdwapening = 2,
                DiameterVerdeel = 8,
            };

            Console.WriteLine($"✅ [BordesEntity.InitBasisStrook] PlaatWapening geïnitialiseerd");
            Console.WriteLine($"   _basisStrook.Wapening HashCode: {_basisStrook.Wapening.GetHashCode()}");
            Console.WriteLine($"   Onder.BasisWapening HashCode: {_plaatWapening.Onder.BasisWapening.GetHashCode()}");
            Console.WriteLine($"   Boven.BasisWapening HashCode: {_plaatWapening.Boven.BasisWapening.GetHashCode()}");
            Console.WriteLine($"   Zijn Boven en Onder hetzelfde? {ReferenceEquals(_plaatWapening.Boven.BasisWapening, _plaatWapening.Onder.BasisWapening)}");



        }
        public void UpdateBasisStrook(double moment, double breedte)
        {
            if (_basisStrook == null)
                return;



            _basisStrook.Profiel??= new();
            _basisStrook.Profiel.Breedte = breedte;
            _basisStrook.Profiel.Hoogte = this.Dikte;

            _basisStrook.Snedekrachten??= new();
            _basisStrook.Snedekrachten.My = moment;

            //_basisStrook.Wapening.Breedte = breedte;
            _basisStrook.Wapening.ReferentieLengte = breedte;



        }

        /// <summary>
        /// ✅ NIEUW: Creëer MainPart (PlatePart) voor bordes.
        /// Wordt aangeroepen in constructor.
        /// PlaatDekking en PlaatWapening worden later gezet via Init().
        /// </summary>
        private SlabPart CreateMainPart()
        {
            return new SlabPart
            {
                Name = "Bordesplaat",
                Dikte = 200, // Default waarde (wordt gesynchroniseerd in Init)
                Material = this.Materiaal, // ✅ Shared reference
                ParentAssemblage = this
                // ⚠️ PlaatDekking en PlaatWapening worden gezet in Init() / RestoreReferencesAfterDeserialization()
            };
        }

        /// <summary>
        /// ✅ NIEUW: Helper property voor type-safe access naar MainPart.
        /// Returns null indien MainPart geen Slab is.
        /// </summary>
        [JsonIgnore]
        public SlabPart? MainSlab => MainPart as SlabPart;


    }
}
