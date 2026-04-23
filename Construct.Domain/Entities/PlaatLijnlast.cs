using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Eurocode.Belastingen;

namespace Construct.Domain.Entities
{
    /// <summary>
    /// Een lijnlast met een startpunt S en eindpunt E in 2D plaatvlak-coördinaten (mm),
    /// een lineair variërende magnitude in kN/m en een koppeling aan een belastinggeval.
    /// Negatieve magnitudes zijn neerwaarts; positieve zijn opwaarts.
    /// MagnitudeA en MagnitudeB mogen niet van teken verschillen (wel 0 zijn).
    /// </summary>
    public class PlaatLijnlast
    {
        /// <summary>Omschrijving / naam van de lijnlast, bv. "gevelwandlast".</summary>
        public string Naam { get; set; } = string.Empty;
        public string Omschrijving { get; set; } = string.Empty;

        /// <summary>
        /// Startpunt S in het plaatvlak (mm). Coördinaten: lx = 0..Lengte, by = 0..Breedte.
        /// </summary>
        public PlaatPunt S { get; set; } = new();

        /// <summary>
        /// Eindpunt E in het plaatvlak (mm). Coördinaten: lx = 0..Lengte, by = 0..Breedte.
        /// </summary>
        public PlaatPunt E { get; set; } = new();

        /// <summary>
        /// Beginafstand langs de rand (mm), gemeten vanaf het startpunt van de rand.
        /// Alleen relevant als de lijnlast via <see cref="PlaatRandLijnlast"/> aan een rand gekoppeld is.
        /// </summary>
        public double A { get; set; }

        /// <summary>
        /// Eindafstand gemeten vanaf het eindpunt van de rand (mm).
        /// 0 = lijnlast loopt door tot het einde van de rand.
        /// Voor Links/Rechts: gemeten vanaf by = Breedte.
        /// Voor Boven/Onder: gemeten vanaf lx = Lengte.
        /// </summary>
        public double B { get; set; } = 0;

        /// <summary>Magnitude aan het startpunt in kN/m. Negatief = neerwaarts.</summary>
        public double MagnitudeA { get; set; } = -5.0;

        /// <summary>Magnitude aan het eindpunt in kN/m. Negatief = neerwaarts.</summary>
        public double MagnitudeB { get; set; } = -5.0;

        /// <summary>Belastinggeval waaraan deze lijnlast is gekoppeld (bv. BG1 of BG2).</summary>
        [JsonIgnore]
        public BelastingGeval? BelastingGeval { get; set; }

        /// <summary>
        /// Nr van het belastinggeval, voor serialisatie.
        /// Wordt via <see cref="PlaatEntity.Bijwerken"/> hersteld.
        /// </summary>
        public int BelastingGevalNr { get; set; } = 1;

        // ----------------------------------------------------------------
        //  Computed helpers
        // ----------------------------------------------------------------

        /// <summary>True als de last neerwaarts werkt (één of beide magnitudes negatief).</summary>
        [JsonIgnore]
        public bool IsNeerwaarts => MagnitudeA < 0 || MagnitudeB < 0;

        /// <summary>Grootste absolute magnitude van de twee eindpunten.</summary>
        [JsonIgnore]
        public double MaxAbsMagnitude => Math.Max(Math.Abs(MagnitudeA), Math.Abs(MagnitudeB));

        /// <summary>
        /// Valideert dat MagnitudeA en MagnitudeB niet van teken verschillen.
        /// Geeft een lege string terug als geldig.
        /// </summary>
        public string? ValideerMagnitudes()
        {
            int sA = Math.Sign(MagnitudeA);
            int sB = Math.Sign(MagnitudeB);
            if (sA != 0 && sB != 0 && sA != sB)
                return "MagnitudeA en MagnitudeB mogen niet van teken verschillen.";
            return null;
        }
    }

    /// <summary>
    /// Lijnlast die aan een specifieke rand van de plaat is gekoppeld.
    /// Start- en eindpunt worden afgeleid van <see cref="Rand"/>, <see cref="PlaatLijnlast.A"/> en <see cref="PlaatLijnlast.B"/>.
    /// </summary>
    public class PlaatRandLijnlast : PlaatLijnlast
    {
        /// <summary>Rand waarop de lijnlast is aangebracht.</summary>
        public PlaatRand Rand { get; set; }

        /// <summary>
        /// Verschuiving haaks op de rand (mm). Positief = naar het midden van de plaat.
        /// Links:  lx += Offset.  Rechts: lx -= Offset.
        /// Boven:  by += Offset.  Onder:  by -= Offset.
        /// </summary>
        public double Offset { get; set; } = 0;

        /// <summary>
        /// Berekent start- en eindpunt S/E op basis van Rand, A, B, Offset en de plaatafmetingen.
        /// Roep aan na het wijzigen van A, B, Offset, Rand of plaatafmetingen.
        /// </summary>
        public void BerekenPunten(double lengte, double breedte)
        {
            (S, E) = Rand switch
            {
                // Links  (lx=0): positie langs Breedte-as; Offset verschuift lx naar rechts
                PlaatRand.Links  => (new PlaatPunt(0 + Offset,       A),           new PlaatPunt(0 + Offset,       breedte - B)),

                // Rechts (lx=Lengte): positie langs Breedte-as; Offset verschuift lx naar links
                PlaatRand.Rechts => (new PlaatPunt(lengte - Offset,  A),           new PlaatPunt(lengte - Offset,  breedte - B)),

                // Boven  (by=0): positie langs Lengte-as; Offset verschuift by naar beneden
                PlaatRand.Boven  => (new PlaatPunt(A,                0 + Offset),  new PlaatPunt(lengte - B,       0 + Offset)),

                // Onder  (by=Breedte): positie langs Lengte-as; Offset verschuift by naar boven
                PlaatRand.Onder  => (new PlaatPunt(A,                breedte - Offset), new PlaatPunt(lengte - B,  breedte - Offset)),

                _ => (new PlaatPunt(A, 0), new PlaatPunt(lengte - B, 0)),
            };
        }

        /// <summary>
        /// Geeft de maximale positie langs de rand terug (Breedte voor Links/Rechts, Lengte voor Boven/Onder).
        /// </summary>
        public double MaxPositieOpRand(double lengte, double breedte) => Rand switch
        {
            PlaatRand.Links or PlaatRand.Rechts => breedte,
            _                                    => lengte,
        };
    }

    /// <summary>
    /// Een 2D punt in het plaatvlak (mm).
    /// lx = positie langs de Lengte-as (0..Lengte).
    /// by = positie langs de Breedte-as (0..Breedte).
    /// </summary>
    public class PlaatPunt
    {
        public double Lx { get; set; }
        public double By { get; set; }

        public PlaatPunt() { }
        public PlaatPunt(double lx, double by) { Lx = lx; By = by; }

        public void Deconstruct(out double lx, out double by) { lx = Lx; by = By; }
    }
}
