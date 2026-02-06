using Eurocode.Grondslagen;
using System.Text.Json.Serialization;

namespace Construct.Domain.Entities
{
    public partial class ProjectInfoEntity
    {
        public ProjectInfoEntity() { }

        public override string ToString()
        {
            return $"{Nummer ?? "projectnr."} | {Naam ?? "naam"} | {Plaatsnaam?.ToUpper() ?? "PLAATS"}";
        }

        [JsonPropertyOrder(-1000)]
        public string? Nummer { get; set; }
        
        [JsonPropertyOrder(-900)]
        public string? Naam { get; set; }
        
        [JsonPropertyOrder(-800)]
        public string? Plaatsnaam { get; set; }


        [JsonPropertyOrder(100)]
        public GrondslagenContext Grondslagen { get; set; } = new()
        {
            NationaleBijlage = NationaleBijlageEnum.NL,
            Gevolgklasse = GevolgklasseEnum.CC2,
            OntwerpLevensduur = OntwerpLevensduurEnum.Vijftig,
        };

    }

}
