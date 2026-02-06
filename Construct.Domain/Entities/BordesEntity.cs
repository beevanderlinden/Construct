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
    public class BordesEntity : AssemblageEntity
    {
        public BordesEntity()
        {
            this.AssemblageType = AssemblageTypeEnum.BetonAssemblage;
            this.Naam = "Bordes";
            this.Merk = "BD-?";

            

            Trap1 = new VerbindingAansluitendElement(this);
            Trap2 = new VerbindingAansluitendElement(this);
            Trap2.Gespiegeld = true;


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
            strook.PlaatWapening = this.PlaatWapening!; // basisstrook is gelijk aan deze plaatwapening
            strook.Beam.PlaatWapening = strook.PlaatWapening;



            strook.Beam.Loads.Clear();

            if (Belastingen.BelastingGevallen.Count > 0)
            {
                var perm = Belastingen.BelastingGevallen[0];
                var dl1g = new DistributedLoad(perm, "L1",0, LengteM, -VlaklastG);
                dl1g.Description = $"eigen gewicht + afwerking";
                //dl1g.StartMagnitude = dl1g.EndMagnitude = 0; // tijdelijk nul zetten
                strook.Beam.Loads.Add(dl1g);

                var veranderlijk = Belastingen.BelastingGevallen[1];
                var dl1q = new DistributedLoad(veranderlijk, "L1", 0, LengteM, -veranderlijk.OpgelegdeBelastingen.Vlaklast);
                dl1q.Description = "opgelegde belasting vlaklast";
                //dl1q.StartMagnitude = dl1q.EndMagnitude = 0; // tijdelijk nul zetten

                strook.Beam.Loads.Add(dl1q);

                var pl = new MovingPointLoad(LengteM/3.0, -veranderlijk.OpgelegdeBelastingen.Puntlast) { 
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
            var q1G = Math.Round(-vlaklastG * belastingBreedteMM * 0.001, 2);
            var q1Q = Math.Round(-bg2.OpgelegdeBelastingen.Vlaklast * belastingBreedteMM * 0.001,2);

            int nr = 1;

            DistributedLoad dl1G = new(bg1, $"L{nr++}", 0, L, q1G);
            beam.Loads.Add(dl1G);
            DistributedLoad dl1Q = new(bg2, $"L{nr}", 0, L, q1Q);
            beam.Loads.Add(dl1Q);



            foreach (var l in GetTrapDistrubutedLoads(Trap1, bg1, bg2, $"L{nr++}"))
            {
                beam.Loads.Add(l);
            }
            foreach (var l in GetTrapDistrubutedLoads(Trap2, bg1, bg2, $"L{nr++}"))
            {
                beam.Loads.Add(l);
            }


            strook.PlaatWapening = this.PlaatWapening!.Clone();
            var strookbreedte = belastingBreedteMM;
            strook.PlaatWapening.Onder!.BasisWapening.ReferentieLengte = strookbreedte;
            strook.PlaatWapening.Boven!.BasisWapening.ReferentieLengte = strookbreedte;



            strook.Beam.PlaatWapening = strook.PlaatWapening;



            strook.BerekenStrook();

        }


        public static (double G, double Q) GetTrapBelasting(SteekTrapEntity t)
        {
            return (t.ReactieG, t.ReactieQ);
        }

        public static List<DistributedLoad> GetTrapDistrubutedLoads(VerbindingAansluitendElement t, BelastingGeval bg1, BelastingGeval bg2, string name)
        {
            if (t == null || t.AansluitendElement == null) 
                return [];
            
            if (t.AansluitendElement is SteekTrapEntity steekTrap)
            {
                var g = new DistributedLoad(bg1, name, t.PosM.Start, t.PosM.End, Math.Round(-steekTrap.ReactieG,2));
                var q = new DistributedLoad(bg2, name, t.PosM.Start, t.PosM.End, Math.Round(-steekTrap.ReactieQ,2));

                // omschrijving
                g.Description = $"<i>R<sub>Gk</sub></i> uit {steekTrap.Merk}";
                q.Description = $"<i>R<sub>Qk</sub></i> uit {steekTrap.Merk}";

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

        public BendingResults? BasisStrook
        {
            get => _basisStrook;
            set => SetNestedProperty(ref _basisStrook, value);
        }



        public void InitBasisStrook()
        {
            _basisStrook = new BendingResults()
            {
                Beton = _beton,
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
