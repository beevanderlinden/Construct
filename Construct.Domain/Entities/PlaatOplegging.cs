using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Construct.Domain.Entities
{
    /// <summary>
    /// Geeft aan welke rand van de plaat het betreft.
    /// Links/Rechts lopen langs de Breedte-as; Boven/Onder lopen langs de Lengte-as.
    /// </summary>
    public enum PlaatRand
    {
        [Display(Name = "Links",  ShortName = "L")] Links,
        [Display(Name = "Rechts", ShortName = "R")] Rechts,
        [Display(Name = "Boven",  ShortName = "B")] Boven,
        [Display(Name = "Onder",  ShortName = "O")] Onder,
    }

    /// <summary>
    /// Opleggingsconditie in de randnormaalrichting.
    /// </summary>
    public enum PlaatOpleggingConditie
    {
        [Display(Name = "Vrij in rotatie", ShortName = "Vrij")]
        VrijInRotatie,

        [Display(Name = "Ingeklemd", ShortName = "Ing.")]
        Ingeklemd,
    }

    /// <summary>
    /// Basisklasse voor een oplegging op een plaat.
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(PlaatRandOplegging), "rand")]
    [JsonDerivedType(typeof(PlaatPuntOplegging), "punt")]
    public abstract class PlaatOplegging
    {
        /// <summary>Rand waarop de oplegging is aangebracht.</summary>
        public PlaatRand Rand { get; set; }

        /// <summary>Opleggingsconditie: vrij in rotatie of ingeklemd.</summary>
        public PlaatOpleggingConditie Conditie { get; set; } = PlaatOpleggingConditie.VrijInRotatie;
    }

    /// <summary>
    /// Volledige randoplegging langs de hele rand.
    /// </summary>
    public class PlaatRandOplegging : PlaatOplegging { }

    /// <summary>
    /// Puntvormige oplegging op een rand, met positie en breedte.
    /// </summary>
    public class PlaatPuntOplegging : PlaatOplegging
    {
        /// <summary>
        /// Positie gemeten vanaf het startpunt van de rand (mm).
        /// Voor Links/Rechts: positie langs de Breedte-as (0 .. Breedte).
        /// Voor Boven/Onder: positie langs de Lengte-as (0 .. Lengte).
        /// </summary>
        public double PositieOpRand { get; set; }

        /// <summary>Breedte van de steun in mm.</summary>
        public double Breedte { get; set; } = 200;
    }

    /// <summary>
    /// Beschikbare opleggingspresets voor een rechthoekige plaat.
    /// </summary>
    public enum PlaatOpleggingPreset
    {
        [Display(Name = "Geen oplegging", ShortName = "Geen")]
        Geen,

        [Display(Name = "Vrij opgelegde plaat ↔",   ShortName = "Vrij 2 randen")]
        VrijOpgelegd2Randen,

        [Display(Name = "Vrij opgelegde plaat ↕", ShortName = "Vrij breedte")]
        VrijOpgelegdBreedte,

        [Display(Name = "Uitkraging — ingeklemd boven",                 ShortName = "Uitkraging")]
        Uitkraging1Rand,

        [Display(Name = "4 punten — 2× links + 2× rechts",             ShortName = "4 pt. 2×2")]
        VierPunten2x2,

        [Display(Name = "4 punten — 1× links, 1× rechts, 2× boven",    ShortName = "4 pt. 1+1+2")]
        VierPunten1x1x2,

        [Display(Name = "Uitkraging 2 punten — 2× ingeklemd boven (1000 mm)", ShortName = "Uitkraging 2 pt.")]
        UitkragingTweePunten,
    }
}
