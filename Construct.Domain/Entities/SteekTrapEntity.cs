using CommonLibrary;
using CommonLibrary.Helpers;
using Eurocode.Belastingen;
using Eurocode.BetonConstructies;
using Eurocode.Grondslagen;
//using Kaskon.Toolbox.PrefabModels;
using Microsoft.AspNetCore.Components;
using Profielen.Beton;
using Profielen.Parametrisch;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text.Json.Serialization;

namespace Construct.Domain.Entities
{
    
    public class SteekTrapContextWrapper : BaseEurocodeContext
    {
        private SteekTrapEntity _entity;
        public SteekTrapContextWrapper(SteekTrapEntity entity) => _entity = entity;

        [TableColumn(Label = "breedte", Symbol = "b", Unit = "mm")]
        public double Breedte
        {
            get => _entity.Breedte;
            set => _entity.Breedte = value;
        }

        [TableColumn(Label = "aantal optreden", Symbol = "n", Unit = "st")]
        public int AantalOptreden
        {
            get => _entity.OptredeAantal1;
            set => _entity.OptredeAantal1 = value;
        }

        [TableColumn(Label = "dekking", Symbol = "c<sub>toe</sub>", Unit = "mm")]
        public int Dekking
        {
            get => (int)_entity.PlaatDekking.Boven.DekkingToe;
            set => _entity.PlaatDekking.Boven.DekkingToe = value;
        }

        [TableColumn(Label = "schildikte", Symbol = "d<sub>schil</sub>", Unit = "mm")]
        public double Schildikte
        {
            get => _entity.ProfielSchil.Hoogte;
            set => _entity.ProfielSchil.Hoogte = value;
        }

        [TableColumn(Label = "optrede", Symbol = "o<sub></sub>", Unit = "mm")]
        public double Optrede
        {
            get => _entity.OptredeMaat;
            set => _entity.OptredeMaat = value;
        }


        [TableColumn(Label = "aantrede", Symbol = "a<sub></sub>", Unit = "mm")]

        public double Aantrede
        {
            get => _entity.AantredeMaat;
            set => _entity.AantredeMaat = value;
        }





        //[TableColumn(Label = "Doorbuiging akkoord")]
        //public bool DoorbuigingAkkoord => _entity.Doorbuiging.IsValidated;

        //[TableColumn(Label = "Schil wapening")]
        //public bool WapeningSchilAkkoord => _entity.WapeningSchil.IsValidated;

        //[TableColumn(Label = "Schil wapening")]
        //public bool ScheurwijdteAkkooord => _entity.Scheurwijdte.IsValidated;



        // context models
        //public BetonContext Beton => _entity.Beton;
        public WapeningContext Wapening => _entity.WapeningSchil;
        public ParametrischProfielContext Profiel => _entity.ProfielSchil;
        public BetonDekkingContext DekkingBoven => _entity.PlaatDekking.Boven;
        public BendingResults MomentSchil => _entity.MomentSchil;
        public ScheurwijdteContext Scheurwijdte => _entity.Scheurwijdte;
        //public DoorbuigingTrap Doorbuiging => _entity.Doorbuiging;


        // andere properties hier toevoegen indien gewenst





        protected override void Bereken()
        {
            //
        }

        protected override bool Valideer()
        {
            return true;
        }
    }



    public class SteekTrapEntity : BetonAssemblageEntity
    {
        /// <summary>
        /// Na het deserialiseren van de json moeten we de nested properties instellen. Dit doen we met deze Init methode.
        /// </summary>
        /// <param name="projectInfo">De projectinfo</param>
        public override void Init(ProjectInfoEntity projectInfo)
        {
            ProjectInfo = projectInfo; // projectinfo + grondslagen

            Belastingen = new(grondslagen: ProjectInfo.Grondslagen); // Als null, nieuwe aanmaken

            // herstel de parent-relatie (indien uit json geladen, moet dit opnieuw aangemaakt worden)
            Belastingen.Grondslagen = ProjectInfo.Grondslagen;

            // ⚠️ CRITICAL FIX: Zet GEEN nieuw Materiaal als het al is ingesteld via RestoreReferencesAfterDeserialization!
            // Materiaal ??= new BetonContext();  // ❌ REMOVED - Dit overwrites JSON values!
            
            // Nur initialize materiaal if BOTH null: Materiaal AND MateriaalId
            if (Materiaal == null) // && !MateriaalId.HasValue) // <-- Safety check: als null is, altijd herstellen
            {
                Materiaal = new BetonContext();  // Fallback only if truly not set
            }

            var beton = Materiaal as BetonContext;

            // Dekking is een afgeleide (van Grondslagen en Beton),
            // maak dus een nieuw object aan om de referenties te herstellen.
            // de top-level properties kunnen worden uitgelezen van het opgeslagen object (indien aanwezig)

            PlaatDekking.Boven = new(grondslagen: ProjectInfo.Grondslagen, beton: beton ?? new())
            {
                IsPlaatGeometrie = PlaatDekking.Boven?.IsPlaatGeometrie ?? true,
                IsKwaliteitsBeheersing = PlaatDekking.Boven?.IsKwaliteitsBeheersing ?? true,
                SelectedMilieuklassen = PlaatDekking.Boven?.SelectedMilieuklassen ?? [Eurocode.BetonConstructies.MilieuklasseEnum.XC1],
                DekkingToe = PlaatDekking.Boven?.DekkingToe ?? 20,
            };



            DekkingBoven = new(grondslagen: ProjectInfo.Grondslagen, beton: beton ?? new())
            {
                //IsPlaatGeometrie = DekkingBoven?.IsPlaatGeometrie ?? true,
                //IsKwaliteitsBeheersing = DekkingBoven?.IsKwaliteitsBeheersing ?? true,
                //SelectedMilieuklassen = DekkingBoven?.SelectedMilieuklassen ?? [Eurocode.BetonConstructies.MilieuklasseEnum.XC1],
                //DekkingToe = DekkingBoven?.DekkingToe ?? 20,
            };


            DemoUitkraging = new() { Beton = beton ?? new() };

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
                Wapening = WapeningSchil,
                Snedekrachten = SnedekrachtenBGT
            };


            WapeningSchil = new(this.WapeningSchil?.Tekst ?? "8-150", 30);



            // Zorg dat het moment wordt bijgewerkt, zodat alle referenties worden aangelegd
            MomentSchil = new()
            {
                Heading = "Momentwapening schil",
                Name = MomentSchil?.Name ?? "schil",
                Beton = beton ?? new(),
                Profiel = ProfielSchil,
                Wapening = WapeningSchil,
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
                LengteOverspanning = this.LengteTotaal,
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
                new DoorbuigingCombinatieContext(){CombinatieType = BelastingCombinatieTypeEnum.Blijvend, Lijnlast = Krachten?.qG ?? 0},
                new DoorbuigingCombinatieContext(){CombinatieType = BelastingCombinatieTypeEnum.QuasiBlijvend, Lijnlast = Krachten?.qEqp ?? 0},
                new DoorbuigingCombinatieContext(){CombinatieType = BelastingCombinatieTypeEnum.Frequent, Lijnlast = Krachten?.qEfr ?? 0},
            ];
            DoorbuigingValidatie = new(beton ?? new(), ProfielSchil, WapeningSchil, LengteSchuin, DoorbuigingCombinatieContexts);
            DoorbuigingValidatie.Init();

            //Doorbuiging = new(DoorbuigingContext)
            //{
            //    Heading = "Doorbuiging",
            //    IsToetsingEind = true,
            //};


            Scheurwijdte = new(SnedekrachtenBGT, beton ?? new(), DekkingBoven, ProfielSchil, WapeningSchil, ProjectInfo.Grondslagen.NationaleBijlage ?? Eurocode.Grondslagen.NationaleBijlageEnum.EU)
            {
                Heading = "Scheurwijdte schil"
            };





            TandOpleggingBovenzijde = new(this, this.TandOpleggingBovenzijde?.Oplegging ?? new())
            {

            };

            PlaatDekking.Boven.PropertyChanged += OnDekkingContextChanged;


            // doorbuiging (context bijwerken, voor alle non-ref properties)
            DoorbuigingBijwerken();

            


            // check akkoord
            // dit berekent alle geneste eurocode onderdelen
            // en slaat akkoord status op
            IsAkkoord();


        }





        public string ToGeometryString()
        {
            List<string> results = [];

            results.Add("optrede");

            return string.Join(", ", results);


        }




        public MarkupString ToMomentMarkupString()
        {
            return Krachten.GetMomentMarkupString();
        }
        public MarkupString ToGeometrieEnBelastingMarkupString()
        {
            return Krachten.GetGeometrieEnBelastingMarkupString();
        }

        public MarkupString ToGeometrieMarkupString()
        {
            return Krachten.GetGeometrieMarkupString();
        }

        public MarkupString ToBelastingMarkupString()
        {
            return Krachten.GetBelastingMarkupString();
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

            if (HoogteTotaal > 4000)
            {
                Meldingen.Add(new(MeldingType.Opmerking, "trap te hoog"));
            }
            if (AantredeMaat + WelMaat < 210)
            {
                Meldingen.Add(new(MeldingType.Opmerking, "aantrede te klein"));
            }
            if (OptredeMaat > 190)
            {
                Meldingen.Add(new(MeldingType.Opmerking, "optrede te groot"));
            }

            this.GetKrachten(); // altijd bijwerken, mogelijk veranderd
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
                SelectedMilieuklassen = [MilieuklasseEnum.XC1]
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
            Stopwatch sw = new();
            sw.Start();
            ProjectInfo = projectInfo;
            Belastingen = new(grondslagen: ProjectInfo.Grondslagen);
            //Beton = new("C45/55");

            Materiaal = new BetonContext("C45/55");
            var beton = Materiaal as BetonContext;


            PlaatDekking.Onder = new(grondslagen: ProjectInfo.Grondslagen, beton: beton ?? new())
            {
                IsPlaatGeometrie = true,
                IsKwaliteitsBeheersing = true,
                SelectedMilieuklassen = [MilieuklasseEnum.XC1]
            };
            PlaatDekking.Boven = new(grondslagen: ProjectInfo.Grondslagen, beton: beton ?? new())
            {
                IsPlaatGeometrie = true,
                IsKwaliteitsBeheersing = true,
                SelectedMilieuklassen = [Eurocode.BetonConstructies.MilieuklasseEnum.XC1],
            };

            DemoUitkraging = new() { Beton = beton ?? new() };
            //DemoBuiging = new() { Beton = Beton };

            // Profiel voor de schil, bepaalt de schildikte
            ProfielSchil = new() { Breedte = 1000, Hoogte = 120 };
            WapeningSchil = new("12-150", PlaatDekking.Onder);


            
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
            
            Console.WriteLine($"[SteekTrapEntity] aangemaakt ({sw.ElapsedMilliseconds} ms)");
        }

        /// <summary>
        /// Herstelt object-referenties na JSON-deserialisatie.
        /// BELANGRIJK: Roept Init() aan NADAT Materiaal is hersteld!
        /// </summary>
        public override void RestoreReferencesAfterDeserialization(ProjectEntity project)
        {
            // ✅ EERST: Herstel alle basisreferenties (materiaal, belastingen, etc.)
            base.RestoreReferencesAfterDeserialization(project);

            // 🔍 BROKEN LINK DETECTION: MateriaalId is gezet, maar materiaal niet gevonden in dictionary
            // Dit gebeurt als de JSON een MateriaalId bevat die niet (meer) bestaat in project.Materialen
            if (Materiaal == null && MateriaalId.HasValue)
            {
                Console.WriteLine($"⚠️ [SteekTrapEntity] Materiaal met ID {MateriaalId} niet gevonden in project. Zoek bestaand C45/55 materiaal of maak nieuwe aan.");
                
                // Probeer eerst een bestaand C45/55 materiaal te vinden
                var bestaandC45 = project.Materialen.Values
                    .OfType<BetonContext>()
                    .FirstOrDefault(b => b.Betonsterkteklasse == BetonsterkteklasseEnum.C45_55);
                
                if (bestaandC45 != null)
                {
                    Console.WriteLine($"✅ Bestaand C45/55 materiaal gevonden (ID: {bestaandC45.Id}). Gebruik deze.");
                    Materiaal = bestaandC45;
                    MateriaalId = bestaandC45.Id; // Update de MateriaalId naar het gevonden materiaal
                }
                else
                {
                    Console.WriteLine("⚠️ Geen bestaand C45/55 materiaal gevonden. Maak nieuwe aan.");
                    var nieuwMateriaal = new BetonContext("C45/55"); 
                    Materiaal = nieuwMateriaal;
                    MateriaalId = nieuwMateriaal.Id;

                    // Voeg toe aan project zodat het de volgende keer wel gevonden wordt
                    project.Materialen[nieuwMateriaal.Id] = nieuwMateriaal;
                }
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

        public override void Bijwerken()
        {
            DoorbuigingBijwerken();
            Scheurwijdte.BerekenEnValideer();

        }

        public void SetProfiel(BetonProfiel profiel)
        {
            this.ProfielSchil = profiel;
            this.MomentSchil.Profiel = profiel;
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
            this.DekkingBoven.Grondslagen = grondslagen;

        }

       





        //[JsonIgnore]
        //public List<BaseEurocodeContext> Toetsen { get; set; }


        public List<BaseEurocodeContext> GetToetsen()
        {
            return [MomentSchil, DekkingBoven, Dwarskracht, Scheurwijdte, DoorbuigingValidatie];
        }
        


        public string CategorieOmschrijving { get; set; } = "Berekening conform kiwa criteria 73 - categorie 3";


        // geometrie
        //public double Breedte { get; set; } = 1000;

        [TableColumn("Optrede", stringFormat: "0.# mm", order: 11)]
        public double OptredeMaat { get; set; } = 185;
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
                }
            }
        }







        private double _welMaat = 50;
        public double WelMaat
        {
            get => _welMaat;
            set
            {
                if (SetProperty(ref _welMaat, value))
                {
                    //DoorbuigingBijwerken();
                }
            }
        }

        public double WelMaatVertikaal { get; set; } = 50;


        public double TopRadius { get; set; } = 3;
        public double BottomRadius { get; set; } = 40;



        //private double _schilDikte = 120;
        [TableColumn("Schildikte", stringFormat: "0.# mm", order: 31)]
        public double SchilDikte
        {
            get => _profielSchil.Hoogte;
            //set
            //{
            //    if (SetProperty(ref _profielSchil.Hoogte, value))
            //    {
            //        DoorbuigingBijwerken();
            //    }

            //}
        }





        public double SchuineMaat
        {
            get
            {
                return Math.Sqrt(Math.Pow(AantredeMaat, 2) + Math.Pow(OptredeMaat, 2));
            }
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
                this.GetKrachten();
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
        public BetonDoorbuigingContext DoorbuigingContext { get; set; }

        public DoorbuigingValidatieContext DoorbuigingValidatie { get; set; }
        public List<DoorbuigingCombinatieContext> DoorbuigingCombinatieContexts { get; set; } = [];


        //[Obsolete("Gebruik BetonDoorbuigingContext")]
        //public DoorbuigingTrap Doorbuiging { get; set; } = new();




        // diverse
        [TableColumn("Afwerking", Order = 50, StringFormat = "0.## kN/m²")]
        public double BelastingAfwerking { get; set; } = 0;


        // nullables
        public double? LengteOnder { get; set; } = 1000;
        public double? LengteTussen { get; set; } = 1000;
        public double? LengteBoven { get; set; } = 1000;

        public double? DikteOnder { get; set; } = 200;
        public double? DikteTussen { get; set; } = 200;
        public double? DikteBoven { get; set; } = 200;


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

        [TableColumn("Lengte", Order = 50, StringFormat = "0.## mm")]
        public double LengteTotaal
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

        public double LengteSchuin
        {
            get
            {

                if (GebruikEigenLengte)
                    return Math.Sqrt(Math.Pow(LengteTotaal, 2) + Math.Pow(HoogteTotaal, 2));

                return LengteTotaal * SchuineMaat / AantredeMaat;
            }
        }

        private void DoorbuigingBijwerken()
        {
            Console.WriteLine("[Doorbuiging.Bijwerken]");
            if (this.Krachten == null)
            {
                this.Krachten = SteekTrapExtensions.GetKrachten(this);
            }

            if (this.DoorbuigingValidatie == null) return;


            this.DoorbuigingValidatie.LengteMM = this.LengteSchuin;
            this.DoorbuigingValidatie.CombinatieTypeBijkomend = BelastingCombinatieTypeEnum.Frequent;
            this.DoorbuigingValidatie.CombinatieTypeEind = BelastingCombinatieTypeEnum.QuasiBlijvend;
            this.DoorbuigingValidatie.FactorBijkomend = 1 / 250.0;
            this.DoorbuigingValidatie.FactorEind = 1 / 250.0;
            this.DoorbuigingValidatie.FactorZeeg = 0.0;


            var blijvend = this.DoorbuigingValidatie.CombinatieContexts.FirstOrDefault(c => c.CombinatieType == BelastingCombinatieTypeEnum.Blijvend);
            var quasiBlijvend = this.DoorbuigingValidatie.CombinatieContexts.FirstOrDefault(c => c.CombinatieType == BelastingCombinatieTypeEnum.QuasiBlijvend);
            var frequent = this.DoorbuigingValidatie.CombinatieContexts.FirstOrDefault(c => c.CombinatieType == BelastingCombinatieTypeEnum.Frequent);

            if (blijvend != null) blijvend.Lijnlast = this.Krachten.qG * FactorProjectieZToLocalZ;
            if (quasiBlijvend != null) quasiBlijvend.Lijnlast = this.Krachten.qEqp * FactorProjectieZToLocalZ;
            if (frequent != null) frequent.Lijnlast = this.Krachten.qEfr * FactorProjectieZToLocalZ;

            if (!this.DoorbuigingValidatie.BerekenEnValideer())
            {
                Console.WriteLine("Doorbuiging niet akkoord");
                foreach (var m in this.DoorbuigingValidatie.Meldingen)
                {
                    Console.WriteLine($"{m}");
                }
            }






            if (this.DoorbuigingContext == null)
                return;



            this.DoorbuigingContext.LengteMM = this.LengteSchuin;
            this.DoorbuigingContext.Lijnlast = this.Krachten.qEqp * this.FactorProjectieZToLocalZ;
            this.DoorbuigingContext.LijnlastG = this.Krachten.qG * this.FactorProjectieZToLocalZ;
            this.DoorbuigingContext.D = this.SchilDikte - this.WapeningSchil.ZRef;
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
                    return Math.Pow(LengteTotaal / LengteSchuin, 2);

                return Math.Pow(AantredeMaat / SchuineMaat, 2);
            }
        }





        [TableColumn("Hoogte", Order = 50, StringFormat = "0.## mm")]
        public double HoogteTotaal => SteekTrapExtensions.GetHoogteTotaal(this);





        public TandOplegging? TandOpleggingBovenzijde { get; set; } = null;
        public TandOplegging? TandOpleggingOnderzijde { get; set; } = null;

        public UitkragingContext DemoUitkraging { get; set; } = new();

        //public BendingResults DemoBuiging { get; set; } = new();
        public BendingResults BuigingBGT { get; set; } = new();

        public List<BendingResults> MomentCollectie { get; set; } = new();

        private KrachtenDemo _krachten;
        public KrachtenDemo Krachten
        {
            get => _krachten;
            set => SetProperty(ref _krachten, value);
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
        public BetonProfiel ProfielSchil
        {
            get => _profielSchil;
            set => SetNestedProperty(ref _profielSchil!, value);
        }



        private DwarskrachtWapContext _dwarskracht = new();
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
        public GrenswaardeSlankheidContext Slankheid { get; set; }

        /// <summary>
        /// Toets voor de buiging momentwapening van de schil
        /// </summary>
        public BendingResults MomentSchil { get; set; }

        public BendingResults MomentSchilToevallig { get; set; }




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




    }


    public class KrachtenMVT
    {
        public KrachtenMVT()
        {

        }

        public KrachtenMVT(double m = 0, double v = 0, double t = 0)
        {
            M = m;
            V = v;
            T = t;
        }

        public double M { get; set; }
        public double V { get; set; }
        public double T { get; set; }
    }


    public class KrachtenDemo
    {
        public double Mk { get; set; }
        public double MEd { get; set; }
        public double Mfreq { get; set; }
        public double Mqp { get; set; }
        public double Vqp { get; set; }
        public double Vk { get; set; }
        public double VEd { get; set; }
        public double Vfreq { get; set; }


        public double Mk1, Mk2, Mk3;
        public double Vk1, Vk2, Vk3;
        public double MomA, MomB, MomC;
        public double DwarskrachtA, DwarskrachtB, DwarskrachtC;
        //public double DwarskrachtUgtA, DwarskrachtUgtB;

        //public double FactorG { get; set; }
        //public double FactorQ { get; set; }
        public BelastingCombinatie MaatgevendeCombinatieFundamenteel { get; set; }

        public BelastingCombinatie MaatgevendeCombinatieFrequent;
        public BelastingCombinatie MaatgevendeCombinatieKarakteristiek { get; set; }


        // dit is straks niet meer nodig, bij toepassing ligger/raamwerk
        public double Gk { get; set; }
        public double EigenGewicht { get; set; }
        public double Afwerking { get; set; }
        public double Lijnlast_qk { get; set; }
        public double Puntlast_Qk { get; set; }
        public double L { get; set; }


        public double qG { get; set; }

        public double qEfr { get; set; }
        public double qEqp { get; set; }



        private string GetMomentTekst()
        {
            string result = "";
            result += $"M~BG1~ = 0,125 × {Gk:0.0} × {L:0.###}² = {Mk1:0.0} kNm, ";
            result += $"M~BG2~ = 0,125 × {Lijnlast_qk:0.0}×{L:0.###}² = {Mk2:0.0} kNm, ";
            result += $"M~BG3~ = 0,25 × {Puntlast_Qk:0.0}×{L:0.###} = {Mk3:0.0} kNm <br />";
            result += $"M~Ed~ = {MEd:0.0} kNm <br />";
            result += $"M~Ed,freq~ = {Mfreq:0.0} kNm";
            return result;

        }

        public MarkupString GetMomentMarkupString()
        {
            return MarkupHelper.ToMarkupString(GetMomentTekst());
        }


        public string GetGeometrieTekst()
        {
            return $"L~t~ = {L:0.### m}";
        }

        private string GetTekst_Gk()
        {
            if (Afwerking == 0)
                return $"{Gk:0.## kN/m¹}";
            else
                return $"{EigenGewicht:0.##} + {Afwerking:0.##} = {Gk:0.##} kN/m¹";
        }
        private string GetTekst_qk()
        {
            return $"{Lijnlast_qk:0.## kN/m¹}";
        }
        private string GetTekst_Qk()
        {
            return $"{Puntlast_Qk:0.## kN}";
        }


        private string GetBelastingTekst()
        {
            List<string> results = [];

            results.Add(GetTekst_Gk());
            results.Add(GetTekst_qk());
            results.Add(GetTekst_Qk());

            //results.Add($"q~k~ = {Qk:0.## kN/m¹}");
            //results.Add($"Q~k~ = {P:0.## kN}");

            return string.Join(", ", results);
        }

        public MarkupString GetGeometrieEnBelastingMarkupString()
        {
            return MarkupHelper.ToMarkupString(GetGeometrieTekst() + ", " + GetBelastingTekst());
        }

        public MarkupString GetGeometrieMarkupString()
        {
            return MarkupHelper.ToMarkupString(GetGeometrieTekst());
        }

        public MarkupString GetBelastingMarkupString()
        {
            return MarkupHelper.ToMarkupString(GetBelastingTekst());
        }

        public List<(string naam, MarkupString)> GetBelastingMarkupStrings()
        {
            List<(string naam, MarkupString)> results = [];

            results.Add(("permanent q~g,k~", MarkupHelper.ToMarkupString(GetTekst_Gk())));
            results.Add(("veranderlijk q~q,k~", MarkupHelper.ToMarkupString(GetTekst_qk())));
            results.Add(("veranderlijk F~q,k~", MarkupHelper.ToMarkupString(GetTekst_Qk())));

            return results;
        }

    }


    public class SteekTrapService
    {
        public KrachtenDemo BerekenSteektrap(SteekTrapEntity steekTrap)
        {
            Console.WriteLine(steekTrap.Merk + " [BerekenSteektrap] krachten worden berekend (statisch bepaald met vergeet-mij-nietjes)");
            KrachtenDemo returnItem = new();
            // belastingen
            double qG = steekTrap.GetPermanenteBelasting();
            var opgelegdeBelastingen = steekTrap.GetOpgelegdeBelasting();

            if (opgelegdeBelastingen == null)
                opgelegdeBelastingen = new() { Puntlast = 10, LijnlastRand = 10, Vlaklast = 10 };

            // momenten (kar)
            double l = steekTrap.GetLengteTotaal() / 1000;
            double a = 0.5 * l;

            //double m1 = steekTrap.GetMomentVgmn1(qG);

            // in het midden 
            var vergeetMijNietje1 = SteekTrapExtensions.GetVergeetMeNietje(SteekTrapExtensions.VergeetMeNietje.VrijVrijLijnlast, qG, l, a);
            var vergeetMijNietje2 = SteekTrapExtensions.GetVergeetMeNietje(SteekTrapExtensions.VergeetMeNietje.VrijVrijLijnlast, opgelegdeBelastingen.Value.Vlaklast, l, a);
            var vergeetMijNietje3 = SteekTrapExtensions.GetVergeetMeNietje(SteekTrapExtensions.VergeetMeNietje.VrijVrijPuntlast, opgelegdeBelastingen.Value.Puntlast, l, a);

            var momentaanFactoren = steekTrap.Belastingen.BelastingGevallen.FirstOrDefault(bg => bg.Type == BelastingGeval.BelastingGevalTypeEnum.Veranderlijk).MomentaanFactoren;



            var mk1 = vergeetMijNietje1.m;
            var mk2 = vergeetMijNietje2.m;
            var mk3 = vergeetMijNietje3.m;

            var mk = vergeetMijNietje1.m + Math.Max(vergeetMijNietje2.m, vergeetMijNietje3.m);
            mk = 0.0;

            var mfreq = 0.0;
            var mQp = 0.0;
            var vfreq = 0.0;
            var vQp = 0.0;

            var momA = 0.0;
            var momB = 0.0;
            var momC = 0.0;
            var dwaA = 0.0;
            var dwaB = 0.0;
            var dwaC = 0.0;

            var vk1 = qG * l / 2;
            var vk2 = opgelegdeBelastingen.Value.Vlaklast * l / 2;
            var vk3 = opgelegdeBelastingen.Value.Puntlast;
            var vk = vk1 + Math.Max(vk2, vk3);
            vk = 0.0;

            var md = 0.0;
            var vd = 0.0;
            var bcOrdered = steekTrap.Belastingen.BelastingCombinaties.OrderBy(c => c.Type).ToList();
            var lastType = bcOrdered[0].Type;
            foreach (var bc in bcOrdered)
            {
                if (bc.Type != lastType)
                {

                    momA = 0.0;
                    momB = 0.0;
                    momC = 0.0;
                    dwaA = 0.0;
                    dwaB = 0.0;
                    dwaC = 0.0;


                }
                var mom = 0.0;
                var dwa = 0.0;
                foreach (var item in bc.Items)
                {
                    if (item.Geval.Nr == 1)
                    {
                        mom += vergeetMijNietje1.m * item.FactorNetto;
                        dwa += vk1 * item.FactorNetto;

                    }
                    else if (item.Geval.Nr == 2)
                    {
                        mom += vergeetMijNietje2.m * item.FactorNetto;
                        dwa += vk2 * item.FactorNetto;
                    }
                    else if (item.Geval.Nr == 3)
                    {
                        mom += vergeetMijNietje3.m * item.FactorNetto;
                        dwa += vk3 * item.FactorNetto;
                    }
                }

                switch (bc.Type)
                {
                    case BelastingCombinatieTypeEnum.Fundamenteel_A:
                        if (mom > momA) momA = mom;
                        if (dwa > dwaA) dwaA = dwa;
                        if (mom >= md)
                        {
                            md = mom;
                            returnItem.MaatgevendeCombinatieFundamenteel = bc;
                        }
                        break;
                    case BelastingCombinatieTypeEnum.Fundamenteel_B:
                        if (mom > momB) momB = mom;
                        if (dwa > dwaB) dwaB = dwa;
                        if (mom >= md)
                        {
                            md = mom;
                            returnItem.MaatgevendeCombinatieFundamenteel = bc;
                        }
                        break;
                    case BelastingCombinatieTypeEnum.Karakteristiek:

                        if (mom >= mk)
                        {
                            mk = mom;
                            returnItem.MaatgevendeCombinatieKarakteristiek = bc;
                        }
                        break;
                    case BelastingCombinatieTypeEnum.Frequent:
                        if (mom > momC) momC = mom;
                        if (dwa > dwaC) dwaC = dwa;
                        if (mom >= mfreq)
                        {
                            mfreq = mom;

                            returnItem.MaatgevendeCombinatieFrequent = bc;
                        }
                        if (dwa >= vfreq)
                        {
                            vfreq = dwa;
                        }
                        break;
                    case BelastingCombinatieTypeEnum.QuasiBlijvend:
                        if (mom > momC) momC = mom;
                        if (dwa > dwaC) dwaC = dwa;
                        if (mom >= mQp)
                        {
                            mQp = mom;

                            returnItem.MaatgevendeCombinatieFrequent = bc;
                        }
                        if (dwa >= vQp)
                        {
                            vQp = dwa;
                        }
                        break;
                }



                if (dwa > vd)
                    vd = dwa;

                lastType = bc.Type;
            }



            returnItem.Mk = -mk;
            returnItem.MEd = -md;
            returnItem.Mfreq = -mfreq;
            returnItem.Mqp = -mQp;

            returnItem.Vk = vk;
            returnItem.VEd = vd;
            returnItem.Vfreq = vfreq;
            returnItem.Vqp = vQp;


            returnItem.Mk1 = -mk1;
            returnItem.Mk2 = -mk2;
            returnItem.Mk3 = -mk3;

            returnItem.Vk1 = vk1;
            returnItem.Vk2 = vk2;
            returnItem.Vk3 = vk3;

            returnItem.MomA = -momA;
            returnItem.MomB = -momB;
            returnItem.MomC = -momC;

            returnItem.DwarskrachtA = dwaA;
            returnItem.DwarskrachtB = dwaB;
            returnItem.DwarskrachtC = dwaC;


            returnItem.Gk = qG;
            returnItem.EigenGewicht = qG - steekTrap.BelastingAfwerking;
            returnItem.Afwerking = steekTrap.BelastingAfwerking;

            returnItem.Lijnlast_qk = opgelegdeBelastingen.Value.Vlaklast;
            returnItem.Puntlast_Qk = opgelegdeBelastingen.Value.Puntlast;
            returnItem.L = l;


            returnItem.qG = qG;
            returnItem.qEfr = qG + opgelegdeBelastingen.Value.Vlaklast * momentaanFactoren.Mom1;
            returnItem.qEqp = qG + opgelegdeBelastingen.Value.Vlaklast * momentaanFactoren.Mom2;

            steekTrap.UpdateSnedekrachten(returnItem.MEd, returnItem.Mfreq, returnItem.VEd);
            steekTrap.Krachten = returnItem;

            return returnItem;

        }
    }



}
