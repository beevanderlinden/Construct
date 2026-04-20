using CommonLibrary;
using Construct.Domain.Entities.Parts;
using Construct.Domain.Helpers;
using Eurocode.Belastingen;
using Eurocode.BetonConstructies;
using Eurocode.Grondslagen;
//using Kaskon.Toolbox.PrefabModels;
using Microsoft.AspNetCore.Components;
using Profielen.Beton;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text.Json.Serialization;
using Construct.Domain.Common;
using Kaskon_it.Algemeen;
using Plotly.Blazor.LayoutLib.AnnotationLib.FontLib;
using static Kaskon_it.Algemeen.Geometrie;

namespace Construct.Domain.Entities
{
    public class SteekTrapEntity : BetonAssemblageEntity
    {
        /// <summary>
        /// Na het deserialiseren van de json moeten we de nested properties instellen. Dit doen we met deze Init methode.
        /// </summary>
        /// <param name="projectInfo">De projectinfo</param>
        public override void Init(ProjectInfoEntity projectInfo)
        {
            ProjectInfo = projectInfo; // projectinfo + grondslagen

            Belastingen = new(grondslagen: ProjectInfo.Grondslagen)
            {
                // herstel de parent-relatie (indien uit json geladen, moet dit opnieuw aangemaakt worden)
                Grondslagen = ProjectInfo.Grondslagen
            }; // Als null, nieuwe aanmaken

            // ⚠️ CRITICAL FIX: Zet GEEN nieuw Materiaal als het al is ingesteld via RestoreReferencesAfterDeserialization!
            // Materiaal ??= new BetonContext();  // ❌ REMOVED - Dit overwrites JSON values!

            // Nur initialize materiaal if BOTH null: Materiaal AND MateriaalId
            if (Materiaal == null) // && !MateriaalId.HasValue) // <-- Safety check: als null is, altijd herstellen
            {
                Materiaal = new BetonContext();  // Fallback only if truly not set
            }

            // ✅ Gebruik centrale methode om PlaatDekking te initialiseren (synct ook MainPart)
            InitializePlaatDekking();

            // ✅ NIEUW: Koppel MainPart expliciet aan assemblage eigenschappen
            if (MainSlab != null)
            {
                MainSlab.Material = this.Materiaal; // Shared!
                MainSlab.PlaatDekking = this.PlaatDekking; // Shared!
                MainSlab.Dikte = this.SchilDikte; // Sync dikte
                
                // ✅ NIEUW: Initialiseer PlaatWapening voor trap (hoofdwapening in laag 1)
                if (MainSlab.PlaatWapening == null)
                {
                    MainSlab.PlaatWapening = new PlaatWapening()
                    {
                        Onder = new PlaatWapeningGroep()
                        {
                            Heading = "schilwapening onder",
                            DekkingBuitensteLaag = this.PlaatDekking.Onder,
                            BasisWapening = new WapeningContext()
                            {
                                Tekst = this.WapeningSchil?.Tekst ?? "r8-150",
                                TekstOndergrens = "r8-150",
                                ReferentieVlak = ReferentieVlakEnum.Onder,
                                ReferentieLengte = 1000,
                            },
                            VerdeelWapening = new WapeningContext()
                            {
                                Tekst = "r8-150",
                                TekstOndergrens = "r8-150",
                                ReferentieVlak = ReferentieVlakEnum.Onder,
                                ReferentieLengte = 1000,
                            },
                            LaagHoofdwapening = 1, // ✅ TRAP: Laag 1 (niet 2 zoals bordes)
                            DiameterVerdeel = 8,
                        },
                        Boven = new PlaatWapeningGroep()
                        {
                            Heading = "schilwapening boven",
                            DekkingBuitensteLaag = this.PlaatDekking.Boven,
                            BasisWapening = new WapeningContext()
                            {
                                Tekst = "r8-150",
                                TekstOndergrens = "r8-150",
                                ReferentieVlak = ReferentieVlakEnum.Boven,
                                ReferentieLengte = 1000,
                            },
                            VerdeelWapening = new WapeningContext()
                            {
                                Tekst = "r8-150",
                                TekstOndergrens = "r8-150",
                                ReferentieVlak = ReferentieVlakEnum.Boven,
                                ReferentieLengte = 1000,
                            },
                            LaagHoofdwapening = 1, // ✅ TRAP: Laag 1 (niet 2 zoals bordes)
                            DiameterVerdeel = 8,
                        },
                    };
                }
            }

            var beton = Materiaal as BetonContext;

            // ✅ Sync beton naar PlaatDekking (wordt aangemaakt in constructor vóór Materiaal beschikbaar is)
            if (beton != null)
            {
                PlaatDekking.Onder.Beton = beton;
                PlaatDekking.Boven.Beton = beton;
            }

            //DemoUitkraging = new() { Beton = beton ?? new() };

            ProfielSchil = new()
            {
                Breedte = 1000,
                Hoogte = SchilDikte
            };



            // reset
            _snedekrachten = null;
            _snedekrachtenBGT = null;

            //DemoBuiging ??= new() { Beton = Beton, Profiel = ProfielSchil };
            BuigingBGT = new()
            {
                Beton = beton ?? new(),
                Profiel = ProfielSchil,
                Wapening = WapeningSchil!,
                Snedekrachten = SnedekrachtenBGT
            };


            //WapeningSchil = new(this.WapeningSchil?.Tekst ?? "8-150", 30);



            // Zorg dat het moment wordt bijgewerkt, zodat alle referenties worden aangelegd
            MomentSchil = new()
            {
                Heading = "Momentwapening schil",
                Name = MomentSchil?.Name ?? "schil",
                Beton = beton ?? new(),
                Profiel = ProfielSchil,
                Wapening = WapeningSchil!,
                Snedekrachten = Snedekrachten,
                ConstructiefModel = Schematisering.ConstructiefModelEnum.Plaat,
            };

            Dwarskracht = new()
            {
                Beton = beton ?? new(),
                Profiel = ProfielSchil,
                Snedekrachten = Snedekrachten,
                NutHoogte = this.MomentSchil.D,
                AsLangs = this.MomentSchil.AsApplied

            };

            Slankheid = new()
            {
                Heading = "Slankheid schil",
                BendingResults = MomentSchil,
                LengteOverspanning = this.LtProjZ,
            };


            //DoorbuigingContext = new(Beton, ProfielSchil, WapeningSchil)
            //{

            //    ReadOnly = true,
            //    LengteMM = this.LengteTotaal * this.SchuineMaat / this.AantredeMaat,
            //    Lijnlast = this.Krachten.qEqp * this.FactorProjectieZToLocalZ,
            //    D = this.ProfielSchil.Hoogte - this.WapeningSchil.ZRef,
            //    LijnlastG = this.Krachten.qG * this.FactorProjectieZToLocalZ,

            //};


            DoorbuigingCombinatieContexts = [
                new DoorbuigingCombinatieContext(){CombinatieType = BelastingCombinatieTypeEnum.Blijvend, Lijnlast = Krachten?.LijnlastG ?? 0},
                new DoorbuigingCombinatieContext(){CombinatieType = BelastingCombinatieTypeEnum.QuasiBlijvend, Lijnlast = Krachten?.LijnlastQuasiPermanent ?? 0},
                new DoorbuigingCombinatieContext(){CombinatieType = BelastingCombinatieTypeEnum.Frequent, Lijnlast = Krachten?.LijnlastFrequent ?? 0},
            ];
            DoorbuigingValidatie = new(beton ?? new(), MainSlab?.Profiel ?? ProfielSchil, WapeningSchil!, LengteSchuin, DoorbuigingCombinatieContexts);
            DoorbuigingValidatie.Init();

            //Doorbuiging = new(DoorbuigingContext)
            //{
            //    Heading = "Doorbuiging",
            //    IsToetsingEind = true,
            //};


            Scheurwijdte = new(SnedekrachtenBGT, beton ?? new(), PlaatDekking.Onder, MainSlab?.Profiel ?? ProfielSchil, WapeningSchil!, ProjectInfo.Grondslagen.NationaleBijlage ?? Eurocode.Grondslagen.NationaleBijlageEnum.EU)
            {
                Heading = "Scheurwijdte schil"
            };





            if (HeeftBoventand)
            {
                var bestaandeTand = TandOpleggingBovenzijde;
                TandOpleggingBovenzijde = new(this, bestaandeTand?.Oplegging ?? new())
                {
                    TandLengte = bestaandeTand?.TandLengte ?? 100,
                    TandHoogte = bestaandeTand?.TandHoogte ?? 100,
                    HalsDikteOpgave = bestaandeTand?.HalsDikteOpgave,
                    BovensteAantredeLengteOpgave = bestaandeTand?.BovensteAantredeLengteOpgave,
                    WapeningAlgemeen = bestaandeTand?.WapeningAlgemeen ?? new WapeningContext() { Tekst = "6-75" },
                    VoegBreedte = bestaandeTand?.VoegBreedte ?? 10,
                };
                TandOpleggingBovenzijde.HalsDikte = TandOpleggingBovenzijde.BerekenHalsDikte((LengteBoven?? AantredeMaat) - WelMaat);
            }
            else
            {
                TandOpleggingBovenzijde = null;
            }

            PlaatDekking.Boven.PropertyChanged += OnDekkingContextChanged;


            // doorbuiging (context bijwerken, voor alle non-ref properties)
            DoorbuigingBijwerken();

            


            // check akkoord
            // dit berekent alle geneste eurocode onderdelen
            // en slaat akkoord status op
            IsAkkoord();


        }




        public List<(string naam, MarkupString markup)> ToBelastingMarkupStrings()
        {
            return Krachten.GetBelastingMarkupStrings();
        }


        public override string ToString()
        {
            return $"{Merk}";
            //return $"{OptredeAantal1} treden {AantredeMaat:0.#}×{OptredeMaat:0.#}, schildikte = {SchilDikte:0 mm}";
        }

        [JsonIgnore]
        public ObservableCollection<Melding> Meldingen { get; set; } = [];


        public bool IsAkkoord()
        {
            Meldingen.Clear();
            ClearToetsen();
            

            if (HoogteTotaal > 4040)
            {
                Meldingen.Add(new(MeldingType.Waarschuwing, "overschrijding maximale hoogte (trap is meer dan 4 meter hoog)"));
            }
            if (AantredeMaat + WelMaat < 210)
            {
                Meldingen.Add(new(MeldingType.Opmerking, "aantrede te klein voor bouwbesluit"));
            }
            if (OptredeMaat > 190)
            {
                Meldingen.Add(new(MeldingType.Opmerking, "optrede te groot voor bouwbesluit"));
            }

            //this.GetKrachten(); // altijd bijwerken, mogelijk veranderd
            this.SetKrachten(); // update krachten in context zodat deze worden meegenomen in de berekeningen


            this.Dwarskracht.NutHoogte = this.MomentSchil.D; // geen ref op NutHoogte -> bijwerken
            this.Dwarskracht.AsLangs = this.MomentSchil.AsApplied; // geen ref op AsLangs -> bijwerken


            AddToetsen(GetToetsen());


            var returnVal = true;
            foreach (var context in Toetsen)
            {
                if (context == null) continue;
                if (!context.BerekenEnValideer())
                {
                    foreach (var melding in context.Meldingen)
                    {
                        Meldingen.Add(melding);
                    }
                    returnVal = false;
                }
            }

            // de tand
            if(TandOpleggingBovenzijde != null)
            {
                TandOpleggingBovenzijde.HalsDikte = TandOpleggingBovenzijde.BerekenHalsDikte((LengteBoven ?? AantredeMaat) - WelMaat);
                TandOpleggingBovenzijde.BerekenEnValideer();

                // ✅ Optimaliseer tandwapening
                OptimaliseerTandWapening();
            }

            

            BerekenDoorsnedePolygoon();

            Akkoord = returnVal;

            return returnVal;
        }

        public bool Akkoord { get; private set; }

        private void OnDekkingContextChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BetonDekkingContext.DekkingToe))
            {
                foreach (var context in Toetsen)
                {
                    context.BerekenEnValideer();
                }
            }
        }

        public SteekTrapEntity()
        {

            ProjectInfo = new();
            //Beton = new();
            // ⚠️ REMOVED: Materiaal = new BetonContext();
            // Materiaal zal worden ingesteld via JSON-deserialisatie of RestoreReferencesAfterDeserialization
            
            var beton = Materiaal as BetonContext ?? new();  // Fallback naar new indien null
            WapeningSchil = new();
            PlaatDekking.Onder = new(grondslagen: ProjectInfo.Grondslagen, beton: beton)
            {
                IsPlaatGeometrie = true,
                IsKwaliteitsBeheersing = true,
                SelectedMilieuklassen = [MilieuklasseEnum.XC1],
                WapeningDiameterGelijkwaardig = 12
            };
            PlaatDekking.Boven = new(grondslagen: ProjectInfo.Grondslagen, beton: beton)
            {
                IsPlaatGeometrie = true,
                IsKwaliteitsBeheersing = true,
                SelectedMilieuklassen = [Eurocode.BetonConstructies.MilieuklasseEnum.XC1],
            };


            Belastingen = new(new GrondslagenContext());
            ProfielSchil = new() { Breedte = 1000, Hoogte = 120 };
            MomentSchil = new();
        }

        //[JsonConstructor]
        public SteekTrapEntity(ProjectInfoEntity projectInfo)
        {
            ProjectInfo = projectInfo;
            Belastingen = new(grondslagen: ProjectInfo.Grondslagen);
            
            // ⚠️ VERWIJDERD: Materiaal wordt nu gezet via object initializer in GetSteekTrap()
            // Dit voorkomt dat een nieuw BetonContext wordt aangemaakt dat niet in project.Materialen staat
            // Materiaal = new BetonContext("C45/55");  // ❌ REMOVED
            
            var beton = Materiaal as BetonContext;

            // ✅ NIEUW: Creëer MainPart vroeg in constructor
            MainPart = CreateMainPart();

            PlaatDekking.Onder = new(grondslagen: ProjectInfo.Grondslagen, beton: beton ?? new())
            {
                IsPlaatGeometrie = true,
                IsKwaliteitsBeheersing = true,
                SelectedMilieuklassen = [MilieuklasseEnum.XC1],
            };
            PlaatDekking.Boven = new(grondslagen: ProjectInfo.Grondslagen, beton: beton ?? new())
            {
                IsPlaatGeometrie = true,
                IsKwaliteitsBeheersing = true,
                SelectedMilieuklassen = [Eurocode.BetonConstructies.MilieuklasseEnum.XC1],
            };

            //DemoUitkraging = new() { Beton = beton ?? new() };
            //DemoBuiging = new() { Beton = Beton };

            // Profiel voor de schil, bepaalt de schildikte
            ProfielSchil = new() { Breedte = 1000, Hoogte = 120 };
            WapeningSchil = new("r8-150", PlaatDekking.Onder.DekkingToe);


            
            // Toets voor schil 
            MomentSchil = new() { Beton = beton ?? new(), Profiel = ProfielSchil, Wapening = WapeningSchil, Snedekrachten = Snedekrachten };
            Scheurwijdte = new() { Beton = beton ?? new(), Hoogte = ProfielSchil.Hoogte, Wapening = WapeningSchil, Dekking = PlaatDekking.Onder, Snedekrachten = Snedekrachten };

            TandOpleggingBovenzijde = new(this, this?.TandOpleggingBovenzijde?.Oplegging ?? new())
            {
                // alles gebeurt in de initialize
            };

            
            PlaatDekking.Boven.PropertyChanged += OnDekkingContextChanged;

            _snedekrachten = new();

            // ✅ Voor NIEUWE steektrappen (via UI): roep Init() aan
            // Voor GELADEN steektrappen (uit JSON): Init() wordt aangeroepen in RestoreReferencesAfterDeserialization()
            Init(projectInfo);
            
        }

        /// <summary>
        /// Herstelt object-referenties na JSON-deserialisatie.
        /// BELANGRIJK: Roept Init() aan NADAT Materiaal is hersteld!
        /// </summary>
        public override void RestoreReferencesAfterDeserialization(ProjectEntity project)
        {
            // ✅ EERST: Herstel alle basisreferenties (materiaal, belastingen, MainPart, etc.)
            // De base class zorgt nu voor intelligente materiaal fallback!
            base.RestoreReferencesAfterDeserialization(project);

            // ✅ NIEUW: Migreer oude data naar MainPart indien MainPart null is
            if (MainPart == null)
            {
                Console.WriteLine($"✅ [SteekTrapEntity] Migreer oude data naar MainPart");
                MainPart = CreateMainPart();
            }
            
            // ✅ DAARNA: Initialiseer Init() nu MET het echte Materiaal
            //    (niet met een temp BetonContext zoals zou gebeuren als Init() in constructor wordt aangeroepen)
            Init(ProjectInfo);
        }




        //public SteekTrapEntity()
        //{
        //    ProjectInfo = new();
        //    Belastingen = new(grondslagen: ProjectInfo.Grondslagen);
        //    Beton = new();
        //    DekkingBoven = new(grondslagen: ProjectInfo.Grondslagen, beton: Beton);
        //    WapeningSchil = new("10-150");
        //    MomentSchil = new(Beton, ProfielSchil, WapeningSchil, snedekrachten: Snedekrachten);
        //    TandOpleggingBovenzijde = new(this);
        //}

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

            // WapeningSchil sync (zelfde als Onder.BasisWapening)
            if (WapeningSchil != null)
            {
                var definitief = WapeningOptimizer.PasOndergrensToe(WapeningSchil.Tekst, WapeningSchil.TekstOndergrens);
                if (definitief != WapeningSchil.Tekst)
                {
                    WapeningSchil.Tekst = definitief;
                    WapeningSchil.SetZRef();
                }
            }
        }

        public override void Bijwerken()
        {
            // We moeten altijd de krachten bijwerken, omdat deze de basis vormen voor alle volgende berekeningen
            var schilKrachten = this.GetKrachten();


            // ✅ NIEUW: Optimaliseer schilwapening op basis van benodigde As
            OptimaliseerSchilWapening();
            
            // ✅ Pas ondergrenzen toe op alle wapeningen
            PasOndergrenzenToe();

            // We weten nu de voor sterkte benodigde en toegepaste wapening
            //var asProvided_UGT = PlaatWapening?.Onder?.BasisWapening.As;
            

            Scheurwijdte.BerekenEnValideer();

            // ✅ Iteratieve verhoging voor scheurwijdte (max 200%)
            if (!Scheurwijdte.IsValidated)
            {
                var instelling = ProjectInfo?.WapeningAfhandeling ?? WapeningAfhandelingEnum.AlleenVerhogen;

                if (instelling == WapeningAfhandelingEnum.Gebruiker)
                {
                    Console.WriteLine("⚠️ [SteekTrapEntity] Scheurwijdte niet voldaan, maar instelling=Gebruiker: geen automatische aanpassing");
                }
                else
                {
                Console.WriteLine("⚠️ [SteekTrapEntity] Scheurwijdte niet voldaan, probeer wapening te verhogen...");

                double verhoging = 1.0;
                const double verhogingStap = 0.25;
                const double maxVerhoging = 2.0;

                // Haal huidige wapening tekst op
                string huidigeWapeningTekst = 
                    PlaatWapening?.Onder?.BasisWapening?.Tekst ??
                    WapeningSchil?.Tekst ?? "r8-150";

                while (!Scheurwijdte.IsValidated && verhoging < maxVerhoging)
                {
                    verhoging += verhogingStap;

                    // ✅ Verschaal huidige wapening met factor
                    var resultaat = WapeningOptimizer.VerschaalPlaatWapening(
                        huidigeWapeningTekst,
                        factor: verhoging,
                        beschikbareBreedte: 1000,
                        ondergrens: PlaatWapening?.Onder?.BasisWapening?.TekstOndergrens ?? "6-200" // Gebruik ondergrens van huidige wapening als constraint
                    );

                    // Update wapening
                    if (PlaatWapening?.Onder?.BasisWapening != null)
                    {
                        PlaatWapening.Onder.BasisWapening.Tekst = resultaat.tekst;
                        PlaatDekking.Onder.WapeningDiameterGelijkwaardig = PlaatWapening.Onder.BasisWapening.GemiddeldeDiameter;
                        Scheurwijdte.Wapening = PlaatWapening.Onder.BasisWapening;
                    }

                    // Herbereken scheurwijdte met nieuwe wapening
                    Scheurwijdte.BerekenEnValideer();

                    Console.WriteLine($"   Verhoging {verhoging:P0}: {resultaat.tekst} (As={resultaat.asProvided:0}mm²) → SW valid={Scheurwijdte.IsValidated}");
                }

                if (verhoging >= maxVerhoging && !Scheurwijdte.IsValidated)
                {
                    Console.WriteLine($"⚠️ [SteekTrapEntity] Scheurwijdte niet voldaan na {maxVerhoging:P0} verhoging");
                }
                }
            }

            //var huidigeAsProvided = PlaatWapening.Onder.BasisWapening.As;
            SlankheidBijwerken();
            DoorbuigingBijwerken();

            // ✅ Iteratieve verhoging voor doorbuiging (max 200%)
            if (DoorbuigingValidatie != null && !DoorbuigingValidatie.IsValidated)
            {
                var instelling = ProjectInfo?.WapeningAfhandeling ?? WapeningAfhandelingEnum.AlleenVerhogen;

                if (instelling == WapeningAfhandelingEnum.Gebruiker)
                {
                    Console.WriteLine("⚠️ [SteekTrapEntity] Doorbuiging niet voldaan, maar instelling=Gebruiker: geen automatische aanpassing");
                }
                else
                {
                Console.WriteLine("⚠️ [SteekTrapEntity] Doorbuiging niet voldaan, probeer wapening te verhogen...");
                Console.WriteLine($"Doorbuiging voor verhoging: Wbijk = {DoorbuigingValidatie.Wbijk:0} mm");
                Console.WriteLine($"Doorbuiging voor verhoging: Wtot = {DoorbuigingValidatie.Wtot:0} mm");
                Console.WriteLine($"Doorbuiging voor verhoging: As = {DoorbuigingValidatie.Wapening.As:0} mm²");

                double verhoging = 1.0;
                const double verhogingStap = 0.25;
                const double maxVerhoging = 2.0;

                // Haal huidige wapening tekst op
                string huidigeWapeningTekst =
                    PlaatWapening?.Onder?.BasisWapening?.Tekst ??
                    WapeningSchil?.Tekst ?? "r8-150";

                while (!DoorbuigingValidatie.IsValidated && verhoging < maxVerhoging)
                {
                    verhoging += verhogingStap;

                    // ✅ Verschaal huidige wapening met factor
                    var resultaat = WapeningOptimizer.VerschaalPlaatWapening(
                        huidigeWapeningTekst,
                        factor: verhoging,
                        beschikbareBreedte: 1000

                    );

                    // Update wapening
                    if (WapeningSchil != null)
                        WapeningSchil.Tekst = resultaat.tekst;

                    if (PlaatWapening?.Onder?.BasisWapening != null)
                    {
                        PlaatWapening.Onder.BasisWapening.Tekst = resultaat.tekst;
                        PlaatDekking.Onder.WapeningDiameterGelijkwaardig = PlaatWapening.Onder.BasisWapening.GemiddeldeDiameter;

                    }

                    // Herbereken doorbuiging met nieuwe wapening
                    DoorbuigingBijwerken();

                    Console.WriteLine($"   Verhoging {verhoging:P0}: {resultaat.tekst} (As={resultaat.asProvided:0}mm²)  Doorbuiging valid={DoorbuigingValidatie.IsValidated}");
                    Console.WriteLine($"   H = {DoorbuigingValidatie.Profiel.Hoogte:0} mm");
                    Console.WriteLine($"   Wbijk = {DoorbuigingValidatie.Wbijk:0.#} mm");
                }

                if (verhoging >= maxVerhoging && !DoorbuigingValidatie.IsValidated)
                {
                    Console.WriteLine($"⚠️ [SteekTrapEntity] Doorbuiging niet voldaan na {maxVerhoging:P0} verhoging");
                }
                }
            }



            IsAkkoord();

            // ✅ Brandwerendheid bijwerken
            MainSlab?.UpdateRei();

            // ✅ Roep base aan zodat validatie wordt gemaakt
            base.Bijwerken();
        }
        
        /// <summary>
        /// ✅ NIEUW: Optimaliseer schilwapening op basis van MomentSchil.AsRequired.
        /// Gebruikt WapeningOptimizer voor DRY logica.
        /// </summary>
        private void OptimaliseerSchilWapening()
        {
            // Bereken momentwapening schil eerst
            if (MomentSchil == null || Snedekrachten == null)
            {
                Console.WriteLine("⚠️ [SteekTrapEntity] MomentSchil of Snedekrachten is null, skip wapening optimalisatie");
                return;
            }


            

            // Bereken met huidige wapening
            MomentSchil.BerekenEnValideer();

            var asRequired = MomentSchil.AsRequired;

            if (asRequired <= 0)
            {
                Console.WriteLine("✅ [SteekTrapEntity] Geen wapening benodigd (AsRequired = 0)");
                return;
            }

            var instelling = ProjectInfo?.WapeningAfhandeling ?? WapeningAfhandelingEnum.AlleenVerhogen;
            var huidigeTekst = PlaatWapening?.Onder?.BasisWapening?.Tekst;

            var resultaat = WapeningOptimizer.BepaalWapeningMetInstelling(
                huidigeTekst,
                asRequired,
                PlaatWapening!.Onder!.BasisWapening,
                instelling
            );

            // Update wapening
            if (PlaatWapening?.Onder?.BasisWapening != null)
            {
                PlaatWapening.Onder.BasisWapening.Tekst = resultaat.tekst;
                Console.WriteLine($"✅ [SteekTrapEntity] Schilwapening ({instelling}): {resultaat.tekst} (As={resultaat.asProvided:0}mm², benodigd={asRequired:0}mm²)");
                PlaatDekking.Onder.WapeningDiameterGelijkwaardig = PlaatWapening.Onder.BasisWapening.GemiddeldeDiameter;

                // Herbereken met nieuwe wapening
                MomentSchil.Wapening = PlaatWapening.Onder.BasisWapening ?? WapeningSchil;
                MomentSchil.BerekenEnValideer();
            }
        }
        
        /// <summary>
        /// Optimaliseer tandwapening op basis van benodigde As.
        /// Respecteert de project-brede WapeningAfhandelingEnum instelling:
        ///   - Lege invoer: altijd optimale wapening bepalen (ongeacht instelling).
        ///   - Gebruiker: niet aanpassen.
        ///   - AlleenVerhogen: huidige wapening controleren; alleen verhogen als onvoldoende.
        ///   - Optimaliseer: altijd herberekenen vanaf minimum (kan ook verlagen).
        /// </summary>
        private void OptimaliseerTandWapening()
        {
            if (TandOpleggingBovenzijde == null)
            {
                Console.WriteLine("⚠️ [SteekTrapEntity] Geen tand beschikbaar, skip optimalisatie");
                return;
            }

            var tand = TandOpleggingBovenzijde;

            // controleer
            tand.Oplegging.OplegLengteNettoAanwezig = tand.TandLengte - 10; 


            var instelling = ProjectInfo?.WapeningAfhandeling ?? WapeningAfhandelingEnum.AlleenVerhogen;
            bool invoerLeeg = string.IsNullOrWhiteSpace(tand.WapeningAlgemeen?.Tekst);

            const double constraintDiameter = 6.0;
            const double constraintMaxHoh = 150.0;
            double currentDiameter = constraintDiameter;
            double currentHoh = constraintMaxHoh;

            // Altijd dekking bijwerken
            tand.DekkingAlgemeen = Math.Max(PlaatDekking.Boven.DekkingToe, PlaatDekking.Onder.DekkingToe);
            tand.WapeningAlgemeen!.DekkingToegepast = tand.DekkingAlgemeen;
            if (tand.BuigingTand != null)
                tand.BuigingTand.Wapening = tand.WapeningAlgemeen;

            // Gebruiker-instelling met bestaande invoer: niet aanpassen
            if (!invoerLeeg && instelling == WapeningAfhandelingEnum.Gebruiker)
            {
                Console.WriteLine("⚠️ [SteekTrapEntity] Tandwapening: instelling=Gebruiker, geen automatische aanpassing");
                tand.Bijwerken();
                tand.BerekenEnValideer();
                return;
            }

            // Startpunt bepalen:
            //   - Lege invoer of Optimaliseer: begin bij minimum (Ø6-150)
            //   - AlleenVerhogen met bestaande waarde: gebruik huidige tekst als startpunt
            bool startVanafMinimum = invoerLeeg || instelling == WapeningAfhandelingEnum.Optimaliseer;

            if (startVanafMinimum)
                tand.WapeningAlgemeen.Tekst = $"Ø{currentDiameter:0.#}-{currentHoh:0}";

            tand.Bijwerken();

            if (tand.TotaleWapeningBenodigd > tand.WapeningAlgemeen.As)
            {
                Console.WriteLine($"⚠️ [SteekTrapEntity] Tandwapening onvoldoende: AsReq={tand.TotaleWapeningBenodigd:0}mm² > AsProv={tand.WapeningAlgemeen.As:0}mm²");

                // Bij AlleenVerhogen was de bestaande waarde onvoldoende: start optimalisatie ook vanaf minimum
                if (!startVanafMinimum)
                {
                    tand.WapeningAlgemeen.Tekst = $"Ø{currentDiameter:0.#}-{currentHoh:0}";
                    tand.Bijwerken();
                }

                // STAP 1: Verklein hoh (meer wapening per meter) tot minimum van 50mm
                double minHoh = 50;
                const double targetUtilization = 0.95;

                while (currentHoh >= minHoh)
                {
                    currentHoh -= 5;
                    if (currentHoh < minHoh)
                        currentHoh = minHoh;

                    tand.WapeningAlgemeen.Tekst = $"Ø{currentDiameter:0.#}-{currentHoh:0}";
                    tand.Bijwerken();

                    if (tand.TotaleWapeningBenodigd <= tand.BuigingTand!.AsApplied * targetUtilization)
                    {
                        Console.WriteLine($"✅ [SteekTrapEntity] Tandwapening: hoh aangepast naar {currentHoh}mm (Ø{currentDiameter})");
                        tand.BerekenEnValideer();
                        return;
                    }

                    if (currentHoh <= minHoh)
                        break;
                }

                // STAP 2: Hoh verkleinen volstaat niet → verhoog diameter
                List<double> diameters = [6, 8, 10, 12];
                var beschikbareDiameters = diameters.Where(d => d >= constraintDiameter).ToList();

                foreach (var d in beschikbareDiameters.Skip(1))
                {
                    tand.WapeningAlgemeen.Tekst = $"Ø{d:0.#}-{minHoh:0}";
                    tand.Bijwerken();

                    if (tand.BuigingTand!.AsRequired <= tand.BuigingTand.AsApplied)
                    {
                        Console.WriteLine($"✅ [SteekTrapEntity] Tandwapening aangepast: Ø{d}-{minHoh}");
                        tand.BerekenEnValideer();
                        return;
                    }
                }

                Console.WriteLine($"❌ [SteekTrapEntity] Tandwapening: geen oplossing gevonden!");
            }
            else if (startVanafMinimum && tand.BuigingTand?.AsRequired < tand.WapeningAlgemeen.As)
            {
                // Minimum wapening is al te zwaar: verhoog hoh om wapening te verlagen
                // (alleen bij lege invoer of Optimaliseer, niet bij AlleenVerhogen)
                const double targetUtilization = 0.90;

                while (currentHoh <= constraintMaxHoh)
                {
                    currentHoh += 5;
                    if (currentHoh > constraintMaxHoh)
                        currentHoh = constraintMaxHoh;

                    tand.WapeningAlgemeen.Tekst = $"Ø{currentDiameter:0.#}-{currentHoh:0}";
                    tand.Bijwerken();

                    if (tand.BuigingTand.AsRequired >= tand.BuigingTand.AsApplied * targetUtilization)
                    {
                        Console.WriteLine($"✅ [SteekTrapEntity] Tandwapening geoptimaliseerd: Ø{currentDiameter}-{currentHoh}mm (benutting ~90%)");
                        break;
                    }

                    if (currentHoh >= constraintMaxHoh)
                    {
                        Console.WriteLine($"✅ [SteekTrapEntity] Tandwapening: Ø{currentDiameter}-{currentHoh}mm (op maximum)");
                        break;
                    }
                }
            }
            else
            {
                Console.WriteLine($"✅ [SteekTrapEntity] Tandwapening: {tand.WapeningAlgemeen.Tekst} is voldoende");
            }

            tand.BerekenEnValideer();
        }

        protected override void ValidateAssemblage()
        {
            if (Validation == null) return;

            // Valideer meldingen van de trap zelf
            foreach (var melding in Meldingen.Where(m => m != null))
            {
                switch (melding.Type)
                {
                    case MeldingType.Error:
                        Validation.AddError($"{melding.Bericht}");
                        break;
                    case MeldingType.Waarschuwing:
                        Validation.AddWarning($"{melding.Bericht}");
                        break;
                    case MeldingType.Waarschuwing | MeldingType.Error:
                        Validation.AddError(melding.Bericht);
                        Validation.AddWarning(melding.Bericht);
                        break;

                }
            }


            // Dekking
            if (PlaatDekking.HeeftWaarschuwing())
            {
                var waarschuwingen = PlaatDekking.Meldingen.Where(x => x.Type == MeldingType.Waarschuwing).ToList();
                

                Validation.AddWarning(
                    "Dekking niet akkoord",
                    detail: string.Join("\r\n", waarschuwingen)
                    );              
            }


            // Check dwarskracht
            if (Dwarskracht != null && Dwarskracht.Ved > Dwarskracht.DwarskrachtWeerstandBeton)
            {
                Validation.AddWarning(
                    "Dwarskrachtwapening benodigd",
                    $"VEd = {Dwarskracht.Ved:0.#} kN > VRd,c = {Dwarskracht.DwarskrachtWeerstandBeton:0.#} kN"
                );
            }

            // Check scheurwijdte
            if (Scheurwijdte != null && !Scheurwijdte.IsValidated)
            {
                Validation.AddWarning("Scheurwijdte mogelijk niet voldaan");
            }

            // Check tand
            if (TandOpleggingBovenzijde != null)
            {
                var tand = TandOpleggingBovenzijde;
                if (tand.DwarskrachtTand != null &&
                    tand.DwarskrachtTand.Ved > tand.DwarskrachtTand.DwarskrachtWeerstandBeton)
                {
                    Validation.AddError("Tand: dwarskrachtwapening nodig (niet toegestaan)");
                }

                var subToetsen = tand.GetToetsen();

                foreach (var subToets in subToetsen)
                {
                    if (subToets == null)
                    {

                        continue;
                    }
                    foreach (var melding in subToets.Meldingen)
                    {
                        switch (melding.Type)
                        {
                            case MeldingType.Waarschuwing: Validation.AddWarning("[Tand] " + $"[{subToets.Heading}] " + melding.Bericht); 
                                break;
                            case MeldingType.Error: Validation.AddError("[Tand] " + $"[{subToets.Heading}] " + melding.Bericht);
                                break;
                        }
                    }
                        
                    
                }

            }

            

        }

        public void SetProfiel(BetonProfiel profiel)
        {
            this.ProfielSchil = profiel;
            this.MomentSchil.Profiel = profiel;

            if (this.Slankheid != null)
                this.Slankheid.Profiel = profiel;
            //this.DoorbuigingContext?.Profiel = profiel;
        }

        // Stel Beton in en zorg dat DekkingBoven ook wordt bijgewerkt
        public void SetBeton(BetonContext beton)
        {
            //this.Beton = beton;
            this.Materiaal = beton;
            //

            foreach (var context in Toetsen)
            {
                if (context is BetonDekkingContext dekking)
                {
                    dekking.Beton = beton;
                }
                else if (context is BendingResults buiging)
                {
                    buiging.Beton = beton;
                }
                else if (context is ScheurwijdteContext scheurwijdte)
                {
                    scheurwijdte.Beton = beton;
                }
                else if (context is DwarskrachtWapContext dwarskracht)
                {
                    dwarskracht.Beton = beton;
                }
                else if (context is BetonDoorbuigingContext w)
                {
                    w.Beton = beton;
                }
            }



            //if (DekkingBoven != null)
            //{
            //    DekkingBoven.Beton = beton; // Werk DekkingBoven bij
            //MomentSchil.Beton = beton;

            //}

            // foreach BaseEurocodeContext 
            // hier kun je door alle contexten van de steektrap lopen en het beton setten!
            // mogelijk naar de BaseAssembly verplaatsen
        }

        public void SetGrondslagen(GrondslagenContext grondslagen)
        {
            //this.ProjectInfo.Grondslagen = grondslagen;
            //this.Belastingen.Grondslagen = grondslagen;
            this.Belastingen.Grondslagen = grondslagen;
            this.ProjectInfo.Grondslagen = grondslagen;
            this.PlaatDekking.Boven.Grondslagen = grondslagen;
            this.PlaatDekking.Onder.Grondslagen = grondslagen;
            #pragma warning disable CS0618
                        this.DekkingBoven.Grondslagen = grondslagen;
            #pragma warning restore CS0618

        }

       
       

        //[JsonIgnore]
        //public List<BaseEurocodeContext> Toetsen { get; set; }


        public List<BaseEurocodeContext> GetToetsen()
        {

            return [MomentSchil, PlaatDekking.Boven, Dwarskracht, Scheurwijdte, Slankheid, DoorbuigingValidatie, TandOpleggingBovenzijde!];
        }
        


        public string CategorieOmschrijving { get; set; } = "Berekening conform kiwa criteria 73 - categorie 3";


        // geometrie
        //public double Breedte { get; set; } = 1000;

        private double _optredeMaat = 185;
        [TableColumn("Optrede", stringFormat: "0.# mm", order: 11)]
        public double OptredeMaat
        {
            get => _optredeMaat;
            set
            {
                if (SetProperty(ref _optredeMaat, value))
                    BerekenDoorsnedePolygoon();
            }
        }
        public List<int> OptredeAantal { get; set; } = [8];

        //public double AantredeMaat { get; set; } = 210;

        private double _aantredeMaat = 220;
        [TableColumn("Aantrede", stringFormat: "0.# mm", order: 21)]

        public double AantredeMaat
        {
            get => _aantredeMaat;
            set
            {
                if (SetProperty(ref _aantredeMaat, value))
                {
                    // 🚀 AantredeMaat beïnvloedt SchuineMaat en LengteSchuin
                    OnPropertyChanged(nameof(SchuineMaat));
                    OnPropertyChanged(nameof(LengteSchuin));
                    DoorbuigingBijwerken();
                    BerekenDoorsnedePolygoon();
                }
            }
        }






        public double BaseY
        {
            get
            {
                switch (OnderType)
                {
                    case TredeOnderType.OpVloer: return 0;
                    case TredeOnderType.OpBordes: return -100;
                    case TredeOnderType.IsBordes: return -DikteOnder?? 200;
                    default: return 0;
                }
            }
        }

        private double _welMaat = 40;
        public double WelMaat
        {
            get => GebruikWelHoek
                ? (OptredeMaat - WelMaatVertikaal) * Math.Tan(WelOpgaveHoek.ToRad())
                : _welMaat;
            set
            {
                if (SetProperty(ref _welMaat, value))
                {
                    //DoorbuigingBijwerken();
                    BerekenDoorsnedePolygoon();
                }
            }
        }

        private bool _gebruikWelHoek = true;
        public bool GebruikWelHoek
        {
            get => _gebruikWelHoek;
            set => SetProperty(ref _gebruikWelHoek, value);
        }

        private double _welOpgaveHoek = 15.0;
        public double WelOpgaveHoek
        {
            get => _welOpgaveHoek;
            set => SetProperty(ref _welOpgaveHoek, value);
        }

        public double WelMaatMax => Math.Tan(30.0.ToRad()) * (OptredeMaat - WelMaatVertikaal); // Maximaal welmaat bij 30° helling


        private bool _dragendeTrapBomen = false;
        public bool DragendeTrapBomen
        {
            get => _dragendeTrapBomen;
            set
            {
                if (SetProperty(ref _dragendeTrapBomen, value))
                {
                    BerekenBoomPolygoon();

                }
            }
        }

        private double _trapBomenHoogte = 280;
        public double TrapBomenHoogte
        {
            get => _trapBomenHoogte;
            set
            {
                if (SetProperty(ref _trapBomenHoogte, value))
                    BerekenBoomPolygoon();
            }
        }
        public double TrapBoomBreedte { get; set; } = 60;
        public double TrapBoomAfstand1 { get; set; } = 40;
        public double TrapBoomAfstand2 { get; set; } = 40;
        public double TrapBoomAfstand3 { get; set; } = 18;

        //--- HELPERS
        public double DsnRight => LtProjZ;
        public double DsnLeft => -WelMaat - (DragendeTrapBomen ? TrapBoomAfstand1 : 0);
        public double DsnBottom => 0;
        public double DsnTop => -HoogteTotaal - (DragendeTrapBomen ? TrapBoomAfstand3 : 0);

        // snijpunt boom
        public (Punt a, Punt b) Looplijn => new(
            new(-(decimal)WelMaat, -(decimal)OptredeMaat, 0M), 
            new((decimal)(-WelMaat + AantredeMaat),-(decimal)(2*OptredeMaat),0M));



        // Aanvulling dragende bomen
        



        private double _welMaatVertikaal = 60;
        public double WelMaatVertikaal
        {
            get => _welMaatVertikaal;
            set
            {
                if (SetProperty(ref _welMaatVertikaal, value))
                    BerekenDoorsnedePolygoon();
            }
        }


        public double TopRadius { get; set; } = 3;
        public double BottomRadius { get; set; } = 40;

        [JsonIgnore]
        public List<(double X, double Y)> DoorsnedePolygoon { get; private set; } = [];

        [JsonIgnore]
        public List<(double X, double Y)> BoomPolygoon { get; private set; } = [];




        //private double _schilDikte = 120;
        [TableColumn("Schildikte", stringFormat: "0.# mm", order: 31)]
        public double SchilDikte
        {
            get => MainSlab?.Dikte ?? _profielSchil.Hoogte; // ✅ Lees van MainPlate indien beschikbaar
            set
            {
                // ✅ Sync met MainPlate
                if (MainSlab != null)
                {
                    MainSlab.Dikte = value;
                }
                
                // ✅ Update ProfielSchil (geen ref mogelijk op property)
                var oldValue = _profielSchil.Hoogte;
                _profielSchil.Hoogte = value;
                
                if (oldValue != value)
                    {
                        OnPropertyChanged(nameof(SchilDikte));
                        DoorbuigingBijwerken();
                        BerekenDoorsnedePolygoon();
                    }
            }
        }

        public double AdviesSchildikteMin 
        {
            get
            {
                var basis = 80;

                // optimale schildikte op basis van eigen onderzoek.
                var curve = 5.15 * 1e-6 * Math.Pow(LengteSchuin - 2800, 2) + 0.0264 * (LengteSchuin - 2800);

                var h = Math.Max(curve,0) + basis;

                return (int)h;
            }
                
        }




        public double SchuineMaat
        {
            get
            {
                return Math.Sqrt(Math.Pow(AantredeMaat, 2) + Math.Pow(OptredeMaat, 2));
            }
        }

        public double Hellingshoek
        {
            get => Math.Atan(OptredeMaat / AantredeMaat) * (180.0 / Math.PI);
        }





        public SteekTrapTypeEnum? SteekTrapType { get; set; } = SteekTrapTypeEnum.Standaard;


        //private ProjectInfoEntity _projectInfo;
        //public ProjectInfoEntity ProjectInfo
        //{
        //    get => _projectInfo;
        //    set
        //    {
        //        if (_projectInfo?.Grondslagen != null)
        //            _projectInfo.Grondslagen.PropertyChanged -= OnGrondslagenPropertyChanged;

        //        _projectInfo = value;

        //        if (_projectInfo?.Grondslagen != null)
        //            _projectInfo.Grondslagen.PropertyChanged += OnGrondslagenPropertyChanged;
        //    }
        //}


        protected override void OnGrondslagenPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Alleen herberekenen bij relevante wijzigingen, of altijd:
            this.GetKrachten(); // als de grondslagen wijzigen ook de krachten.
            this.IsAkkoord(); // als de krachten wijzigen ook de validatie.
        }

        protected override void OnBelastingenPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Console.WriteLine("Belastingen gewijzigd");
            this.GetKrachten(); // als de belastingen wijzigen ook de krachten.
            this.IsAkkoord(); // als de krachten wijzigen ook de validatie.
        }



        //private BelastingenContext _belastingen;
        //public Eurocode.Belastingen.BelastingenContext Belastingen
        //{
        //    get
        //    {
        //        // dit wil ik niet hoeven te doen hier, dit moet via eventlistener, maar dat werkt niet
        //        //this.GetKrachten(); // Stack Overflow
        //        return _belastingen;


        //    }
        //    set => SetNestedProperty(ref _belastingen!, value);

        //}

        protected override void NestedPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (ReferenceEquals(sender, _profielSchil))
            {
                Console.WriteLine("[NestedPropertyChanged] ProfielSchil bijgewerkt");
                BerekenDoorsnedePolygoon();
                //this.GetKrachten();
                this.IsAkkoord();
            }
            if (ReferenceEquals(sender, this._dekkingBoven))
            {
                this.IsAkkoord();
            }
            if (ReferenceEquals(sender, Beton))
            {
                this.IsAkkoord();
            }


        }

        private SectionForces? _snedekrachten;
        private SectionForces? _snedekrachtenBGT;

        //private Snedekrachten? _snedekrachtenEind;



        /// <summary>
        /// Eurocode2 Betonconstructies
        /// </summary>
        //public Eurocode.BetonConstructies.BetonContext Beton;
        public WapeningContext WapeningSchil { get; set; }
        
        

        /// <summary>
        /// ✅ NIEUW: Plaatwapening voor trap schil (delegeert naar MainPlate).
        /// Voor trap: LaagHoofdwapening = 1 (in plaats van 2 bij bordes).
        /// </summary>
        [JsonIgnore]
        public PlaatWapening? PlaatWapening
        {
            get => MainSlab?.PlaatWapening;
            set
            {
                if (MainSlab != null)
                {
                    MainSlab.PlaatWapening = value;
                }
            }
        }
        
        //public WapeningContext WapeningSchilVerdeel;
        //public WapeningContext WapeningBoven;
        //[Obsolete]
        //public WapeningContext WapeningOnder { get; set; } = new("10-149");


        private BetonDekkingContext _dekkingBoven = new();

        [Obsolete("Gebruik Dekking.Boven van de Assemblage")]
        public BetonDekkingContext DekkingBoven
        {
            get => _dekkingBoven;
            set => SetNestedProperty(ref _dekkingBoven!, value);
        }

        // doorbuiging
        //public DoorbuigingStudie DoorbuigingOLD { get; set; } = new();

        //public DoorbuigingTrap
        public BetonDoorbuigingContext DoorbuigingContext { get; set; } = default!;

        public DoorbuigingValidatieContext DoorbuigingValidatie { get; set; } = default!;
        public List<DoorbuigingCombinatieContext> DoorbuigingCombinatieContexts { get; set; } = [];


        //[Obsolete("Gebruik BetonDoorbuigingContext")]
        //public DoorbuigingTrap Doorbuiging { get; set; } = new();






        // nullables
        public double? LengteOnder { get; set; } = 0;
        public double? LengteTussen { get; set; } = 1000;
        public double? LengteBoven { get; set; } = 500;

        public double LengteBovenNetto => (LengteBoven ?? AantredeMaat) - WelMaat;

        public double? DikteOnder { get; set; } = 200;
        public double? DikteTussen { get; set; } = 200;
        public double? DikteBoven { get; set; } = 200;

        public TredeOnderType OnderType { get; set; } = TredeOnderType.OpVloer;


        // wapening
        //public string WapeningOnder { get; set; } = "8-150";
        //public string WapeningBoven { get; set; } = "8-150";


        // berekend
        private int _optredeAantal1 = 16;
        public int OptredeAantal1
        {
            get => _optredeAantal1;
            set
            {
                if (SetProperty(ref _optredeAantal1, value))
                {
                    //DoorbuigingBijwerken();
                    BerekenDoorsnedePolygoon();
                }
            }
        }


        public int OptredeAantal2 { get; set; } = 8;

        //public double AsAppliedOnder
        //{
        //    get
        //    {
        //       return WapeningHelper.GetDsnOpp(WapeningOnder);
        //   }
        //
        public List<PointF> Hartlijn => SteekTrapExtensions.GetHartlijn(this);


        //public double LengteTotaal => SteekTrapExtensions.GetLengteTotaal(this);

        public bool GebruikEigenLengte
        {
            get => _gebruikEigenLengte;
            set => SetProperty(ref _gebruikEigenLengte, value);
        }

        private bool _gebruikEigenLengte;
        private double? _lengteTotaalEigenOpgave;
        public double? LengteTotaalEigenOpgave
        {
            get => _lengteTotaalEigenOpgave;
            set => SetProperty(ref _lengteTotaalEigenOpgave, value);
        }

        private bool _gebruikEigenGewicht;
        public bool GebruikEigenGewicht
        {
            get => _gebruikEigenGewicht;
            set => SetProperty(ref _gebruikEigenGewicht, value);
        }

        private double? _eigenGewichtPerM2Opgave;

        public override double EigenGewichtPerM2
        {
            get
            {
                if (GebruikEigenGewicht && _eigenGewichtPerM2Opgave.HasValue)
                    return _eigenGewichtPerM2Opgave.Value;
                return this.GetGk();
            }
            set
            {
                _eigenGewichtPerM2Opgave = value;
            }
        }


        [TableColumn("LengteElement", Order = 999, StringFormat = "0 mm" )]
        public double LengteElement
        {
            get
            {
                return (LengteOnder ?? AantredeMaat) + (OptredeAantal1 * AantredeMaat) + (LengteBoven ?? AantredeMaat);

            }
        }

        [TableColumn("L~t~ (proj.z)", Order = 50, StringFormat = "0 mm")]
        public double LtProjZ
        {
            get
            {
                if (GebruikEigenLengte && _lengteTotaalEigenOpgave.HasValue)
                    return _lengteTotaalEigenOpgave.Value;
                else
                    return SteekTrapExtensions.GetLengteTotaal(this);
            }
            set
            {
                _lengteTotaalEigenOpgave = value;
            }
        }

       
        



        private PuntD _startPunt = new();
        public PuntD StartPunt
        {
            get => _startPunt;
            set => _startPunt = value;
        }

        private PuntD _eindPunt = new();
        public PuntD EindPunt
        {
            get => _eindPunt;
            set => _eindPunt = value;
        }

        private List<PuntD> _knoopPunten = [];
        public List<PuntD> KnoopPunten
        {
            get => _knoopPunten;
            set => _knoopPunten = value;
        }

        


        public double LengteSchuin
        {
            get
            {

                if (GebruikEigenLengte)
                    return Math.Sqrt(Math.Pow(LtProjZ, 2) + Math.Pow(HoogteTotaal, 2));

                return LtProjZ * SchuineMaat / AantredeMaat;
            }
        }


        private void SlankheidBijwerken()
        {
            if (Slankheid != null)
            {
                Slankheid.LengteOverspanning = (int)(LtProjZ * SchuineMaat / AantredeMaat);
            }
        }

        private void DoorbuigingBijwerken()
        {
            Console.WriteLine("[Doorbuiging.Bijwerken]");
            if (this.Krachten == null)
            {
                this.Krachten = SteekTrapExtensions.GetKrachten(this);
            }

            if (this.DoorbuigingValidatie == null) 
                return;


            this.DoorbuigingValidatie.LengteMM = this.LengteSchuin;
            this.DoorbuigingValidatie.CombinatieTypeBijkomend = BelastingCombinatieTypeEnum.Frequent;
            this.DoorbuigingValidatie.CombinatieTypeEind = BelastingCombinatieTypeEnum.QuasiBlijvend;
            this.DoorbuigingValidatie.FactorBijkomend = 0.002;
            this.DoorbuigingValidatie.FactorEind = 0.004;
            this.DoorbuigingValidatie.FactorZeeg = 0.0;
            this.DoorbuigingValidatie.Wapening = this.PlaatWapening?.Onder?.BasisWapening ?? new();
            this.DoorbuigingValidatie.Profiel = this.MainSlab?.Profiel ?? this.ProfielSchil;

            // Sync ProfielSchil.Hoogte met MainSlab.Dikte zodat MomentSchil.Profiel correct blijft
            if (MainSlab != null && _profielSchil.Hoogte != MainSlab.Dikte)
                _profielSchil.Hoogte = MainSlab.Dikte;


            var blijvend = this.DoorbuigingValidatie.CombinatieContexts.FirstOrDefault(c => c.CombinatieType == BelastingCombinatieTypeEnum.Blijvend);
            var quasiBlijvend = this.DoorbuigingValidatie.CombinatieContexts.FirstOrDefault(c => c.CombinatieType == BelastingCombinatieTypeEnum.QuasiBlijvend);
            var frequent = this.DoorbuigingValidatie.CombinatieContexts.FirstOrDefault(c => c.CombinatieType == BelastingCombinatieTypeEnum.Frequent);

            if (blijvend != null) 
                blijvend.Lijnlast = -this.Krachten.LijnlastG * FactorProjectieZToLocalZ; // negatief = neerwaarts

            if (quasiBlijvend != null)
                quasiBlijvend.Lijnlast = -this.Krachten.LijnlastQuasiPermanent * FactorProjectieZToLocalZ;

            if (frequent != null)
                frequent.Lijnlast = -this.Krachten.LijnlastFrequent * FactorProjectieZToLocalZ;

            if (!this.DoorbuigingValidatie.BerekenEnValideer())
            {
                Console.WriteLine("Doorbuiging niet akkoord");
                foreach (var m in this.DoorbuigingValidatie.Meldingen.ToList())
                {
                    Console.WriteLine($"{m}");
                }
            }


            




            if (this.DoorbuigingContext == null)
                return;



            this.DoorbuigingContext.LengteMM = this.LengteSchuin;
            this.DoorbuigingContext.Lijnlast = this.Krachten.LijnlastQuasiPermanent * this.FactorProjectieZToLocalZ;
            this.DoorbuigingContext.LijnlastG = this.Krachten.LijnlastG * this.FactorProjectieZToLocalZ;
            this.DoorbuigingContext.D = this.SchilDikte - this.WapeningSchil.ReferentieAfstand;
            Debug.WriteLine("doorbuiging context bijgewerkt");

            this.DoorbuigingContext.BerekenEnValideer();
            Console.WriteLine($"w_tot::::{DoorbuigingContext.Weind:0.0} mm");
            Console.WriteLine($"w_bijk::::{DoorbuigingContext.Wbijk:0.0} mm");
            Console.WriteLine($"doorbuigin akkoord::::{DoorbuigingContext.IsValidated}");


            //this.Doorbuiging.BerekenEnValideer();
            //Debug.WriteLine($"doorbuiging berekend (IsValidated:{this.Doorbuiging.IsValidated})");


        }

        /// <summary>
        /// Cos^2 = (A/S)^2
        /// </summary>
        public double FactorProjectieZToLocalZ
        {
            get
            {
                if (GebruikEigenLengte)
                    return Math.Pow(LtProjZ / LengteSchuin, 2);

                return Math.Pow(AantredeMaat / SchuineMaat, 2);
            }
        }





        private bool _gebruikEigenHoogte;
        public bool GebruikEigenHoogte
        {
            get => _gebruikEigenHoogte;
            set => SetProperty(ref _gebruikEigenHoogte, value);
        }

        private double? _hoogteTotaalEigenOpgave;

        [TableColumn("Hoogte", Order = 50, StringFormat = "0.## mm")]
        public double HoogteTotaal
        {
            get
            {
                if (GebruikEigenHoogte && _hoogteTotaalEigenOpgave.HasValue)
                    return _hoogteTotaalEigenOpgave.Value;
                return SteekTrapExtensions.GetHoogteTotaal(this);
            }
            set
            {
                _hoogteTotaalEigenOpgave = value;
            }
        }





        public bool HeeftBoventand { get; set; } = true;
        public bool HeeftOndertand { get; set; } = false;
        public TandOplegging? TandOpleggingBovenzijde { get; set; } = null;
        public TandOplegging? TandOpleggingOnderzijde { get; set; } = null;

        public double MinimaleTandhoogte
        {
            get
            {
                var c1 = this.PlaatDekking.Boven.DekkingToe;
                var c2 = this.PlaatDekking.Onder.DekkingToe;
                var diam = this.TandOpleggingBovenzijde?.WapeningAlgemeen.GemiddeldeDiameter ?? 8;

                var minHoogte = c1 + c2 + 7 * diam;

                minHoogte /= 10;
                minHoogte = Math.Ceiling(minHoogte) * 10;

                return minHoogte;
            }
        }

        public double MaximaleTandLengte
        {
            get
            {
                
                return AantredeMaat - 80;
            }
        }

        //public UitkragingContext DemoUitkraging { get; set; } = new();

        //public BendingResults DemoBuiging { get; set; } = new();
        public BendingResults BuigingBGT { get; set; } = new();

        
        public List<BendingResults> MomentCollectie { get; set; } = [];

        private KrachtenDemo _krachten = default!;
        [JsonIgnore]
        public KrachtenDemo Krachten
        {
            get => _krachten;
            set => SetProperty(ref _krachten, value);
        }


        private KrachtenDemo _spiegelKrachten = default!;
        [JsonIgnore]
        public KrachtenDemo SpiegelKrachten
        {
            get => _spiegelKrachten;
            set => SetProperty(ref _spiegelKrachten, value);
        }

        private KrachtenDemo _boomKrachten = default!;
        [JsonIgnore]
        public KrachtenDemo BoomKrachten
        {
            get => _boomKrachten;
            set => SetProperty(ref _boomKrachten, value);
        }



        public double ReactieG => Krachten.Vk1;
        public double ReactieQ => Math.Max(Krachten.Vk2, Krachten.Vk3);

        public SectionForces Snedekrachten
        {
            get
            {
                if (_snedekrachten == null)
                {
                    _snedekrachten = SteekTrapExtensions.GetSnedekrachten(this);
                }
                return _snedekrachten;
            }
        }

        public SectionForces SnedekrachtenBGT
        {
            get
            {
                if (_snedekrachtenBGT == null)
                {
                    _snedekrachtenBGT = SteekTrapExtensions.GetSnedekrachten(this, true);
                }
                return _snedekrachtenBGT;
            }
        }


        //public Snedekrachten Snedekrachten => SteekTrapExtensions.GetSnedekrachten(this);
        private BetonProfiel _profielSchil = new();

        [JsonIgnore] // wordt gevuld bij init
        public BetonProfiel ProfielSchil
        {
            get => _profielSchil;
            set => SetNestedProperty(ref _profielSchil!, value);
        }



        private DwarskrachtWapContext _dwarskracht = new();

        [JsonIgnore]
        public DwarskrachtWapContext Dwarskracht
        {
            get => _dwarskracht;
            set => SetNestedProperty(ref _dwarskracht!, value);
            //{
            //    return new(this.Beton, ProfielSchil, Snedekrachten) { NutHoogte = this.MomentSchil.D, AsLangs = this.MomentSchil.AsApplied };
            //}
        }


        /// <summary>
        /// Toets voor de slankheid van het element
        /// </summary>
        [JsonIgnore]
        public GrenswaardeSlankheidContext Slankheid { get; set; } = default!;

        /// <summary>
        /// Toets voor de buiging momentwapening van de schil
        /// </summary>
        [JsonIgnore]
        public BendingResults MomentSchil { get; set; }

        [JsonIgnore]
        public ScheurwijdteContext Scheurwijdte { get; set; } = new();


        public void UpdateSnedekrachten(
            double mEd, double mEqp, double vEd)
        {
            // Controleer of het Snedekrachten object al bestaat
            if (_snedekrachten != null)
            {
                // Update de waarden van Snedekrachten met de doorgegeven waardes of de standaard waarde (0)
                _snedekrachten.My = mEd;
                _snedekrachten.Vz = vEd;
            }
            if (_snedekrachtenBGT != null)
            {
                _snedekrachtenBGT.My = mEqp;
            }
        }

        /// <summary>
        /// ✅ NIEUW: Creëer MainPart (PlatePart) voor steektrap.
        /// Wordt aangeroepen in constructor.
        /// PlaatDekking en PlaatWapening worden later gezet via Init().
        /// </summary>
        private SlabPart CreateMainPart()
        {
            return new SlabPart
            {
                Name = "Trapschil",
                Dikte = 120, // Standaard schildikte
                Material = this.Materiaal, // ✅ Shared reference
                ParentAssemblage = this
                // ⚠️ PlaatDekking wordt gezet in Init() / RestoreReferencesAfterDeserialization()
            };
        }

        /// <summary>
        /// ✅ NIEUW: Helper property voor type-safe access naar MainPart als SlabPart.
        /// Returns null indien MainPart geen SlabPart is.
        /// </summary>
        [JsonIgnore]
        public SlabPart? MainSlab => MainPart as SlabPart;

        /// <summary>
        /// Berekent de absolute polygoonpunten van de doorsnede en slaat ze op in DoorsnedePolygoon.
        /// Wordt aangeroepen aan het einde van IsAkkoord().
        /// </summary>
        private void BerekenDoorsnedePolygoon()
        {
            var punten = new List<(double X, double Y)>();

            if (GebruikEigenLengte)
            {
                punten.Add((0, 0));
                punten.Add((LtProjZ, 0));
                punten.Add((LtProjZ, -HoogteTotaal));
                punten.Add((0, -HoogteTotaal));
                DoorsnedePolygoon = punten;
                BerekenBoomPolygoon();
                return;
            }

            double wel = WelMaat;
            double welV = WelMaatVertikaal;
            double tandHoogte = TandOpleggingBovenzijde?.TandHoogte ?? 0;
            double tandLengte = TandOpleggingBovenzijde?.TandLengte ?? 0;
            double halsDikte = TandOpleggingBovenzijde?.HalsDikte ?? AantredeMaat;

            double absX = 0, absY = 0;
            punten.Add((absX, absY));

            for (int i = 0; i < OptredeAantal1; i++)
            {
                double optredeNetto = OptredeMaat - welV;
                if (optredeNetto > 0)
                {
                    absX -= wel;
                    absY -= optredeNetto;
                    punten.Add((absX, absY));
                }

                if (welV > 0)
                {
                    absY -= welV;
                    punten.Add((absX, absY));
                }

                double aantredeBreedte = (TandOpleggingBovenzijde != null && i == OptredeAantal1 - 1)
                    ? halsDikte + tandLengte
                    : AantredeMaat;
                double aantredeNetto = aantredeBreedte + wel;
                if (aantredeNetto > 0)
                {
                    absX += aantredeNetto;
                    punten.Add((absX, absY));
                }
            }

            absY += tandHoogte;
            punten.Add((absX, absY));
            absX -= tandLengte;
            punten.Add((absX, absY));

            double x2 = TandOpleggingBovenzijde != null
                ? (OptredeAantal1 - 1) * AantredeMaat + halsDikte
                : AantredeMaat * OptredeAantal1 - tandLengte;
            double y2 = -OptredeMaat * OptredeAantal1 - tandHoogte;

            var (q1x, q1y, q2x, q2y) = DoorsnedeOffset((0, 0), (AantredeMaat, -OptredeMaat), SchilDikte);

            var rightIntersect = DoorsnedeSnijpunt(
                (q1x, q1y), (q2x, q2y),
                (x2, y2), (x2, y2 + 1000));

            var leftIntersect = DoorsnedeSnijpunt(
                (q1x, q1y), (q2x, q2y),
                (0, 0), (1000, 0));

            if (rightIntersect is { } r) punten.Add((r.X, r.Y));
            if (leftIntersect is { } l) punten.Add((l.X, l.Y));

            DoorsnedePolygoon = punten;
            BerekenBoomPolygoon();
        }

        /// <summary>
        /// Berekent de polygoonpunten van de trapboom (stringer).
        /// Leeg als TrapBomen = false. Anders een parallellogram parallel aan de schilonderkant,
        /// verschoven over TrapBomenHoogte loodrecht op het schilvlak.
        /// </summary>
        private void BerekenBoomPolygoon()
        {
            if (!DragendeTrapBomen)
            {
                BoomPolygoon = [];
                return;
            }

            // Schil onderkant: loodrechte offset van de looplijnrichting
            var (q1x, q1y, q2x, q2y) = DoorsnedeOffset((0, 0), (AantredeMaat, -OptredeMaat), SchilDikte);

            double tandHoogte = TandOpleggingBovenzijde?.TandHoogte ?? 0;
            double tandLengte = TandOpleggingBovenzijde?.TandLengte ?? 0;
            double halsDikte = TandOpleggingBovenzijde?.HalsDikte ?? AantredeMaat;
            double x2 = TandOpleggingBovenzijde != null
                ? (OptredeAantal1 - 1) * AantredeMaat + halsDikte
                : AantredeMaat * OptredeAantal1 - tandLengte;
            double y2 = -OptredeMaat * OptredeAantal1 - tandHoogte;

            // Bovenzijde boom = onderkant schil
            var topLeft  = DoorsnedeSnijpunt((q1x, q1y), (q2x, q2y), (0, 0),   (1000, 0));        // snijdt y=0
            var topRight = DoorsnedeSnijpunt((q1x, q1y), (q2x, q2y), (x2, y2), (x2, y2 + 1000)); // snijdt x=x2

            if (topLeft is not { } tl || topRight is not { } tr)
            {
                BoomPolygoon = [];
                return;
            }

            // Loodrechte offsetvector voor boomhoogte (zelfde richting als schildikte)
            double schuine = Math.Sqrt(AantredeMaat * AantredeMaat + OptredeMaat * OptredeMaat);
            double ox = OptredeMaat / schuine * TrapBomenHoogte;
            double oy = AantredeMaat / schuine * TrapBomenHoogte;

            BoomPolygoon =
            [
                (tl.X,      tl.Y),
                (tr.X,      tr.Y),
                (tr.X + ox, tr.Y + oy),
                (tl.X + ox, tl.Y + oy),
            ];
        }

        private static (double, double, double, double) DoorsnedeOffset(
            (double X, double Y) p1, (double X, double Y) p2, double offset)
        {
            double dx = p2.X - p1.X, dy = p2.Y - p1.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            double ox = -dy / len * offset, oy = dx / len * offset;
            return (p1.X + ox, p1.Y + oy, p2.X + ox, p2.Y + oy);
        }

        private static (double X, double Y)? DoorsnedeSnijpunt(
            (double X, double Y) p1, (double X, double Y) p2,
            (double X, double Y) q1, (double X, double Y) q2)
        {
            double dx1 = p2.X - p1.X, dy1 = p2.Y - p1.Y;
            double dx2 = q2.X - q1.X, dy2 = q2.Y - q1.Y;
            double det = dx1 * dy2 - dy1 * dx2;
            if (Math.Abs(det) < 1e-9) return null;
            double t = ((q1.X - p1.X) * dy2 - (q1.Y - p1.Y) * dx2) / det;
            return (p1.X + t * dx1, p1.Y + t * dy1);
        }


    }



}
