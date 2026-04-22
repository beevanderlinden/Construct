using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Construct.Domain.Entities
{
    /// <summary>
    /// Richting van een strook in het bovenaanzicht van de plaat.
    /// </summary>
    public enum PlaatStrookRichting
    {
        /// <summary>Strook loopt van Links naar Rechts (spanning langs de Lengte-as, x-richting).</summary>
        [Display(Name = "Langs lengte-as (Links → Rechts)", ShortName = "→")]
        LangsLengte,

        /// <summary>Strook loopt van Boven naar Onder (spanning langs de Breedte-as, y-richting).</summary>
        [Display(Name = "Langs breedte-as (Boven → Onder)", ShortName = "↓")]
        LangsBreedte,
    }

    /// <summary>
    /// Positionering van een berekeningsstrook in het 2D bovenaanzicht van een plaat.
    /// Bevat geometrie (positie, begin/eind, breedte) én het berekeningsmodel (StrookEntity).
    /// Wordt altijd afgeleid uit <see cref="PlaatEntity.Opleggingen"/> — niet geserialiseerd.
    /// </summary>
    public class PlaatStrook
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>Weergavenaam, bijv. "S1 (L→R)".</summary>
        public string Naam { get; set; } = "strook";

        /// <summary>Richting van de strook in het bovenaanzicht.</summary>
        public PlaatStrookRichting Richting { get; set; }

        /// <summary>
        /// Positie langs de dwars-as (mm).
        /// LangsLengte-strook: y-positie (0 .. Breedte).
        /// LangsBreedte-strook: x-positie (0 .. Lengte).
        /// </summary>
        public double PositieDwars { get; set; }

        /// <summary>
        /// Strookbreedte voor belastingbepaling (invloedsbreedte) in mm.
        /// </summary>
        public double InvloedsBreedteMm { get; set; } = 1000;

        /// <summary>
        /// Beginpositie langs de spanrichting, gemeten vanuit de oorsprong van de plaat (mm).
        /// LangsLengte: x-coördinaat. LangsBreedte: y-coördinaat.
        /// </summary>
        public double StartMm { get; set; }

        /// <summary>
        /// Eindpositie langs de spanrichting (mm).
        /// </summary>
        public double EindMm { get; set; }

        /// <summary>Overspanning = EindMm − StartMm (mm).</summary>
        [JsonIgnore]
        public double Overspanning => EindMm - StartMm;

        /// <summary>Opleggingsconditie aan het begin van de strook (koppelt aan SBLigger.StartSupport).</summary>
        public PlaatOpleggingConditie ConditieStart { get; set; } = PlaatOpleggingConditie.VrijInRotatie;

        /// <summary>Opleggingsconditie aan het einde van de strook (koppelt aan SBLigger.EndSupport).</summary>
        public PlaatOpleggingConditie ConditieEind { get; set; } = PlaatOpleggingConditie.VrijInRotatie;

        /// <summary>
        /// Het berekeningsmodel voor deze strook (ligger + wapening + toetsen).
        /// </summary>
        public StrookEntity Strook { get; set; } = new();

        // ------------------------------------------------------------------
        //  Afgeleide SVG-coördinaten (gemak bij tekenen)
        // ------------------------------------------------------------------

        /// <summary>SVG x1 van de hartlijn.</summary>
        [JsonIgnore]
        public double SvgX1 => Richting == PlaatStrookRichting.LangsLengte ? StartMm     : PositieDwars;

        /// <summary>SVG y1 van de hartlijn.</summary>
        [JsonIgnore]
        public double SvgY1 => Richting == PlaatStrookRichting.LangsLengte ? PositieDwars : StartMm;

        /// <summary>SVG x2 van de hartlijn.</summary>
        [JsonIgnore]
        public double SvgX2 => Richting == PlaatStrookRichting.LangsLengte ? EindMm       : PositieDwars;

        /// <summary>SVG y2 van de hartlijn.</summary>
        [JsonIgnore]
        public double SvgY2 => Richting == PlaatStrookRichting.LangsLengte ? PositieDwars : EindMm;

        /// <summary>SVG middelpunt x (voor tekstlabel).</summary>
        [JsonIgnore]
        public double SvgMidX => (SvgX1 + SvgX2) / 2;

        /// <summary>SVG middelpunt y (voor tekstlabel).</summary>
        [JsonIgnore]
        public double SvgMidY => (SvgY1 + SvgY2) / 2;

        // ------------------------------------------------------------------
        //  Factory
        // ------------------------------------------------------------------

        /// <summary>
        /// Maak een nieuwe <see cref="PlaatStrook"/> aan met naam en geometrie.
        /// Beam-schematisering (steunpunten, lengte, belastingen) wordt ingesteld
        /// door <c>PlaatEntity.BerekenStroken()</c> in Stap 2.
        /// </summary>
        public static PlaatStrook Maak(
            string naam,
            PlaatStrookRichting richting,
            double positieDwars,
            double startMm,
            double eindMm,
            double invloedsBreedteMm,
            PlaatOpleggingConditie conditieStart,
            PlaatOpleggingConditie conditieEind)
        {
            return new PlaatStrook
            {
                Naam              = naam,
                Richting          = richting,
                PositieDwars      = positieDwars,
                StartMm           = startMm,
                EindMm            = eindMm,
                InvloedsBreedteMm = invloedsBreedteMm,
                ConditieStart     = conditieStart,
                ConditieEind      = conditieEind,
                Strook            = new StrookEntity { Naam = naam },
            };
        }
    }
}
