namespace Mechanica.LiggerSB
{
    // ---------------- PointLoad ----------------
    public class PointLoad : ILoad
    {
        public double Position { get; set; }   // x position
        public double Magnitude { get; set; }  // signed (down negative)

        public PointLoad(double position, double magnitude)
        {
            Position = position;
            Magnitude = magnitude;
        }

        public double MomentAt(double x) => (x <= Position) ? 0.0 : Magnitude * (x - Position);

        public double TotalForce => Magnitude;
        public double ForceArmFromStart => Position;

        // For partial up to x (strictly left): include only if pos < x
        public double PartialForceUpTo(double x) => (Position < x) ? Magnitude : 0.0;

        // For moment contribution to section at x: include only if pos < x: P*(x-pos)
        public double PartialMomentUpTo(double x) => (Position < x) ? Magnitude * (x - Position) : 0.0;
    }


}

