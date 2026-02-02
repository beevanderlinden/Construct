namespace Mechanica.LiggerSB
{
    // ---------------- SBLigger (solver without FEM; uses forget-me-not integrals) ----------------
    public class SBLiggerTestBak
    {
        public double Length { get; }
        public SupportType StartSupport { get; set; } = SupportType.Pin;
        public SupportType EndSupport { get; set; } = SupportType.Pin;

        // reactions: vertical upward positive; moments positive = sagging
        public double StartVerticalReaction { get; private set; }
        public double EndVerticalReaction { get; private set; }
        public double StartFixMoment { get; private set; }
        public double EndFixMoment { get; private set; }

        public List<ILoad> Loads { get; } = new();

        public SBLiggerTestBak(double length)
        {
            if (length <= 0) throw new ArgumentException("Length must be > 0");
            Length = length;
        }

        // Solve reactions depending on support combination.
        // Assumes ILoad.TotalForce signed (down negative).
        public void Solve()
        {
            StartVerticalReaction = EndVerticalReaction = StartFixMoment = EndFixMoment = 0.0;

            // Total vertical load (signed) and first moment about A (positive sagging)
            double totalLoad = Loads.Sum(l => l.TotalForce);
            double momentAboutA = Loads.Sum(l => l.TotalForce * l.ForceArmFromStart);

            // Sum of concentrated external moments (signed sagging positive)
            double externalMoments = Loads.OfType<MomentLoad>().Sum(m => m.M);
            // moment equilibrium term
            double momentSum = momentAboutA + externalMoments;

            bool startHasVert = (StartSupport == SupportType.Pin || StartSupport == SupportType.Roller || StartSupport == SupportType.Fixed);
            bool endHasVert = (EndSupport == SupportType.Pin || EndSupport == SupportType.Roller || EndSupport == SupportType.Fixed);
            int verticalUnknowns = (startHasVert ? 1 : 0) + (endHasVert ? 1 : 0);
            int momentUnknowns = (StartSupport == SupportType.Fixed ? 1 : 0) + (EndSupport == SupportType.Fixed ? 1 : 0);
            int totalUnknowns = verticalUnknowns + momentUnknowns;

            // Case: two vertical unknowns, no moment unknown (Pin-Pin, Roller-Pin, Roller-Roller)
            if (totalUnknowns == 2 && momentUnknowns == 0)
            {
                double Rb = -momentSum / Length;
                double Ra = -totalLoad - Rb;
                StartVerticalReaction = startHasVert ? Ra : 0.0;
                EndVerticalReaction = endHasVert ? Rb : 0.0;
                return;
            }

            // Case: Fixed-Free (one vertical + one moment unknown)
            if (totalUnknowns == 2 && momentUnknowns == 1)
            {
                // Start Fixed, End Free
                if (StartSupport == SupportType.Fixed && EndSupport == SupportType.Free)
                {
                    EndVerticalReaction = 0.0;
                    StartVerticalReaction = -totalLoad;
                    // compatibility: rotation at fixed = 0 -> ∫ M_load(x) dx + MA*L = 0  => MA = -I1/L
                    double I1 = Integrate(x => Loads.Sum(ld => ld.PartialMomentUpTo(x)), 0.0, Length);
                    StartFixMoment = -I1 / Length;
                    EndFixMoment = 0.0;
                    return;
                }
                // End Fixed, Start Free
                if (EndSupport == SupportType.Fixed && StartSupport == SupportType.Free)
                {
                    StartVerticalReaction = 0.0;
                    EndVerticalReaction = -totalLoad;
                    double I1 = Integrate(x => Loads.Sum(ld => ld.PartialMomentUpTo(x)), 0.0, Length);
                    EndFixMoment = -I1 / Length;
                    StartFixMoment = 0.0;
                    return;
                }
            }

            // Case: Fixed-Fixed (1 degree statical indeterminacy) -> forget-me-not: two eqns from rotation compatibilities
            if (momentUnknowns == 2)
            {
                // I1 = ∫ M_load(x) dx
                // I2 = ∫ M_load(x)*(L-x) dx
                double I1 = Integrate(x => Loads.Sum(ld => ld.PartialMomentUpTo(x)), 0.0, Length);
                double I2 = Integrate(x => Loads.Sum(ld => ld.PartialMomentUpTo(x)) * (Length - x), 0.0, Length);

                // Solve:
                // [ L    L/2 ] [MA] = -[I1]
                // [L/2   L   ] [MB]   -[I2]
                double a11 = Length, a12 = Length / 2.0;
                double a21 = Length / 2.0, a22 = Length;
                double b1 = -I1, b2 = -I2;
                double det = a11 * a22 - a12 * a21;
                if (Math.Abs(det) < 1e-12) throw new InvalidOperationException("Singular system in fixed-fixed solver.");

                double MA = (b1 * a22 - a12 * b2) / det;
                double MB = (a11 * b2 - b1 * a21) / det;

                StartFixMoment = MA;
                EndFixMoment = MB;

                // vertical reactions from ΣM about A: Rb*L + moment contributions (including MB at end) + momentAboutA + externalMoments = 0
                double Rb = -(momentAboutA + externalMoments + MB + 0.0 /* other contributions */) / Length;
                double Ra = -totalLoad - Rb;
                StartVerticalReaction = Ra;
                EndVerticalReaction = Rb;
                return;
            }

            // Case: single unknown vertical
            if (totalUnknowns == 1)
            {
                if (startHasVert && !endHasVert) { StartVerticalReaction = -totalLoad; EndVerticalReaction = 0.0; return; }
                if (!startHasVert && endHasVert) { EndVerticalReaction = -totalLoad; StartVerticalReaction = 0.0; return; }
            }

            // Case: Fixed-Pin or Pin-Fixed (3 unknowns) - treat pragmatically (determinable by statics + rotation compatibility)
            if (totalUnknowns == 3)
            {
                // Start Fixed + End Pin/Roller
                if (StartSupport == SupportType.Fixed && (EndSupport == SupportType.Pin || EndSupport == SupportType.Roller))
                {
                    // Solve via moments equilibrium: first compute Rb as if no fixed moment, then correct
                    double Rb = -momentSum / Length;
                    double Ra = -totalLoad - Rb;
                    double I1 = Integrate(x => Loads.Sum(ld => ld.PartialMomentUpTo(x)), 0.0, Length);
                    double MA = I1 - Rb * Length;
                    StartVerticalReaction = Ra;
                    EndVerticalReaction = Rb;
                    StartFixMoment = MA;
                    EndFixMoment = 0.0;
                    return;
                }
                // End Fixed + Start Pin/Roller
                if (EndSupport == SupportType.Fixed && (StartSupport == SupportType.Pin || StartSupport == SupportType.Roller))
                {
                    double Ra = -momentSum / Length;
                    double Rb = -totalLoad - Ra;
                    double I1 = Integrate(x => Loads.Sum(ld => ld.PartialMomentUpTo(x)), 0.0, Length);
                    double MB = I1 - Ra * Length;
                    StartVerticalReaction = Ra;
                    EndVerticalReaction = Rb;
                    StartFixMoment = 0.0;
                    EndFixMoment = MB;
                    return;
                }
            }

            // Fallback: if both vertical supports exist use two-vertical formula
            if (startHasVert && endHasVert)
            {
                double Rb = -momentSum / Length;
                double Ra = -totalLoad - Rb;
                StartVerticalReaction = Ra;
                EndVerticalReaction = Rb;
                return;
            }

            throw new InvalidOperationException("Support combo not supported by SBLigger.");
        }

        // ShearAt(x): uses strict < x for left, <= x for right for point loads/jumps.
        // positive upward on the cut
        public LR ShearAt(double x)
        {
            if (x < 0 || x > Length) throw new ArgumentOutOfRangeException(nameof(x));
            double left = 0.0, right = 0.0;

            // left: start reaction plus loads with position < x or distributed partial up to (strict)
            left += StartVerticalReaction;
            foreach (var l in Loads)
            {
                if (l is PointLoad pl)
                {
                    if (pl.Position < x) left += pl.Magnitude;
                }
                else if (l is DistributedLoad dl)
                {
                    left += dl.PartialForceUpTo(x); // this includes up to min(x,end); for strictness it's fine because distributed loads are continuous
                }
                else
                {
                    // fallback: include full load if its centroid < x
                    if (l.ForceArmFromStart < x) left += l.TotalForce;
                }
            }

            // right: include loads with position <= x for point loads
            right += StartVerticalReaction;
            foreach (var l in Loads)
            {
                if (l is PointLoad pl)
                {
                    if (pl.Position <= x) right += pl.Magnitude;
                }
                else if (l is DistributedLoad dl)
                {
                    right += dl.PartialForceUpTo(x); // same
                }
                else
                {
                    if (l.ForceArmFromStart <= x) right += l.TotalForce;
                }
            }

            return new LR(left, right);
        }

        // MomentAt(x): negative sagging (field moment), positive support moment
        // Left: StartFixMoment + StartVerticalReaction*x - sum partial moments from loads strictly left of x
        // Right: Left + concentrated moments at x (causing jump)
        public LR MomentAt(double x)
        {
            if (x < 0 || x > Length) throw new ArgumentOutOfRangeException(nameof(x));

            double left = 0.0;

            // Start fixed moment: positive for support moment, negative for sagging
            left += StartFixMoment;

            // Start vertical reaction: upward positive, downward load negative
            left += StartVerticalReaction * x;

            // Subtract contributions from loads strictly left of x
            foreach (var l in Loads)
            {
                if (l is PointLoad pl)
                {
                    if (pl.Position < x)
                        left -= pl.Magnitude * (x - pl.Position);
                }
                else if (l is DistributedLoad dl)
                {
                    left -= dl.PartialMomentUpTo(x);
                }
                else if (l is MomentLoad)
                {
                    // do nothing here, handled separately below
                }
                else
                {
                    if (l.ForceArmFromStart < x)
                        left -= l.TotalForce * (x - l.ForceArmFromStart);
                }
            }

            // End fix moment only affects section at x == Length
            if (Math.Abs(x - Length) < 1e-9)
                left += EndFixMoment;

            // Right: include concentrated moments at x (jump)
            double right = left;
            foreach (var ml in Loads.OfType<MomentLoad>())
            {
                if (ml.Position <= x + 1e-9)
                {
                    // Only add if exactly at x: jump
                    if (Math.Abs(ml.Position - x) < 1e-9)
                        right += ml.M;
                }
            }

            // --- CONVENTIE: veldmoment negatief, steunpunt positief ---
            // Negate the result so that sagging (valley) = negative
            return new LR(left, right);
        }


        // Composite Simpson integrator for computing forget-me-not integrals (relatively high n for safety)
        private static double Integrate(Func<double, double> f, double a, double b, int n = 600)
        {
            if (a == b) return 0.0;
            if (n % 2 == 1) n++;
            double h = (b - a) / n;
            double s = f(a) + f(b);
            for (int i = 1; i < n; i++)
            {
                double x = a + i * h;
                s += (i % 2 == 0) ? 2 * f(x) : 4 * f(x);
            }
            return s * h / 3.0;
        }

        // ---------------- Diagram generation ----------------
        public IEnumerable<(double x, LR shear)> GetShearDiagram(int steps = 200)
        {
            for (int i = 0; i <= steps; i++)
            {
                double x = (Length * i) / steps;
                yield return (x, ShearAt(x));
            }
        }

        public IEnumerable<(double x, LR moment)> GetMomentDiagram(int steps = 200)
        {
            for (int i = 0; i <= steps; i++)
            {
                double x = (Length * i) / steps;
                yield return (x, MomentAt(x));
            }
        }




    }


}

