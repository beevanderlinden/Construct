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

            // ⚠️ OPMERKING: PlaatDekking kan hier NIET geïnitialiseerd worden omdat ProjectInfo nog null is
            // Dit gebeurt later in Init() of AddAssemblage.razor na koppeling aan project

            // ✅ Genereer BelastingCombinaties zodat Beam.LoadContext correct werkt
            Belastingen.GenereerBelastingCombinaties(
                Belastingen,
                Belastingen.BelastingGevallen,
                Belastingen.CombinatiesTypes);

            Trap1 = new VerbindingAansluitendElement(this);
            Trap2 = new VerbindingAansluitendElement(this);
            Trap2.Gespiegeld = true;

            TandOplegging = new() { };
            Tand = new TandOplegging(this, this.TandOplegging) ;


            this.InitBasisStrook();
            if (_basisStrook != null)
                AddToets(_basisStrook);


            var qbasis = -10;

            var basis = new StrookEntity();
            basis.Father = this;
            basis.Profiel = _basisStrook?.Profiel?? new();
            basis.Beam.Length = this.Lengte * 1e-3;
            basis.Beam.StartSupport = Mechanica.SimpleBeam.SupportType.Pin;
            basis.Beam.EndSupport = Mechanica.SimpleBeam.SupportType.Pin;

            var dl1 = new DistributedLoad(0, basis.Beam.Length, qbasis);
            dl1.LoadCase = Belastingen.BelastingGevallen[0];
            

            basis.Beam.Loads.Add(dl1);
            basis.Naam = "basisstrook";

            this.AddStrook(basis);

            var vs = new StrookEntity();
            vs.Naam = "versterkte strook";
            vs.Father = this;
            vs.Profiel = _versterkteStrook?.Profiel ?? new();
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




        }

        public override void RestoreReferencesAfterDeserialization(ProjectEntity project)
        {
            // ✅ EERST: Algemeen deel (Belastingen, ProjectInfo, Materiaal, etc.)
            base.RestoreReferencesAfterDeserialization(project);

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

            // ✅ Initialiseer PlaatDekking met project eigenschappen
            InitializePlaatDekking();

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

        public OpleggingContext TandOplegging = new() { };
        public TandOplegging Tand { get; set; }
        
        /// <summary>
        /// Wapening constraints voor berekeningen
        /// </summary>
        public WapeningConstraint BijlegBovenConstraint { get; set; } = new(8, 2, null);
        public WapeningConstraint BijlegOnderConstraint { get; set; } = new(8, 2, null);
        public WapeningConstraint DetailWapeningConstraint { get; set; } = new(6, null, 125);
        public WapeningConstraint OnderHoofdConstraint { get; set; } = new(6, null, 150);
        public WapeningConstraint OnderVerdeelConstraint { get; set; } = new(6, null, 250);
        public WapeningConstraint BovenHoofdConstraint { get; set; } = new(6, null, 150);
        public WapeningConstraint BovenVerdeelConstraint { get; set; } = new(6, null, 250);
        
        /// <summary>
        /// Berekende/huidige diameter detailwapening
        /// </summary>
        public double? DetailWapeningDiameter => PlaatWapening?.Boven?.VerdeelWapening?.GrootsteDiameter;

        // stroken
        public double VlaklastG => EigenGewicht + AfwerkingVlaklast;
        public double EigenGewicht => Dikte * 0.001 * 25;
        public double LengteM => Lengte * 1e-3;

        // helpers
        private BendingResults? _basisStrook; // todo generieke strook class maken.
        private BendingResults? _versterkteStrook;

        private void UpdateStrook1()
        {
            var strook = this.Stroken.FirstOrDefault(s => s.Naam.StartsWith("basis"));
            if (strook == null) return;
            strook.Beam.Length = this.Lengte * 1e-3;
            strook.Beam.Materiaal = this.Materiaal;
            strook.Beam.Profiel = strook.Profiel;
            strook.PlaatWapening = this.PlaatWapening; // Geen Clone() nodig. Deze strook gebruikt dezelfde PlaatWapening als de assemblage, dus we willen dat ze dezelfde reference delen. Wijzigingen in de assemblage moeten direct doorwerken in de strook.

            // Pas constraints toe ALLEEN indien huidige waarden de constraints schenden
            if (strook.PlaatWapening != null)
            {
                // ONDER: BasisWapening (hoofdwapening)
                if (strook.PlaatWapening.Onder?.BasisWapening != null)
                {
                    var parsed = ParseWapeningTekst(strook.PlaatWapening.Onder.BasisWapening.Tekst);
                    if (parsed.HasValue)
                    {
                        double diameter = parsed.Value.diameter;
                        double hoh = parsed.Value.hoh;
                        bool aangepast = false;
                        
                        // Check DiameterMin constraint
                        double minDiam = OnderHoofdConstraint?.DiameterMin ?? 0;
                        if (minDiam > 0 && diameter < minDiam)
                        {
                            Console.WriteLine($"[CONSTRAINT] Onder.BasisWap: diameter {diameter}→{minDiam}");
                            diameter = minDiam;
                            aangepast = true;
                        }
                        
                        // Check HohMax constraint
                        double maxHoh = OnderHoofdConstraint?.HohMax ?? 9999;
                        if (maxHoh < 9999 && hoh > maxHoh)
                        {
                            Console.WriteLine($"[CONSTRAINT] Onder.BasisWap: hoh {hoh}→{maxHoh}");
                            hoh = maxHoh;
                            aangepast = true;
                        }
                        
                        if (aangepast)
                        {
                            strook.PlaatWapening.Onder.BasisWapening.Tekst = $"r{diameter:0}-{hoh:0}";
                        }
                    }
                }
                
                // ONDER: VerdeelWapening
                if (strook.PlaatWapening.Onder?.VerdeelWapening != null)
                {
                    var parsed = ParseWapeningTekst(strook.PlaatWapening.Onder.VerdeelWapening.Tekst);
                    if (parsed.HasValue)
                    {
                        double diameter = parsed.Value.diameter;
                        double hoh = parsed.Value.hoh;
                        bool aangepast = false;
                        
                        double minDiam = OnderVerdeelConstraint?.DiameterMin ?? 0;
                        if (minDiam > 0 && diameter < minDiam)
                        {
                            Console.WriteLine($"[CONSTRAINT] Onder.VerdeelWap: diameter {diameter}→{minDiam}");
                            diameter = minDiam;
                            aangepast = true;
                        }
                        
                        double maxHoh = OnderVerdeelConstraint?.HohMax ?? 9999;
                        if (maxHoh < 9999 && hoh > maxHoh)
                        {
                            Console.WriteLine($"[CONSTRAINT] Onder.VerdeelWap: hoh {hoh}→{maxHoh}");
                            hoh = maxHoh;
                            aangepast = true;
                        }
                        
                        if (aangepast)
                        {
                            strook.PlaatWapening.Onder.VerdeelWapening.Tekst = $"r{diameter:0}-{hoh:0}";
                        }
                    }
                }
                
                // BOVEN: BasisWapening
                if (strook.PlaatWapening.Boven?.BasisWapening != null)
                {
                    var parsed = ParseWapeningTekst(strook.PlaatWapening.Boven.BasisWapening.Tekst);
                    if (parsed.HasValue)
                    {
                        double diameter = parsed.Value.diameter;
                        double hoh = parsed.Value.hoh;
                        bool aangepast = false;
                        
                        double minDiam = BovenHoofdConstraint?.DiameterMin ?? 0;
                        if (minDiam > 0 && diameter < minDiam)
                        {
                            Console.WriteLine($"[CONSTRAINT] Boven.BasisWap: diameter {diameter}→{minDiam}");
                            diameter = minDiam;
                            aangepast = true;
                        }
                        
                        double maxHoh = BovenHoofdConstraint?.HohMax ?? 9999;
                        if (maxHoh < 9999 && hoh > maxHoh)
                        {
                            Console.WriteLine($"[CONSTRAINT] Boven.BasisWap: hoh {hoh}→{maxHoh}");
                            hoh = maxHoh;
                            aangepast = true;
                        }
                        
                        if (aangepast)
                        {
                            strook.PlaatWapening.Boven.BasisWapening.Tekst = $"r{diameter:0}-{hoh:0}";
                        }
                    }
                }
                
                // BOVEN: VerdeelWapening
                if (strook.PlaatWapening.Boven?.VerdeelWapening != null)
                {
                    var parsed = ParseWapeningTekst(strook.PlaatWapening.Boven.VerdeelWapening.Tekst);
                    if (parsed.HasValue)
                    {
                        double diameter = parsed.Value.diameter;
                        double hoh = parsed.Value.hoh;
                        bool aangepast = false;
                        
                        double minDiam = BovenVerdeelConstraint?.DiameterMin ?? 0;
                        if (minDiam > 0 && diameter < minDiam)
                        {
                            Console.WriteLine($"[CONSTRAINT] Boven.VerdeelWap: diameter {diameter}→{minDiam}");
                            diameter = minDiam;
                            aangepast = true;
                        }
                        
                        double maxHoh = BovenVerdeelConstraint?.HohMax ?? 9999;
                        if (maxHoh < 9999 && hoh > maxHoh)
                        {
                            Console.WriteLine($"[CONSTRAINT] Boven.VerdeelWap: hoh {hoh}→{maxHoh}");
                            hoh = maxHoh;
                            aangepast = true;
                        }
                        
                        if (aangepast)
                        {
                            strook.PlaatWapening.Boven.VerdeelWapening.Tekst = $"r{diameter:0}-{hoh:0}";
                        }
                    }
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
                dl1g.Description = $"e.g. ({EigenGewicht:0.0})";
                if (AfwerkingVlaklast != 0) dl1g.Description += $" + afw. ({AfwerkingVlaklast:0.0})";
                strook.Beam.Loads.Add(dl1g);

                var veranderlijk = Belastingen.BelastingGevallen[1];
                var dl1q = new DistributedLoad(veranderlijk, "L1~Qk~", 0, LengteM, -veranderlijk.OpgelegdeBelastingen.Vlaklast);
                dl1q.Description = "opgelegde belasting vlaklast";
                strook.Beam.Loads.Add(dl1q);

                var pl = new MovingPointLoad(LengteM/2.12, 10 * -veranderlijk.OpgelegdeBelastingen.Puntlast) { 
                    StartPos = 0.05,
                    EndPos = LengteM - 0.05,
                    Name = "P1",
                    LoadCase = veranderlijk,
                };
                pl.Description = "opgelegde belasting puntlast";
            }

            if (Belastingen.BelastingGevallen.Count > 2)
            {
                var bg3 = Belastingen.BelastingGevallen[2];
                var p1 = new PointLoad(-bg3.OpgelegdeBelastingen.Puntlast, bg3) { Position = LengteM / 2, Name = "P1"};
                p1.Description = "opgelegde belasting puntlast";
                strook.Beam.Loads.Add(p1);
            }

            strook.BerekenStrook();


            // als de basisstrook te weinig wapening heeft verhoog dan de diameter of verlaag de hoh-maat
            var rMin = strook.BendingResults.OrderBy(r => r.Moment).First();
            var asRequired = rMin.AsRequired;
            var asProvided = rMin.AsApplied;


            if (asRequired > asProvided)
            {
                // Beschikbare diameters (klein → groot, max Ø12 voor basiswapening)
                List<double> diameters = new() { 6, 8, 10, 12 };
                
                // Parse huidige wapening
                var parsed = ParseWapeningTekst(this.PlaatWapening?.Onder?.BasisWapening?.Tekst);
                if (!parsed.HasValue)
                {
                    Console.WriteLine("⚠️ Kan wapening tekst niet parsen, skip optimalisatie");
                    return;
                }
                
                double currentDiameter = parsed.Value.diameter;
                double currentHoh = parsed.Value.hoh;
                
                // Haal constraints op
                double minDiameter = OnderHoofdConstraint?.DiameterMin ?? 6.0;
                double maxHoh = OnderHoofdConstraint?.HohMax ?? 150;
                
                bool gevonden = false;
                
                // Stap 1: Probeer diameter te verhogen (binnen constraints)
                foreach (var d in diameters.Where(d => d >= minDiameter && d > currentDiameter))
                {
                    this.PlaatWapening.Onder.BasisWapening.Tekst = $"r{d:0}-{currentHoh:0}";
                    this.PlaatWapening.Onder.BasisWapening.SetZRef();
                    
                    // Update strook profiel en herbereken
                    strook.Profiel.Hoogte = this.Dikte;
                    strook.BerekenStrook();
                    
                    rMin = strook.BendingResults.OrderBy(r => r.Moment).First();
                    if (rMin.AsRequired <= rMin.AsApplied)
                    {
                        Console.WriteLine($"✅ Basiswapening onder: diameter verhoogd naar Ø{d} (hoh={currentHoh}mm blijft gelijk)");
                        gevonden = true;
                        break;
                    }
                }
                
                // Stap 2: Als diameter verhogen niet voldoende is, verlaag hoh-afstand
                if (!gevonden)
                {
                    // Gebruik maximale diameter binnen constraints
                    double maxDiameter = diameters.Where(d => d >= minDiameter).LastOrDefault();
                    if (maxDiameter == 0) maxDiameter = 12; // fallback
                    
                    this.PlaatWapening.Onder.BasisWapening.Tekst = $"r{maxDiameter:0}-{currentHoh:0}";
                    this.PlaatWapening.Onder.BasisWapening.SetZRef();
                    
                    // Verlaag hoh in stappen van 25mm (min 50mm)
                    double minHoh = 50;
                    for (double hoh = currentHoh - 25; hoh >= minHoh; hoh -= 25)
                    {
                        this.PlaatWapening.Onder.BasisWapening.Tekst = $"r{maxDiameter:0}-{hoh:0}";
                        this.PlaatWapening.Onder.BasisWapening.SetZRef();
                        
                        strook.Profiel.Hoogte = this.Dikte;
                        strook.BerekenStrook();
                        
                        rMin = strook.BendingResults.OrderBy(r => r.Moment).First();
                        if (rMin.AsRequired <= rMin.AsApplied)
                        {
                            Console.WriteLine($"✅ Basiswapening onder: hoh verlaagd naar {hoh}mm (Ø{maxDiameter})");
                            gevonden = true;
                            break;
                        }
                    }
                    
                    if (!gevonden)
                    {
                        // Laatste poging: minimale hoh
                        this.PlaatWapening.Onder.BasisWapening.Tekst = $"r{maxDiameter:0}-{minHoh:0}";
                        this.PlaatWapening.Onder.BasisWapening.SetZRef();
                        strook.BerekenStrook();
                        
                        rMin = strook.BendingResults.OrderBy(r => r.Moment).First();
                        Console.WriteLine($"❌ Basiswapening onder: geen oplossing gevonden! (benodigd: {rMin.AsRequired:0}mm², toegepast: {rMin.AsApplied:0}mm² bij Ø{maxDiameter}-{minHoh})");
                    }
                }
            }
            else
            {
                // Wapening is voldoende
                Console.WriteLine($"✅ Basiswapening onder: {this.PlaatWapening?.Onder?.BasisWapening?.Tekst} is voldoende ({asProvided:0}mm² >= {asRequired:0}mm²)");
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
            var r1 = this.Trap1.Reacties;
            var r2 = this.Trap2.Reacties;

            var belastingCombinaties = this.Belastingen.BelastingCombinaties;

            // Bereken maximale fundamentele waarde voor Trap1
            double maxTrap1 = BerekenMaximaleFundamenteleWaarde(r1.G, r1.Q, belastingCombinaties);

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
            
            // ✅ LEES constraints (maar wijzig ze NOOIT!)
            double constraintDiameter = DetailWapeningDiameterMin ?? 8.0;
            double constraintMaxHoh = DetailWapeningHoh ?? 150;
            
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
                        Console.WriteLine($"✅ Detailwapening: hoh aangepast naar {currentHoh}mm (Ø{currentDiameter} gehandhaafd, constraint: Ø{constraintDiameter}-{constraintMaxHoh})");
                        return; // Voldoende met kleinere hoh
                    }
                    
                    if (currentHoh <= minHoh)
                        break;
                }
                
                // Stap 2: Als hoh verkleinen niet helpt, verhoog diameter (maar >= constraint)
                List<double> diameters = new() { 6, 8, 10 };
                var beschikbareDiameters = diameters.Where(d => d >= constraintDiameter).ToList();
                
                foreach (var d in beschikbareDiameters.Skip(1)) // Skip eerste (is al geprobeerd)
                {
                    this.Tand.WapeningAlgemeen.Tekst = $"Ø{d:0.#}-{minHoh:0}";
                    this.Tand.Bijwerken();
                    
                    if (this.Tand.BuigingTand.AsRequired <= this.Tand.BuigingTand.AsApplied)
                    {
                        Console.WriteLine($"⚠️ Detailwapening aangepast: Ø{d}-{minHoh} (constraint was: Ø{constraintDiameter}-{constraintMaxHoh})");
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
        }

        /// <summary>
        /// Berekent de maximale fundamentele waarde (Gk × factorG + Qk × factorQ) 
        /// voor alle fundamentele belastingcombinaties (Type A en B).
        /// </summary>
        public double BerekenMaximaleFundamenteleWaarde(double gk, double qk, List<BelastingCombinatie> combinaties)
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
            var strookbreedte = belastingBreedteMM;
            strook.PlaatWapening.Onder!.BasisWapening.ReferentieLengte = strookbreedte;
            strook.PlaatWapening.Boven!.BasisWapening.ReferentieLengte = strookbreedte;

            strook.Beam.PlaatWapening = strook.PlaatWapening;
            strook.Beam.EI = 1e-9 * strook.Beam.Profiel?.Iy * strook.Beam.Materiaal?.E ?? 1;

            strook.BerekenStrook();

            // ========================================================================
            // BIJLEGWAPENING BEPALEN (met gebruikersopgave als startpunt)
            // ========================================================================
            
            var rMin = strook.BendingResults.OrderBy(r => r.Moment).First();
            var asRequired = rMin.AsRequired;
            var asProvided = rMin.AsApplied;
            
            if (asRequired > asProvided)
            {
                var bijlegReqOnder = asRequired - asProvided;
                
                // ✅ START met automatische berekening (ZONDER constraints)
                int aantalStavenBerekend = (int)((strook.Profiel?.B ?? 200.0) / 100.0);
                
                // ONDERWAPENING
                List<double> diameters = new() { 8, 10, 12, 16, 20, 25 };
                int aantalOnder = aantalStavenBerekend;
                double diameterOnder = 8.0;
                bool gevonden = false;
                
                // Probeer eerst met berekend aantal staven en verschillende diameters
                foreach (var d in diameters)
                {
                    var asTest = BerekenAs(aantalOnder, d);

                    double verhogingTgvAfnameNuttigeHoogte = 1;
                    double d_gemiddeld = strook.PlaatWapening.Onder?.BasisWapening.GemiddeldeDiameter ?? 6;
                    if (d > d_gemiddeld)
                    {
                        verhogingTgvAfnameNuttigeHoogte = rMin.D / (rMin.D - ((d - d_gemiddeld) / 2.0));
                    }

                    if (asTest >= bijlegReqOnder * verhogingTgvAfnameNuttigeHoogte)
                    {
                        diameterOnder = d;
                        gevonden = true;
                        break;
                    }
                }
                
                // Als geen diameter voldoende is, verhoog aantal staven
                if (!gevonden)
                {
                    aantalOnder = (int)Math.Ceiling(bijlegReqOnder / BerekenAs(1, diameters.Last()));
                    diameterOnder = diameters.Last();
                }
                
                // ✅ NU PAS: Check constraints en forceer minimums
                int minAantal = BijlegOnderConstraint?.AantalMin ?? 0;
                double minDiameter = BijlegOnderConstraint?.DiameterMin ?? 0;
                
                if (aantalOnder < minAantal)
                {
                    Console.WriteLine($"[CONSTRAINT] Aantal verhoogd van {aantalOnder} naar {minAantal} (constraint)");
                    aantalOnder = minAantal;
                }
                
                if (diameterOnder < minDiameter)
                {
                    Console.WriteLine($"[CONSTRAINT] Diameter verhoogd van Diameter{diameterOnder} naar Diameter{minDiameter} (constraint)");
                    diameterOnder = minDiameter;
                    
                    // Hercheck of het nog voldoende is
                    if (BerekenAs(aantalOnder, diameterOnder) < bijlegReqOnder)
                    {
                        Console.WriteLine($"[CONSTRAINT] Na diameter constraint is het onvoldoende - verhoog aantal");
                        aantalOnder = (int)Math.Ceiling(bijlegReqOnder / BerekenAs(1, diameterOnder));
                        aantalOnder = Math.Max(aantalOnder, minAantal);
                    }
                }
                
                Console.WriteLine($"[OK] Bijleg onder: {aantalOnder}x Diameter{diameterOnder} (constraint: n>={minAantal}, Diameter>={minDiameter})");
                
                // Update WapeningContext
                BijlegWapeningOnder ??= new WapeningContext();
                BijlegWapeningOnder.Tekst = MaakWapeningTekst(aantalOnder, diameterOnder);
                BijlegWapeningOnder.ReferentieVlak = ReferentieVlakEnum.Onder;
                BijlegWapeningOnder.LaagNummer = PlaatWapening?.Onder?.BasisWapening.LaagNummer ?? 2;
                BijlegWapeningOnder.DekkingToegepast = PlaatWapening?.Onder?.BasisWapening.DekkingToegepast ?? 30;
                BijlegWapeningOnder.ReferentieLengte = strookbreedte;
                
                strook.Beam.PlaatWapening.Onder.BasisWapening.Tekst += $"+{BijlegWapeningOnder.Tekst}";


                // Mogelijk kan de toegepast wapening nog niet voldoende zijn.
                // Omdat de nuttige hoogte gewijzigd is.
                // Dus herhaal deze stappen hierboven (maximaal 1 keer)



                
                // ✅ BOVENWAPENING: 50% van onderwapening
                var bijlegReqBoven = bijlegReqOnder * 0.5;
                
                int aantalBoven = aantalStavenBerekend;
                double diameterBoven = 8.0;
                gevonden = false;
                
                foreach (var d in diameters)
                {
                    var asTest = BerekenAs(aantalBoven, d);
                    if (asTest >= bijlegReqBoven)
                    {
                        diameterBoven = d;
                        gevonden = true;
                        break;
                    }
                }
                
                if (!gevonden)
                {
                    aantalBoven = (int)Math.Ceiling(bijlegReqBoven / BerekenAs(1, diameters.Last()));
                    diameterBoven = diameters.Last();
                }
                
                // Check constraints
                int minAantalBoven = BijlegBovenConstraint?.AantalMin ?? 0;
                double minDiameterBoven = BijlegBovenConstraint?.DiameterMin ?? 0;
                
                if (aantalBoven < minAantalBoven)
                {
                    Console.WriteLine($"[CONSTRAINT] Aantal boven verhoogd van {aantalBoven} naar {minAantalBoven} (constraint)");
                    aantalBoven = minAantalBoven;
                }
                
                if (diameterBoven < minDiameterBoven)
                {
                    Console.WriteLine($"[CONSTRAINT] Diameter boven verhoogd van Diameter{diameterBoven} naar Diameter{minDiameterBoven} (constraint)");
                    diameterBoven = minDiameterBoven;
                    
                    if (BerekenAs(aantalBoven, diameterBoven) < bijlegReqBoven)
                    {
                        aantalBoven = (int)Math.Ceiling(bijlegReqBoven / BerekenAs(1, diameterBoven));
                        aantalBoven = Math.Max(aantalBoven, minAantalBoven);
                    }
                }
                
                Console.WriteLine($"[OK] Bijleg boven: {aantalBoven}x Diameter{diameterBoven} (constraint: n>={minAantalBoven}, Diameter>={minDiameterBoven})");
                
                // Update WapeningContext
                BijlegWapeningBoven ??= new WapeningContext();
                BijlegWapeningBoven.Tekst = MaakWapeningTekst(aantalBoven, diameterBoven);
                BijlegWapeningBoven.ReferentieVlak = ReferentieVlakEnum.Boven;
                BijlegWapeningBoven.LaagNummer = PlaatWapening?.Boven?.BasisWapening.LaagNummer ?? 1;
                BijlegWapeningBoven.DekkingToegepast = PlaatWapening?.Boven?.BasisWapening.DekkingToegepast ?? 30;
                BijlegWapeningBoven.ReferentieLengte = strookbreedte;
                
                strook.Beam.PlaatWapening.Boven.BasisWapening.Tekst += $"+{BijlegWapeningBoven.Tekst}";
            }
            else
            {
                // Geen bijleg nodig
                BijlegWapeningOnder = null;
                BijlegWapeningBoven = null;
                Console.WriteLine("[INFO] Geen bijlegwapening nodig");
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
            if (t == null || t.AansluitendElement == null) 
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



        public override void Bijwerken()
        {
            UpdateStrook1();
            UpdateStrook2();
            UpdateTand();
        }

        /// <summary>
        /// Reset alle wapening constraints en plaatwapening naar standaard waarden
        /// </summary>
        public void ResetConstraintsToDefault()
        {
            // Reset constraints
            BijlegBovenConstraint = new(8, 2, null);
            BijlegOnderConstraint = new(8, 2, null);
            DetailWapeningConstraint = new(6, null, 125);
            OnderHoofdConstraint = new(6, null, 150);
            OnderVerdeelConstraint = new(6, null, 250);
            BovenHoofdConstraint = new(6, null, 150);
            BovenVerdeelConstraint = new(6, null, 250);
            
            // Reset plaatwapening naar defaults
            if (PlaatWapening != null)
            {
                if (PlaatWapening.Onder?.BasisWapening != null)
                {
                    PlaatWapening.Onder.BasisWapening.Tekst = "r6-150";
                    Console.WriteLine("[RESET] Onder.BasisWapening -> r6-150");
                }
                
                if (PlaatWapening.Onder?.VerdeelWapening != null)
                {
                    PlaatWapening.Onder.VerdeelWapening.Tekst = "r6-250";
                    Console.WriteLine("[RESET] Onder.VerdeelWapening -> r6-250");
                }
                
                if (PlaatWapening.Boven?.BasisWapening != null)
                {
                    PlaatWapening.Boven.BasisWapening.Tekst = "r6-150";
                    Console.WriteLine("[RESET] Boven.BasisWapening -> r6-150");
                }
                
                if (PlaatWapening.Boven?.VerdeelWapening != null)
                {
                    PlaatWapening.Boven.VerdeelWapening.Tekst = "r6-250";
                    Console.WriteLine("[RESET] Boven.VerdeelWapening -> r6-250");
                }
            }
            
            // Reset bijlegwapening
            BijlegWapeningOnder = null;
            BijlegWapeningBoven = null;
            
            Console.WriteLine("[RESET] Constraints en plaatwapening teruggezet naar defaults");
        }



        protected void Bereken()
        {
            // TODO: Functiee nalopen


            double tempM = 0.125 * -10 * Math.Pow(Lengte * 0.001, 2);
            UpdateBasisStrook(tempM, 1000);





            var wap = _basisStrook.Wapening;

            // Als basiswapening al voldoende is → geen bijleg nodig
            if (_basisStrook.AsRequired <= wap.AsBasis)
            {
                wap.AantalBijlegStaven = 0;
                wap.DiameterBijlegStaven = 0;
                return;
            }

            // Beschikbare diameters (klein → groot)
            List<double> diameters = new() { 8, 10, 12, 16, 20, 25, 32, 40, 50 };

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

                    // Bereken AsProvided: basis + bijleg (gebruik eventueel je eigen berekening)
                    double AsProvided = wap.AsBasis + BerekenAsBijleg(aantal, d);

                    // Als je model al een property AsBijleg heeft, kun je ook:
                    // double AsProvided = wap.AsBasis + wap.AsBijleg;

                    if (AsProvided >= _basisStrook.AsRequired)
                    {
                        // Gevonden: bewaar deze configuratie en stop
                        gevonden = true;
                        // Als je wap.AsBijleg of andere afhankelijke velden
                        // via berekening wilt updaten, kun je dat hier doen (bv. UpdateWapeningFields())
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
                // Kies gewenste fallback: keep last attempted config, log of zet een foutmelding
                Console.WriteLine("⚠️ Geen voldoende bijleg gevonden met maximaal 4 staven per diameter.");
                // Optioneel: zet aantal=4 van grootste diameter als 'beste poging'
                var laatsteDiameter = diameters.Last();
                wap.DiameterBijlegStaven = laatsteDiameter;
                wap.AantalBijlegStaven = 4;
            }
        }

        // Helper: eenvoudige doorsnede berekening voor bijleg (mm → mm^2)
        private double BerekenAsBijleg(int aantal, double diameterMm)
        {
            // doorsnede per staaf = π * d^2 / 4
            // diameter wordt in mm gegeven; As in mm^2
            double perStaaf = Math.PI * Math.Pow(diameterMm, 2) / 4.0;
            return aantal * perStaaf;
        }

        /// <summary>
        /// Parseert wapening tekst zoals "r6-150" naar (diameter, hoh)
        /// </summary>
        private static (double diameter, double hoh)? ParseWapeningTekst(string? tekst)
        {
            if (string.IsNullOrWhiteSpace(tekst))
                return (double.MinValue, double.MaxValue);

            // Format: "r6-150" of "Ø8-200"
            var match = System.Text.RegularExpressions.Regex.Match(
                tekst, 
                @"[rØø](\d+(?:[.,]\d+)?)-(\d+(?:[.,]\d+)?)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (match.Success)
            {
                string diamStr = match.Groups[1].Value.Replace(',', '.');
                string hohStr = match.Groups[2].Value.Replace(',', '.');
                double diameter = double.Parse(diamStr, System.Globalization.CultureInfo.InvariantCulture);
                double hoh = double.Parse(hohStr, System.Globalization.CultureInfo.InvariantCulture);
                return (diameter, hoh);
            }

            return null;
        }

        /// <summary>
        /// Maakt wapening tekst van aantal en diameter: "3r12"
        /// </summary>
        private static string MaakWapeningTekst(int aantal, double diameter)
        {
            // Formateer diameter zonder decimalen als het een heel getal is
            string diamStr = diameter == Math.Floor(diameter)
                ? diameter.ToString("0")
                : diameter.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
            return $"{aantal}r{diamStr}";
        }

        /// <summary>
        /// Berekent As voor gegeven aantal staven en diameter (mm²)
        /// </summary>
        private static double BerekenAs(int aantal, double diameterMm)
        {
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
            set => SetAndRecalcultate(ref _breedte, value);
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
            get => _dikte;
            set => SetAndRecalcultate(ref _dikte, value);
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

        // ✅ Gebruikersopgave voor bijlegwapening - NU VIA CONSTRAINTS
        private int? _bijlegAantalOnder;
        private double? _bijlegDiameterOnder;
        private int? _bijlegAantalBoven;
        private double? _bijlegDiameterBoven;

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
        public int? BijlegAantalOnder
        {
            get => BijlegOnderConstraint.AantalMin;
            set => BijlegOnderConstraint.AantalMin = value;
        }

        public double? BijlegDiameterOnder
        {
            get => BijlegOnderConstraint.DiameterMin;
            set => BijlegOnderConstraint.DiameterMin = value;
        }

        public int? BijlegAantalBoven
        {
            get => BijlegBovenConstraint.AantalMin;
            set => BijlegBovenConstraint.AantalMin = value;
        }

        public double? BijlegDiameterBoven
        {
            get => BijlegBovenConstraint.DiameterMin;
            set => BijlegBovenConstraint.DiameterMin = value;
        }

        public double? DetailWapeningDiameterMin
        {
            get => DetailWapeningConstraint.DiameterMin;
            set => DetailWapeningConstraint.DiameterMin = value;
        }

        public double? DetailWapeningHoh
        {
            get => DetailWapeningConstraint.HohMax;
            set => DetailWapeningConstraint.HohMax = value;
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
                BasisWapening = _basisStrook.Wapening,
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
                BasisWapening = _basisStrook.Wapening,
                VerdeelWapening = new WapeningContext()
                {
                    Tekst = "r6-250",
                    ReferentieVlak = ReferentieVlakEnum.Boven,
                    ReferentieLengte = 1000,
                },
                LaagHoofdwapening = 2,
                DiameterVerdeel = 8,
            };


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

            _basisStrook.Wapening.Breedte = breedte; 



        }




    }
}
