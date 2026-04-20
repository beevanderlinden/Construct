using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Construct.Application.Interfaces.Beam
{
    public class PointLoad : ILoad
    {
        public double Position { get; set; }
        public double Magnitude { get; set; }
        public PointLoad(double position, double magnitude)
        {
            Position = position;
            Magnitude = magnitude;
        }
        public double MomentAt(double x)
        {
            if (x < Position)
                return 0;
            return Magnitude * (x - Position);
        }
        public double TotalForce => Magnitude;
        public double ForceArmFromStart => Position;

    }

    public class DistributedLoad : ILoad
    {
        public double StartPosition { get; set; }
        public double EndPosition { get; set; }
        public double StartMagnitude { get; set; } 
        public double EndMagnitude { get; set; }
        public DistributedLoad(double startPosition, double endPosition, double startMagnitude, double endMagnitude)
        {
            StartPosition = startPosition;
            EndPosition = endPosition;
            StartMagnitude = startMagnitude;
            EndMagnitude = endMagnitude;
        }
        public double MomentAt(double x)
        {
            if (x <= StartPosition)
                return 0;

            double effectiveEnd = Math.Min(x, EndPosition);
            double L = effectiveEnd - StartPosition;

            // lineaire last: moment = (q1 + q2) / 2 * L * (afstand van zwaartepunt tot x)
            double q1 = StartMagnitude;
            double q2 = StartMagnitude + (EndMagnitude - StartMagnitude) * (L / (EndPosition - StartPosition)); // lineair interpoleren tot punt x
            double total = 0.5 * (q1 + q2) * L;
            double centroid = L * (2 * q2 + q1) / (3 * (q1 + q2));
            return total * (x - StartPosition - centroid);
        }
        public double TotalForce
        {
            get
            {
                return 0.5 * (StartMagnitude + EndMagnitude) * (EndPosition - StartPosition);
            }
        }
        public double ForceArmFromStart
        {
            get
            {
                double q1 = StartMagnitude;
                double q2 = EndMagnitude;
                double L = EndPosition - StartPosition;

                // Geen lengte of beide magnitudes nul -> midden
                if (L <= 0) return StartPosition;
                if (Math.Abs(q1) < 1e-12 && Math.Abs(q2) < 1e-12)
                    return StartPosition + L / 2.0;

                // Als q1 + q2 bijna nul is, is de totale verticale kracht ~0 en
                // het zwaartepunt (voor kracht) is niet goed gedefinieerd.
                // In dat geval kun je kiezen: return midden, of behandel integraal exact.
                if (Math.Abs(q1 + q2) < 1e-12)
                {
                    // fallback: return midpoint (of gooi exceptie als je dat prefereert)
                    return StartPosition + L / 2.0;
                }

                // Correcte centroid-formule voor lineair variërende last:
                double xLocal = L * (q1 + 2.0 * q2) / (3.0 * (q1 + q2));

                return StartPosition + xLocal;
            }
        }
    }


    public class MomentLoad : ILoad
    {
        public double Position { get; set; }
        public double Magnitude { get; set; }
        public MomentLoad(double position, double magnitude)
        {
            Position = position;
            Magnitude = magnitude;
        }
        public double MomentAt(double x)
        {
            if (x < Position)
                return 0;

            return Magnitude;
        }

        public double TotalForce => 0; // geen verticale kracht
        public double ForceArmFromStart => Position;
    }

    public class SimpleBeam
    {
        public double Length { get; set; }
        public double StartReaction { get; set; }
        public double EndReaction { get; set; }
        public List<ILoad> Loads { get; set; } = [];

        public double MomentAt(double x)
        {
            double moment = StartReaction * x;
            foreach (var load in Loads)
            {
                moment += load.MomentAt(x);
            }
            return -moment;
        }

        public double ShearAt(double x)
        {
            double shear = StartReaction;
            foreach (var load in Loads)
            {
                // Alle verticale lasten die links van x werken
                if (load is PointLoad pl && pl.Position <= x)
                    shear += pl.TotalForce;

                else if (load is DistributedLoad dl)
                {
                    if (x >= dl.StartPosition)
                    {
                        double effectiveEnd = Math.Min(x, dl.EndPosition);
                        double L = effectiveEnd - dl.StartPosition;
                        // lineair verdeelde last: gemiddelde kracht over L
                        double q1 = dl.StartMagnitude;
                        double q2 = dl.StartMagnitude + (dl.EndMagnitude - dl.StartMagnitude) * (L / (dl.EndPosition - dl.StartPosition));
                        double total = 0.5 * (q1 + q2) * L;
                        shear += total;
                    }
                }
                // MomentLoad veroorzaakt geen verticale kracht
            }

            return shear;
        }


        public void SolveReactions()
        {
            double sumForce = 0; // som van de krachten 
            double sumMoment = 0;

            foreach (var load in Loads)
            {
                sumForce += load.TotalForce;
                sumMoment += load.TotalForce * load.ForceArmFromStart;
            }

            // Evenwicht vergelijkingen
            EndReaction = -sumMoment / Length;
            StartReaction = -sumForce - EndReaction;

        }




    }

}
