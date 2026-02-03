//using Kaskon.Toolbox.PrefabModels;

//using Mechanica.LiggerSB;
using System.Text.Json.Serialization;

namespace Construct.Domain.Entities
{
    public class VerbindingAansluitendElement
    {
        /// <summary>
        /// Constructor voor JSON deserialisatie met parametermapping
        /// Dit zorgt ervoor dat alle properties correct uit JSON worden gemapt
        /// </summary>
        [JsonConstructor]
        public VerbindingAansluitendElement(
            bool gebruikEigenOpgave = false,
            double lengteEigenOpgave = 1200,
            double breedteEigenOpgave = 100,
            double hoogteEigenOpgave = 105,
            double randafstand = 150,
            bool gespiegeld = false,
            AssemblageEntity? father = null)
        {
            GebruikEigenOpgave = gebruikEigenOpgave;
            LengteEigenOpgave = lengteEigenOpgave;
            BreedteEigenOpgave = breedteEigenOpgave;
            HoogteEigenOpgave = hoogteEigenOpgave;
            Randafstand = randafstand;
            Gespiegeld = gespiegeld;
            Father = father ?? null!;
        }

        /// <summary>
        /// Normale constructor met father parameter (voor programmatisch gebruik)
        /// </summary>
        public VerbindingAansluitendElement(AssemblageEntity father)
        {
            Father = father;
        }

        private AssemblageEntity? _aansluitendElement;
        public AssemblageEntity? AansluitendElement
        {
            get => _aansluitendElement;
            set => _aansluitendElement = value;
        }

        public AssemblageEntity Father { get; set; } = null!;




        public bool GebruikEigenOpgave { get; set; } = false;
        public double LengteEigenOpgave { get; set; } = 1200;
        public double BreedteEigenOpgave { get; set; } = 100;
        public double HoogteEigenOpgave { get; set; } = 105;
        public double Randafstand { get; set; } = 150;
        public bool Gespiegeld { get; set; } = false;


        


        /// <summary>
        /// Geeft de start- en eindpositie van de verbinding aan.
        /// 
        /// </summary>
        public (double Start, double End) Pos
        {
            get
            {
                var a = Randafstand;
                var l = Lengte;
                var z = Father.Lengte;
                var p1 = a;
                var p2 = p1 + l;

                if (Gespiegeld)
                {
                    p2 = z - a;
                    p1 = p2 - l;
                }

                return (p1, p2);
            }
        }


        public (double Start, double End) PosM
        {
            get
            {
                return (Pos.Start * 1e-3, Pos.End * 1e-3);
            }
        }


        public double Lengte
        {
            get
            {
                if (GebruikEigenOpgave) return LengteEigenOpgave;
                double lengte = 1200;
                if (_aansluitendElement is SteekTrapEntity steekTrap)
                {
                    lengte = steekTrap.Breedte; // de lengte van de aansluiting is de breedte van de trap
                }
                return lengte;
            }
        }
        public double Breedte
        {
            get
            {
                if (GebruikEigenOpgave) return BreedteEigenOpgave;

                double breedte = 100;
                if (_aansluitendElement is SteekTrapEntity steekTrap)
                {
                    breedte = steekTrap.TandOpleggingBovenzijde?.TandLengte ?? 100; // de breedte van de aansluiting is de tandlengte van de trap
                }
                return breedte;
            }
        }
           
        public double Hoogte
        {
            get
            {
                if (GebruikEigenOpgave) return HoogteEigenOpgave;
                double hoogte = 105;
                if (_aansluitendElement is SteekTrapEntity steekTrap)
                {
                    var oplegging = steekTrap.TandOpleggingBovenzijde ?? steekTrap.TandOpleggingOnderzijde ?? null;
                    if (oplegging == null) return hoogte;

                    hoogte = oplegging.TandHoogte + oplegging.DikteOplegmateriaal;
                }
                return hoogte;
            }
        }

        public (double G, double Q) Reacties
        {
            get
            {
                if (AansluitendElement == null) return (0, 0);
                else if (AansluitendElement is SteekTrapEntity steektrap)
                {
                    return (steektrap.ReactieG, steektrap.ReactieQ);
                }
                else return (0, 0);
            }
        }
        

    }
}
