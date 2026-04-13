using System.Text.Json.Serialization;

namespace Construct.Domain.Entities
{
    /// <summary>
    /// Een hoektrap: een trap met twee lengtematen (L1 en L2) en twee breedtematen (B1 en B2).
    /// L2 moet kleiner of gelijk zijn aan L1.
    /// </summary>
    public class HoekTrapEntity : SteekTrapEntity
    {
        private double _l1 = 3000;
        private double _l2 = 1500;
        private double _b1 = 1200;
        private double _b2 = 1200;

        /// <summary>
        /// Lengte 1: de hoofdlengte van de hoektrap (in mm).
        /// </summary>
        public double L1
        {
            get => _l1;
            set
            {
                if (SetProperty(ref _l1, value))
                {
                    if (_l2 > _l1)
                    {
                        SetProperty(ref _l2, _l1, nameof(L2));
                    }
                }
            }
        }

        /// <summary>
        /// Lengte 2: de nevenlengte van de hoektrap (in mm). Moet kleiner of gelijk zijn aan L1.
        /// </summary>
        public double L2
        {
            get => _l2;
            set
            {
                var clamped = Math.Min(value, _l1);
                SetProperty(ref _l2, clamped);
            }
        }

        /// <summary>
        /// Breedte 1: de eerste breedte van de hoektrap (in mm).
        /// </summary>
        public double B1
        {
            get => _b1;
            set => SetProperty(ref _b1, value);
        }

        /// <summary>
        /// Breedte 2: de tweede breedte van de hoektrap (in mm).
        /// </summary>
        public double B2
        {
            get => _b2;
            set => SetProperty(ref _b2, value);
        }
    }
}
