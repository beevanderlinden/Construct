using CommonLibrary;
using Eurocode.BetonConstructies;
//using Kaskon.Toolbox.PrefabModels;
using Profielen.Parametrisch;

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



}
