using Eurocode.BetonConstructies;
using Profielen.Parametrisch;
using System.Text.Json.Serialization;

namespace Construct.Domain.Entities.Parts
{
    /// <summary>
    /// Balk-onderdeel met middellijn, profiel en dekking.
    /// Ondersteunt beton, staal en andere materialen.
    /// Kan later uitgebreid worden met polyline voor gebogen balken.
    /// </summary>
    public class BeamPart : ConstructionPart
    {
        public override PartType Type => PartType.Beam;
        
        /// <summary>Middellijn van de balk (startpunt ? eindpunt)</summary>
        // Later: public Polyline3D? CenterLine { get; set; }
        public (double X, double Y, double Z) StartPoint { get; set; }
        public (double X, double Y, double Z) EndPoint { get; set; }
        
        /// <summary>Lengte van de balk in mm (berekend uit middellijn)</summary>
        [JsonIgnore]
        public double Lengte
        {
            get
            {
                var dx = EndPoint.X - StartPoint.X;
                var dy = EndPoint.Y - StartPoint.Y;
                var dz = EndPoint.Z - StartPoint.Z;
                return Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }
        }
        
        /// <summary>Dwarsdoorsnede profiel (breedte × hoogte)</summary>
        public ParametrischProfielContext Profiel { get; set; } = new(300, 600);
        
        /// <summary>
        /// Dekking voor betonbalken (verschilt van plaatdekking).
        /// Alleen relevant voor beton assemblages.
        /// </summary>
        public BetonDekkingContext? Dekking { get; set; }
        
        // Toekomst: wapeningschema voor balken (verschilt van plaatwapening)
        // public BalkWapeningContext? Wapening { get; set; }
        
        /// <summary>
        /// Helper property: Cast Material naar BetonContext voor betonconstructies.
        /// Returns null indien Material geen BetonContext is.
        /// </summary>
        [JsonIgnore]
        public BetonContext? Beton => Material as BetonContext;
    }
}
