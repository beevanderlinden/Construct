using CommonLibrary;
using Eurocode.Belastingen;
using Eurocode.BetonConstructies;
using Profielen.Beton;
using Profielen.Parametrisch;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Construct.Domain.Entities
{
    public class TandOplegging : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



        public void Bijwerken()
        {
            var beton = this.Father.Materiaal as BetonContext;

            this.Oplegging.OplegLengteNettoAanwezig = TandLengte - VoegBreedte;
            LengteOplegmateriaal = TandLengte / 2;


            if (Father is SteekTrapEntity steektrap)
            {
                this._oplegReactie = steektrap.Krachten.VEd;
            }

            _snedekrachtenTand = new(my: 1, vz: _oplegReactie);
            List<double> afstanden = [0.25 * TandLengte, 0.25 * TandHoogte, 0.5 * LengteOplegmateriaal];
            _afstandVoorGedrongenLigger = afstanden.Min();



            this._buigingTand = new BendingResults(beton ?? new(), this.ProfielTand, this.WapeningAlgemeen, this.SnedekrachtenTand)
            {
                PosLabel = "tand",
                Name = "Tand",
                IsGedrongenLigger = this.TandGedrongen,
                LengteMaatBijGedrongenLiggerInMM = _afstandVoorGedrongenLigger
            };

            // nu weten we de z van de tand
            this._interneHefboomsArmTand = _buigingTand.Z;

            // en dus de arm (voor aandeel vanuit horizontale kracht) 
            this.ArmVoorTandHorizontaal = _interneHefboomsArmTand + TandHoogte + DikteOplegmateriaal - NuttigeHoogteTand;

            // en dan is MyEd bijgewerkt, maar moeten we nog de snedekrachten bijwerken
            this._snedekrachtenTand.My = this.MomentVoorTand;
            this._snedekrachtenTand.Nx = this.ReactieHorizontaal; // sigma,cp = NEd / Ac

            // en de controle dwarskracht erbij zetten
            this._dwarskrachtTand = new(beton ?? new(), this.ProfielTand, this.SnedekrachtenTand)
            {
                Theta = 21.8, // NB. hoek heeft geen invloed op VRd,c
                AsLangs = this.WapeningAlgemeen.As, // percentage langswapening heeft lichte invloed op VRd,c
                NutHoogte = this.NuttigeHoogteTand, // nuttige hoogte dient te worden doorgegeven.

            };




        }

        public AssemblageEntity Father { get; set; }
        public double TandHoogte { get; set; } = 100;

        public double VoegBreedte { get; set; } = 10;

        private double _interneHefboomsArmTand;
        private double _afstandVoorGedrongenLigger;

        private double _oplegReactie;
        public double OplegReactie
        {
            get => _oplegReactie;
            set
            {
                if (_oplegReactie != value)
                {
                    _oplegReactie = value;
                    OnPropertyChanged();
                }
            }
        }

        //{
        //    get
        //    {
        //        if (Father is SteekTrapEntity steektrap)
        //        {
        //            return steektrap.GetKrachten().VEd;
        //        }
        //        return 0;
        //    }
        //}


        public List<BaseEurocodeContext> GetToetsen()
        {
            List<BaseEurocodeContext> toetsen = [DwarskrachtTand, BuigingTand, BuigingHals, Oplegging];

            return toetsen;

        }

        private DwarskrachtWapContext _dwarskrachtTand;

        public DwarskrachtWapContext DwarskrachtTand
        {
            get => _dwarskrachtTand;
            set
            {
                if (_dwarskrachtTand != value)
                {
                    _dwarskrachtTand = value;
                    OnPropertyChanged();
                }
            }
        }


        public double PercentageHorizontaleBelasting { get; set; } = 0.4;

        /// <summary>
        /// Geeft aan of het een ondertand is (dragend element). 
        /// Als het geen ondertand is dan is het het boventand (ondersteund element).
        /// </summary>
        public bool IsOndertand { get; set; } = false;
        public bool IsBoventand => !IsOndertand;

        public double WerkendeBreedte { get; set; } = 1000;

        public double TandLengte { get; set; } = 100;

        public double HalsDikte { get; set; } = 100;
        public double DekkingAlgemeen { get; set; } = 30;
        public double StaafDiameterAlgemeen { get; set; } = 8;
        public double DikteOplegmateriaal { get; set; } = 10;
        public double LengteOplegmateriaal { get; set; } = 50;
        public bool MetHals { get; set; } = true;
        public double ArmVoorHals
        {
            get
            {
                return (HalsDikte + TandLengte) / 2.0;
            }
        }


        public double LengteAb
        {
            get
            {
                return 0.5 * TandLengte;
            }
        }

        public double KleinsteWaardeVoorArm
        {
            get
            {
                List<double> waarden = [
                    LengteAb * 0.5,
                    TandLengte * 0.25,
                    TandHoogte * 0.25
                ];

                return waarden.Min();
            }
        }

        public double ArmVoorTand
        {
            get
            {
                return ((TandLengte + 20) / 2.0 + KleinsteWaardeVoorArm); // toevoeging 20mm komt uit Excel.
            }
        }

        public double OverspanningVoorTand
        {
            get
            {
                return 2 * ArmVoorTand;
            }
        }

        public double OverspanningVoorTandGedeeldDoorHoogte
        {
            get
            {
                return OverspanningVoorTand / TandHoogte;
            }
        }

        public bool TandGedrongen
        {
            get
            {
                return (OverspanningVoorTandGedeeldDoorHoogte < 3);
            }
        }


        public double ArmVoorTandHorizontaal { get; set; } = 0;
        //{
        //get
        //{
        //    // = z + hc + dikteoplegmateriaal - d
        //    // zie paarse boekje 3.1.1
        //    return _buigingTand.Z + TandHoogte + DikteOplegmateriaal - NuttigeHoogteTand;
        //}
        //}



        public double MomentVoorHals
        {
            get
            {
                return OplegReactie * ArmVoorHals * 0.001;
            }
        }

        public double MomentVoorTand
        {
            get
            {
                return (Math.Abs(OplegReactie) * ArmVoorTand * 0.001 + MomentVoorTandUitHorizontaleBelasting) * (IsBoventand? -1 : 1) ;
            }
        }

        public double ReactieHorizontaal
        {
            get
            {
                return Math.Abs(OplegReactie) * PercentageHorizontaleBelasting;
            }
        }

        public double MomentVoorTandUitHorizontaleBelasting
        {
            get
            {
                return ReactieHorizontaal * ArmVoorTandHorizontaal * 0.001;
            }
        }




        public double NuttigeHoogteHals
        {
            get
            {
                return HalsDikte - DekkingAlgemeen - StaafDiameterAlgemeen * 0.5;
            }
        }

        public double NuttigeHoogteTand
        {
            get
            {
                return TandHoogte - DekkingAlgemeen - StaafDiameterAlgemeen * 0.5;
            }
        }

        // Voor beton zie father.Beton
        // Voor dekking zie father.Dekking

        public BetonProfiel ProfielHals
        {
            get
            {
                return new BetonProfiel(WerkendeBreedte, HalsDikte);
            }
        }

        public BetonProfiel ProfielTand
        {
            get
            {
                return new BetonProfiel(WerkendeBreedte, TandHoogte);
            }
        }




        public WapeningContext WapeningAlgemeen { get; set; } = new WapeningContext();
        public SectionForces SnedekrachtenHals
        {
            get
            {
                return new SectionForces(my: MomentVoorHals);
            }
        }


        private SectionForces _snedekrachtenTand;

        public SectionForces SnedekrachtenTand
        {
            get => _snedekrachtenTand;
            set
            {
                if (_snedekrachtenTand != value)
                {
                    _snedekrachtenTand = value;
                }
            }
        }

        public string ConclusieOplegging
        {
            get
            {
                if (Oplegging.IsValidated)
                {
                    return $"Aanwezig opleglengte ({Oplegging.OplegLengteNettoAanwezig:0} mm) is groter dan nominale opleglengte ({Oplegging.OplegLengteNominaal:0} mm), akkoord";
                }
                else
                {
                    return $"**Oplegging niet akkoord** {Oplegging.Meldingen.FirstOrDefault()}";
                }
            }
        }


        //public bool HalsGedrongen
        //{
        //    get
        //    {
        //        Eurocode.BetonConstructies.BuigingContext buiging = new();

        //    }
        //}

        public double FactorOphangKracht { get; set; } = 2;

        public double OphangKracht
        {
            get
            {
                return FactorOphangKracht * Math.Abs(OplegReactie);
            }
        }
        public double OphangWapeningHalsBenodigd
        {
            get
            {
                var beton = this.Father.Materiaal as BetonContext;
                return OphangKracht * 1000.0 / (beton?.BetonStaal.Fyd ?? 435);
            }
        }

        public double TotaleWapeningHalsBenodigd
        {
            get
            {
                return BuigingHals.AsRequired + OphangWapeningHalsBenodigd;
            }
        }

        public double TotaleWapeningBenodigd
        {
            get
            {
                return OphangWapeningHalsBenodigd + BuigingTand?.AsRequired ?? 0;
            }
        }


        public BendingResults BuigingHals
        {
            get
            {
                var beton = this.Father.Materiaal as BetonContext;
                return new BendingResults(beton ?? new(), this.ProfielHals, this.WapeningAlgemeen, this.SnedekrachtenHals) { PosLabel="hals", Name = "Hals", IsGedrongenLigger = !true, LengteMaatBijGedrongenLiggerInMM = 2 * ArmVoorHals };
            }
        }


        private BendingResults _buigingTand;
        public BendingResults BuigingTand
        {
            get => _buigingTand;
            set
            {
                if (_buigingTand != value)
                {
                    _buigingTand = value;
                }
            }
            //get
            //{
            //    return new BendingResults(this.Father.Beton, this.ProfielTand, this.WapeningAlgemeen, this.SnedekrachtenTand) { Name = "Tand", IsGedrongenLigger = true, LengteMaatBijGedrongenLiggerInMM = 2 * ArmVoorTand };
            //}
        }

        public double VoegX { get; set; } = 10;
        public double MinimaleTandLengte { get { return Math.Floor(Oplegging.OplegLengteNominaal) + VoegX; } }

        public OpleggingContext Oplegging { get; set; } = new();


        public bool OplegLengteAkkoord
        {
            get
            {
                return Oplegging?.GetOplegLengteNominmaal() <= TandLengte - VoegBreedte;
            }
        }


        [JsonConstructor]
        public TandOplegging(AssemblageEntity father, OpleggingContext oplegging)
        {
            Father = father;
            Initialize(oplegging);
        }

        public void Initialize(OpleggingContext context)
        {
            if (Father != null && Father is SteekTrapEntity steektrap)
            {
                // Stel de live delegate in
                var beton = Father.Materiaal as BetonContext;

                Oplegging.BerekenOplegReactieRekenwaarde = () => steektrap.Krachten.VEd;
                Oplegging.BerekenLengteOndersteundeElement = () => steektrap.LengteTotaal;
                Oplegging.BetonOndersteundeElement = beton ?? new();



                Oplegging.OplegLengteNettoAanwezig = steektrap.TandOpleggingBovenzijde?.TandLengte ?? 50; // todo ONDER/BOVEN mogelijk maken.
                Oplegging.OplegBreedteNetto = context?.OplegBreedteNetto ?? 1000;
                Oplegging.DetailleringWapening = context?.DetailleringWapening ?? OpleggingContext.DetailleringWapeningEnum.VerticaleHaarspelden;
                Oplegging.DrogeVerbinding = context?.DrogeVerbinding ?? false;
                Oplegging.OpleggingElementType = context?.OpleggingElementType ?? OpleggingContext.OpleggingElementTypeEnum.AfzonderlijkElement;
                Oplegging.OplegType = context?.OplegType ?? OplegTypeEnum.LIJNVORMIG;
                Oplegging.OplegMateriaal = context?.OplegMateriaal ?? OplegMateriaalEnum.IHWG_BETON;
                Oplegging.BetonsterkteklasseDragendeElement = context?.BetonsterkteklasseDragendeElement ?? BetonsterkteklasseEnum.C20_25;

                Oplegging.Update();
            }

        }




    }
}
