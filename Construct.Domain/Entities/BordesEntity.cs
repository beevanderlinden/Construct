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

            // ✅ NIEUW: Herstel trap-referenties
            Trap1.RestoreAssemblageReference(project);
            Trap2.RestoreAssemblageReference(project);
            
            var beton = this.Materiaal as BetonContext;

            // ✅ Initialiseer PlaatDekking.Onder/Boven ENKEL als ze null zijn
            if (PlaatDekking.Onder == null)
            {
                PlaatDekking.Onder = new BetonDekkingContext();
                PlaatDekking.Onder.IsKwaliteitsBeheersing = true;
                PlaatDekking.Onder.IsPlaatGeometrie = true;

            }
            PlaatDekking.Onder.Grondslagen = ProjectInfo.Grondslagen;
            PlaatDekking.Onder.Beton = beton ?? new();
            
            if (PlaatDekking.Boven == null)
            {
                PlaatDekking.Boven = new BetonDekkingContext();
                PlaatDekking.Boven.IsKwaliteitsBeheersing = true;
                PlaatDekking.Boven.IsPlaatGeometrie = true;
            }
            PlaatDekking.Boven.Grondslagen = ProjectInfo.Grondslagen;
            PlaatDekking.Boven.Beton = beton ?? new();

            // ✅ Update _basisStrook.Beton na materiaal restore
            if (_basisStrook != null && beton != null)
            {
                _basisStrook.Beton = beton;
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
        public double? DetailWapeningDiameter => PlaatWapening?.Boven?.VerdeelWapening?.GrootsteDiameter;
        public double? DetailWapeningHoh { get; set; } = 150;

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
            strook.PlaatWapening = this.PlaatWapening?.Clone(); // Clone om referentie delen te voorkomen
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
                //dl1g.StartMagnitude = dl1g.EndMagnitude = 0; // tijdelijk nul zetten
                strook.Beam.Loads.Add(dl1g);

                var veranderlijk = Belastingen.BelastingGevallen[1];
                var dl1q = new DistributedLoad(veranderlijk, "L1~Qk~", 0, LengteM, -veranderlijk.OpgelegdeBelastingen.Vlaklast);
                dl1q.Description = "opgelegde belasting vlaklast";
                //dl1q.StartMagnitude = dl1q.EndMagnitude = 0; // tijdelijk nul zetten

                strook.Beam.Loads.Add(dl1q);

                var pl = new MovingPointLoad(LengteM/2.12, 10 * -veranderlijk.OpgelegdeBelastingen.Puntlast) { 
                    StartPos = 0.05,
                    EndPos = LengteM - 0.05,
                    Name = "P1",
                    LoadCase = veranderlijk,
                    
                };
                pl.Description = "opgelegde belasting puntlast";
                //strook.Beam.Loads.Add(pl);
            }

            if (Belastingen.BelastingGevallen.Count > 2)
            {
                var bg3 = Belastingen.BelastingGevallen[2];
                var p1 = new PointLoad(-bg3.OpgelegdeBelastingen.Puntlast, bg3) { Position = LengteM / 2, Name = "P1"};
                p1.Description = "opgelegde belasting puntlast";
                strook.Beam.Loads.Add(p1);


            }
            
            //strook.Beam.Loads.Add(new DistributedLoad(0, strook.Beam.Length, -10, -10, "DL1"));
            

            //var q1 = strook.Beam.Loads.FirstOrDefault();
            //if (q1 is DistributedLoad qload)
            //{
            //    qload.EndPosition = strook.Beam.Length;
            //}

            
            


            strook.BerekenStrook();

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
            this.Tand.WapeningAlgemeen.Tekst = $"Ø{this.DetailWapeningDiameter:0.#}-{this.DetailWapeningHoh?? 150}";
            this.Tand.DekkingAlgemeen = Math.Max(this.PlaatDekking.Boven.DekkingToe, this.PlaatDekking.Onder.DekkingToe);
            this.Tand.WapeningAlgemeen.DekkingToegepast = this.Tand.DekkingAlgemeen;

            if (this.Tand.BuigingTand != null)
            {
                this.Tand.BuigingTand.Wapening = this.Tand.WapeningAlgemeen;

            }

            // bereken
            this.Tand.Bijwerken();

            // controleer wapening
            if (this.DetailWapeningHoh == null) this.DetailWapeningHoh = 150;
            if (this.Tand.BuigingTand?.AsRequired > this.Tand.WapeningAlgemeen.As)
            {
                // pas de hoh-maat aan.
                var previousHoh = this.DetailWapeningHoh ?? 150;
                var hoh = Math.Floor(previousHoh * this.Tand.BuigingTand.AsApplied / this.Tand.BuigingTand.AsRequired);
                this.DetailWapeningHoh = hoh;
                this.Tand.WapeningAlgemeen.Tekst = $"Ø{this.DetailWapeningDiameter:0.#}-{hoh}";
            }
            else if (this.Tand.BuigingTand?.AsRequired < this.Tand.WapeningAlgemeen.As && this.DetailWapeningHoh < 150)
            {
                // situatie te veel wapening: verhoog de hohmaat
                double currentHoh = this.DetailWapeningHoh ?? 100;
                double maxHoh = 150;
                double targetUtilization = 0.90; // 90% benutting
                
                while (currentHoh < maxHoh)
                {
                    // Verhoog hohmaat met 5mm stappen
                    currentHoh += 5;
                    if (currentHoh > maxHoh) 
                        currentHoh = maxHoh;
                    
                    // Update wapening met nieuwe hohmaat
                    this.DetailWapeningHoh = currentHoh;
                    this.Tand.WapeningAlgemeen.Tekst = $"Ø{this.DetailWapeningDiameter:0.#}-{currentHoh}";
                    
                    // Herbereken
                    this.Tand.Bijwerken();
                    
                    // Check of AsRequired >= 90% van AsApplied
                    if (this.Tand.BuigingTand.AsRequired >= this.Tand.BuigingTand.AsApplied * targetUtilization)
                    {
                        break; // Voldoende benutting bereikt
                    }
                    
                    // Stop als max bereikt
                    if (currentHoh >= maxHoh)
                    {
                        break;
                    }
                }
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

            // aanvullen met bijlegwapening
            this.BijlegWapening = new() { 
                Tekst = "2r8", 
                ReferentieVlak = ReferentieVlakEnum.Onder, 
                LaagNummer = 2 };

            this.BijlegWapeningBoven = new()
            {
                Tekst = "2r8",
                ReferentieVlak = ReferentieVlakEnum.Boven,
                LaagNummer = 2
            };

            strook.Beam.PlaatWapening = strook.PlaatWapening;
            strook.Beam.EI = 1e-9 * strook.Beam.Profiel?.Iy * strook.Beam.Materiaal?.E ?? 1;




            strook.BerekenStrook();

            // bijwerken bijleg,
            var rMin = strook.BendingResults.OrderBy(r => r.Moment).First();
            var req = rMin.AsRequired;
            var prov = rMin.AsApplied;
            if (req > prov)
            {
                var bijlegReq = req - prov;
                int n = (int)((strook.Profiel?.B ?? 200.0) / 100.0);

                // kijk eerst of nØ8 voldoende is, anders nØ10, etc.
                List<double> diams = new() { 8, 10, 12, 16, 20, 25, 32, 40, 50 };
                foreach (var d in diams)
                {
                    var asBijleg = n * Math.PI * Math.Pow(d, 2) / 4.0;
                    if (asBijleg >= bijlegReq)
                    {
                        this.BijlegWapening.Tekst = $"{n}r{d}";
                        break;
                    }
                }

                strook.Beam.PlaatWapening.Onder.BasisWapening.Tekst += $"+{this.BijlegWapening.Tekst}";

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
        public WapeningContext? BijlegWapening
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
                    AantalBijlegStaven = 3,
                    DiameterBijlegStaven = 12,
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
                    Tekst = "r6-150",
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
                    Tekst = "r6-150",
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
