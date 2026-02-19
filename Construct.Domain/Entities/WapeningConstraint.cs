using System;

namespace Construct.Domain.Entities
{
    /// <summary>
    /// Constraints voor wapeningsberekeningen
    /// </summary>
    public class WapeningConstraint
    {
        /// <summary>
        /// Minimale diameter in mm
        /// </summary>
        public double? DiameterMin { get; set; }

        /// <summary>
        /// Minimaal aantal staven
        /// </summary>
        public int? AantalMin { get; set; }

        /// <summary>
        /// Maximale hart-op-hart afstand in mm
        /// </summary>
        public double? HohMax { get; set; }

        public WapeningConstraint()
        {
        }

        public WapeningConstraint(double? diameterMin, int? aantalMin = null, double? hohMax = null)
        {
            DiameterMin = diameterMin;
            AantalMin = aantalMin;
            HohMax = hohMax;
        }
    }
}
