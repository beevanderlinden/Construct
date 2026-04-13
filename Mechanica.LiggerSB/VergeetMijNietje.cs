namespace Mechanica.LiggerSB
{
    public abstract class VergeetMijNietje : IVergeetMijNietje
    {
        /// <summary>
        /// Lengte in meter
        /// </summary>
        public double L { get; set; }

        /// <summary>
        /// E-modulus in kN/m²
        /// </summary>
        public double E { get; set; } = 210e6; // standaard staal

        /// <summary>
        /// Traagheid in m^4
        /// </summary>
        public double I { get; set; } = 5700e-8; // standaard HEB200

        /// <summary>
        /// Reactie in punt A in kN
        /// </summary>
        public double RA { get; set; }
        
        /// <summary>
        /// Reactie in punt B in kN
        /// </summary>
        public double RB { get; set; }

        // vaste punten
        public VmnResult PuntA => new (0, this);
        public VmnResult PuntB => new (L, this);
        public VmnResult PuntC => new (L / 2, this);

        // berekeningen (abstract, per vergeet-mij-nietje verschillend)
        public abstract double GetM(double x);
        public abstract double GetTheta(double x);
        public abstract double GetV(double x);
        public abstract double GetW(double x);

        // controle voor x (voor alle vergeet-mij-nietjes gelijk)
        public void CheckPosX(double x)
        {
            if (x < 0 || x > L)
                throw new ArgumentOutOfRangeException(nameof(x), "x moet tussen 0 en L liggen.");
        }

        public abstract double GetRA();
        public abstract double GetRB();
        public abstract void Init();
    }
}
