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

        private double _dikte = 200;

        /// <summary>Plaatdikte in mm</summary>
        public double Dikte
        {
            get => _dikte;
            set { _dikte = value; UpdateRei(); }
        }

        /// <summary>
        /// Dekking boven/onder voor betonplaat.
        /// BELANGRIJK: Deze wordt gedeeld met parent assemblage indien MainPart!
        /// Alleen relevant voor beton assemblages.
        /// </summary>
        public DekkingContext? PlaatDekking { get; set; }

        private PlaatWapening? _plaatWapening;

        /// <summary>
        /// Plaatwapening (boven/onder met basis + verdeelwapening).
        /// BELANGRIJK: Deze wordt gedeeld met parent assemblage indien MainPart!
        /// Alleen relevant voor beton assemblages.
        /// </summary>
        public PlaatWapening? PlaatWapening
        {
            get => _plaatWapening;
            set { _plaatWapening = value; UpdateRei(); }
        }

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

        private int _rei;

        /// <summary>
        /// Brandwerendheid aan de onderzijde van de plaat in minuten (REI).
        /// Gebaseerd op plaatdikte en hartafstand onderwapening.
        /// Ref: NEN-EN 1992-1-2:2004, Tabel 5.8 (vrijdragende massieve plaat in 1 richting)
        /// </summary>
        public int Rei => _rei;

        /// <summary>
        /// Brandwerendheideis in minuten (bijv. 30, 60, 90, 120).
        /// Waarde 0 = geen eis; de brandwerendheidstabel wordt dan niet opgenomen in het rapport.
        /// </summary>
        public int ReiEis { get; set; } = 0;

        public void UpdateRei() =>
            _rei = PlaatBrandwerendheid.GetRei(Dikte, PlaatWapening?.Onder?.BasisWapening?.ReferentieAfstand ?? 0);

        /// <summary>
        /// Bepaal de minimale hartafstand a voor deze plaat als vrijdragende massieve plaat in 1 richting.
        /// Ref: NEN-EN 1992-1-2:2004, Tabel 5.8
        /// </summary>
        /// <param name="rei">Brandweerheidseis in minuten (bijv. 30, 60, 90, 120, 180, 240)</param>
        /// <returns>
        /// Vereiste hartafstand a in mm.
        /// Geeft <see cref="double.NaN"/> terug indien de plaatdikte onvoldoende is of de REI-waarde niet in de tabel staat.
        /// </returns>
        public double GetAfstandBenodigdVoorREI(int rei) => PlaatBrandwerendheid.GetAfstandBenodigdVoorREI(rei, Dikte);

        public double GetMinimaleDikteVoorREI(int rei) => PlaatBrandwerendheid.GetMinimaleDikteVoorREI(rei, PlaatWapening?.Onder?.BasisWapening?.ReferentieAfstand ?? 0);

    }
}
