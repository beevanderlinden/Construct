using Eurocode.BetonConstructies;
using Profielen.Parametrisch;
using System.Text.Json.Serialization;

namespace Construct.Domain.Entities.Parts
{
    /// <summary>
    /// Plaat-onderdeel met dikte, dekking en wapening.
    /// Ondersteunt beton, staal en andere materialen.
    /// Kan later uitgebreid worden met polygoon geometrie.
    /// </summary>
    public class SlabPart : ConstructionPart
    {
        public override PartType Type => PartType.Plate;
        
        /// <summary>Plaatdikte in mm</summary>
        public double Dikte { get; set; } = 200;
        
        /// <summary>
        /// Dekking boven/onder voor betonplaat.
        /// BELANGRIJK: Deze wordt gedeeld met parent assemblage indien MainPart!
        /// Alleen relevant voor beton assemblages.
        /// </summary>
        public DekkingContext? PlaatDekking { get; set; }
        
        /// <summary>
        /// Plaatwapening (boven/onder met basis + verdeelwapening).
        /// BELANGRIJK: Deze wordt gedeeld met parent assemblage indien MainPart!
        /// Alleen relevant voor beton assemblages.
        /// </summary>
        public PlaatWapening? PlaatWapening { get; set; }
        
        /// <summary>Polygoon geometrie (toekomstig)</summary>
        // public Polygon2D? Geometrie { get; set; }
        
        /// <summary>
        /// Profiel representatie voor berekeningen (B=1000mm conventie voor platen)
        /// </summary>
        [JsonIgnore]
        public ParametrischProfielContext Profiel => new(1000, Dikte);
        
        /// <summary>
        /// Helper property: Cast Material naar BetonContext voor betonconstructies.
        /// Returns null indien Material geen BetonContext is.
        /// </summary>
        [JsonIgnore]
        public BetonContext? Beton => Material as BetonContext;
    }
}
