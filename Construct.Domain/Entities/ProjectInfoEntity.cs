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

        /// <summary>
        /// Minimale brandwerendheid in minuten voor alle vloeren in het project (REI). 
        /// Waarde 0 = geen eis.
        /// </summary>
        [JsonPropertyOrder(200)]
        public int MinimaleREI { get; set; } = 0;


        public FabrieksInstelling FabrieksInstelling { get; set; } = new()
        {
                FabrieksNaam = "MBS",
                DiamBasis = 8,
                DiamDetailWapening = 6,
                HohBovengrens = 150,
                HohOndergrensBasis = 100
        };

        /// <summary>
        /// Bepaalt hoe het programma omgaat met wapeningsinvoer bij herberekening (project-breed).
        /// <list type="bullet">
        /// <item><see cref="WapeningAfhandelingEnum.Gebruiker"/> – Geen automatische aanpassing; invoer gebruiker blijft altijd staan.</item>
        /// <item><see cref="WapeningAfhandelingEnum.AlleenVerhogen"/> – (default) Verhoog alleen als ingevoerde wapening onvoldoende is.</item>
        /// <item><see cref="WapeningAfhandelingEnum.Optimaliseer"/> – Herbereken altijd naar de economisch optimale wapening.</item>
        /// </list>
        /// Als de invoer leeg is, wordt bij alle instellingen automatisch de wapening bepaald.
        /// </summary>
        [JsonPropertyOrder(300)]
        public WapeningAfhandelingEnum WapeningAfhandeling { get; set; } = WapeningAfhandelingEnum.AlleenVerhogen;



    }

}
