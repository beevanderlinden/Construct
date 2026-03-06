using System;
using System.Text.Json.Serialization;

namespace Construct.Domain.Entities.Parts
{
    /// <summary>
    /// Abstract basisklasse voor alle constructieonderdelen (plaat, balk, kolom).
    /// Generiek ontworpen voor beton, staal, hout, etc.
    /// GEEN recursie: SubParts worden beheerd door parent AssemblageEntity (1 niveau diep).
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$partType")]
    [JsonDerivedType(typeof(SlabPart), typeDiscriminator: "plate")]
    [JsonDerivedType(typeof(BeamPart), typeDiscriminator: "beam")]
    public abstract class ConstructionPart
    {
        /// <summary>Unieke identifier voor dit onderdeel</summary>
        public Guid Id { get; set; } = Guid.NewGuid();
        
        /// <summary>Naam van dit onderdeel (bijv. "hoofdplaat", "randbalk rechts")</summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Materiaal voor dit onderdeel (beton, staal, hout, etc.).
        /// Type afhankelijk van constructietype (BetonContext, StaalContext, etc.).
        /// </summary>
        public object? Material { get; set; }
        
        /// <summary>Type van dit onderdeel (Plate, Beam, Column, etc.)</summary>
        public abstract PartType Type { get; }
        
        /// <summary>
        /// Verwijzing naar parent assemblage (voor bidirectionele navigatie).
        /// Gebruikt voor toegang tot project eigenschappen en materiaal synchronisatie.
        /// </summary>
        [JsonIgnore]
        public AssemblageEntity? ParentAssemblage { get; set; }
    }
    
    /// <summary>
    /// Type classificatie voor constructieonderdelen
    /// </summary>
    public enum PartType
    {
        /// <summary>Plaatvormig element (vloer, wand, schil)</summary>
        Plate,
        
        /// <summary>Balkachtig element (ligger, balk, randverstijving)</summary>
        Beam,
        
        /// <summary>Kolomachtig element (kolom, paal)</summary>
        Column, // toekomstig
        
        /// <summary>Aangepast/samengesteld element</summary>
        Custom // toekomstig
    }
}
