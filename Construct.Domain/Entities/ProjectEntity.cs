using CommonLibrary.Interfaces;
using CommonLibrary.Models;
using Eurocode.Belastingen;
using Eurocode.BetonConstructies;
using System.Text.Json.Serialization;

namespace Construct.Domain.Entities
{
    public class ProjectEntity
    {
        [JsonPropertyOrder(-1000)]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Project informatie + Grondslagen voor de berekening (Eurocode 0)
        /// </summary>
        [JsonPropertyOrder(-900)]
        public ProjectInfoEntity ProjectInfo { get; set; } = new();

        /// <summary>
        /// Assemblages zijn samengestelde onderdelen, zoals bijvoorbeeld een trap, kolom of bordes.
        /// </summary>
        [JsonPropertyOrder(100)]
        public List<AssemblageEntity> Assemblages { get; set; } = [];

        /// <summary>
        /// Documenten van het project
        /// </summary>
        [JsonPropertyOrder(200)]
        public List<DocumentEntity> Documenten { get; set; } = [new()
        {
            Author = "UsernameFromLoginIfOrUnknown",
            Title = "Berekening",
            Subtitle = "Sterkteberekening prefab trappen"
        }];

        /// <summary>
        /// Materialen in het project (beton, staal, hout).
        /// ✅ Serialiseert automatisch - ReferenceHandler deduplicaert
        /// </summary>
        [JsonPropertyOrder(300)]
        public Dictionary<Guid, BaseMateriaal> Materialen { get; set; } = [];

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

        /// <summary>
        /// Standaard voor gebruiksklasse
        /// Elementen krijgen hun eigen klasse (geen referentie) maar wel op basis van deze standaard
        /// </summary>
        public Eurocode.Belastingen.GebruiksklasseEnum? DefaultGebruiksklasse { get; set; } = GebruiksklasseEnum.C5_bijeenkomst_grote_menigtes;
        public DekkingContext DefaultDekking { get; set; } = new();



    }
}
