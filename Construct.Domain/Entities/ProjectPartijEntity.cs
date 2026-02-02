namespace Construct.Domain.Entities
{
    public class ProjectPartijEntity
    {
        public string? Naam1 { get; set; } = "Nader te bepalen";
        public string? Naam2 { get; set; } = "";
        public string? Referentie { get; set; } = "";
        public PartijRolEnum Rollen { get; set; } = PartijRolEnum.Onbekend;

        public ProjectPartijEntity()
        {

        }



        public ProjectPartijEntity(string naam1, string naam2, string referentie, PartijRolEnum rollen)
        {
            Naam1 = naam1;
            Naam2 = naam2;
            Referentie = referentie;
            Rollen = rollen;
        }


        [Flags]
        public enum PartijRolEnum
        {
            Onbekend = 1,
            Aannemer = 2,
            Oprachtgever = 4,
            Betonfabriek = 8,
            Hoofdconstructeur = 16,
            Deelconstructeur = 32,
            Architect = 64,
            Staalfabriek = 128,
            Gemeente = 256,
            Overig = 512,

        }

    }





}
