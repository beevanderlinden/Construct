using CommonLibrary.Interfaces;
using CommonLibrary.Models;
using Eurocode.Belastingen;
using Eurocode.BetonConstructies;

namespace Construct.Domain.Entities
{
    public class ProjectEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Project informatie + Grondslagen voor de berekening (Eurocode 0)
        /// </summary>
        public ProjectInfoEntity ProjectInfo { get; set; } = new();

        /// <summary>
        /// Assemblages zijn samengestelde onderdelen, zoals bijvoorbeeld een trap, kolom of bordes.
        /// </summary>
        public List<AssemblageEntity> Assemblages { get; set; } = [];

        /// <summary>
        /// Documenten van het project
        /// </summary>
        public List<DocumentEntity> Documenten { get; set; } = [new()
        {
            Author = "UsernameFromLoginIfOrUnknown",
            Title = "Berekening",
            Subtitle = "Sterkteberekening prefab trappen"
        }];

        /// <summary>
        /// Materialen in het project.
        /// </summary>
        public Dictionary<Guid, BaseMateriaal> Materialen { get; } = [];

        public T VoegMateriaalToe<T>(T materiaal) where T : BaseMateriaal
        {
            materiaal.Id = Guid.NewGuid();
            Materialen[materiaal.Id] = materiaal;
            return materiaal;
        }

        public void VerwijderMateriaal(Guid id)
        {
            Materialen.Remove(id);
        }

        // Mogelijkheid voor Defaults in project
        //public List<BetonContext> Materialen { get; set; } = [new BetonContext("C45/55")];
        public List<GebruiksklasseEnum> Gebruiksklassen { get; set; } = [GebruiksklasseEnum.A_gemeenschappelijke_trappen];

        // TODO implementeer een JsonConverter voor IMateriaal 
        //public List<IMateriaal> Materialen { get; set; } = [new BetonContext("C45/55")];


    }





    //public class AssemblageNummer
    //{
    //    public string? Prefix { get; set; }
    //    public int Nummer
    //    {
    //        get
    //        {
    //            return StartIndex + VolgNummer;
    //        }
    //    }
    //    public int StartNummer { get; set; } = 1;
    //    public int StartIndex { get { return StartNummer - 1; } }
    //    public int VolgNummer { get; set; } = 1;
    //    public int? SubNummer { get; set; } = null;

    //    public string Merk
    //    {
    //        get
    //        {
    //            return $"{Prefix}{Nummer}{SubNummer}";
    //        }
    //    }


    //}


    //public class Trapvlucht
    //{
    //    public double OptredeMaat { get; set; }
    //    public double AantredeMaat { get; set; }
    //    public double OptredeAantal { get; set; }

    //    public double Wel { get; set; }


    //}

    //public class TrapBordes
    //{
    //    public double Dikte { get; set; }
    //    public double Breedte { get; set; }


    //}





}
