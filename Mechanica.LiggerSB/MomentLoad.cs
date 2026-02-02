namespace Mechanica.LiggerSB
{
    // ---------------- MomentLoad (concentrated moment) ----------------
    public class MomentLoad : ILoad
    {
        public double Position { get; set; }
        public double M { get; set; } // signed concentrated moment (positive = sagging)

        public MomentLoad(double position, double m)
        {
            Position = position;
            M = m;
        }

        public double MomentAt(double x) => 0.0; // handled as discontinuity in SBLigger

        public double TotalForce => 0.0;
        public double ForceArmFromStart => Position;

        public double PartialForceUpTo(double x) => 0.0;
        public double PartialMomentUpTo(double x) => 0.0; // don't include concentrated moment in the continuous integral
    }


}

