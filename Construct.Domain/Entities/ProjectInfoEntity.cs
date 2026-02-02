using Eurocode.Grondslagen;

namespace Construct.Domain.Entities
{
    public partial class ProjectInfoEntity
    {
        public ProjectInfoEntity() { }

        public override string ToString()
        {
            return $"{Nummer ?? "projectnr."} | {Naam ?? "naam"} | {Plaatsnaam?.ToUpper() ?? "PLAATS"}";
        }

        public string? Nummer { get; set; }
        public string? Naam { get; set; }
        public string? Plaatsnaam { get; set; }


        public GrondslagenContext Grondslagen { get; set; } = new()
        {
            NationaleBijlage = NationaleBijlageEnum.NL,
            Gevolgklasse = GevolgklasseEnum.CC2,
            OntwerpLevensduur = OntwerpLevensduurEnum.Vijftig,
        };

    }

}
