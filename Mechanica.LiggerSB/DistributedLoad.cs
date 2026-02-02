namespace Mechanica.LiggerSB
{
    // ---------------- DistributedLoad (linearly varying q from q1 at a to q2 at b) ----------------
    public class DistributedLoad : ILoad
    {
        public double StartPosition { get; set; }
        public double EndPosition { get; set; }
        public double StartMagnitude { get; set; } // q1 (N/m) signed (down negative)
        public double EndMagnitude { get; set; }   // q2

        public DistributedLoad(double startPosition, double endPosition, double startMagnitude, double endMagnitude)
        {
            StartPosition = startPosition;
            EndPosition = endPosition;
            StartMagnitude = startMagnitude;
            EndMagnitude = endMagnitude;
        }

        public double Length => Math.Max(0.0, EndPosition - StartPosition);

        public double TotalForce
        {
            get
            {
                if (Length <= 0) return 0.0;
                return 0.5 * (StartMagnitude + EndMagnitude) * Length;
            }
        }

        // centroid (global x) of the trapezoid (handles q1+q2 ~ 0 fallback)
        public double ForceArmFromStart
        {
            get
            {
                double q1 = StartMagnitude, q2 = EndMagnitude, L = Length;
                if (L <= 0) return StartPosition;
                if (Math.Abs(q1 + q2) < 1e-12) return StartPosition + L / 2.0;
                double xLocal = L * (q1 + 2.0 * q2) / (3.0 * (q1 + q2)); // distance from StartPosition
                return StartPosition + xLocal;
            }
        }

        // MomentAt(x) left as optional - we implement PartialMomentUpTo robustly and SBLigger will use it.
        public double MomentAt(double x) => PartialMomentUpTo(x);

        // PartialForceUpTo(x) analytic
        public double PartialForceUpTo(double x)
        {
            if (Length <= 0) return 0.0;
            if (x <= StartPosition) return 0.0;
            double effectiveEnd = Math.Min(x, EndPosition);
            double Lx = effectiveEnd - StartPosition;
            if (Lx <= 0) return 0.0;
            double q1 = StartMagnitude;
            double qx = q1 + (EndMagnitude - q1) * (Lx / Length);
            return 0.5 * (q1 + qx) * Lx;
        }

        // PartialMomentUpTo(x) = ∫_{s=Start}^{s=min(x,End)} q(s) * (x - s) ds
        // Analytical integration for linear q(s)=q1 + k*(s-Start)
        public double PartialMomentUpTo(double x)
        {
            if (x <= StartPosition) return 0.0;

            double sMax = Math.Min(x, EndPosition);
            double Lx = sMax - StartPosition;

            double q1 = StartMagnitude;
            double q2 = EndMagnitude;
            double L = EndPosition - StartPosition;

            if (L <= 0) return 0.0;

            if (Math.Abs(q1 - q2) < 1e-12)
            {
                // Constante load
                // M = ∫_0^Lx q * (x - s) ds = q * (x*Lx - Lx^2/2)
                return q1 * ((x - StartPosition) * Lx - Lx * Lx / 2.0);
            }
            else
            {
                // Lineair load q(s) = q1 + k*s
                double k = (q2 - q1) / L;

                // M = ∫_0^Lx (q1 + k*s) * (Lx - s) ds
                double dx = Lx;
                // analytisch uitgerekend:
                // ∫ q1*(dx - s) ds = q1*(dx*s - s^2/2) = q1*(dx^2 - dx^2/2) = q1*dx^2/2
                // ∫ k*s*(dx - s) ds = k*(dx^2*dx/2 - dx^3/3)? Let's compute exact:
                // ∫ k*s*(dx - s) ds = k*(dx*s^2/2 - s^3/3) 0->dx = k*(dx^3/2 - dx^3/3) = k*dx^3/6
                return q1 * dx * dx / 2.0 + k * dx * dx * dx / 6.0;
            }
        }


    }


}

