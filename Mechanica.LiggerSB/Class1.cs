using System;
using System.Collections.Generic;
using System.Linq;

namespace Mechanica.LiggerSB
{

    // Resultaat met left/right waarde voor discontinuiteiten
    public record LR(double Left, double Right);

    // ---------------- Example usage / quick tests ----------------
    public static class Examples
    {

        public static SBLiggerTestBak GetTest1()
        {
            SBLiggerTestBak ligger = new SBLiggerTestBak(2.2);
            ligger.StartSupport = SupportType.Pin;
            ligger.EndSupport = SupportType.Pin;
            //ligger.Loads.Add(new DistributedLoad(0, 2.2, -5, -5));
            ligger.Loads.Add(new PointLoad(1.1, -100));


            return ligger;
        }

        

        public static void RunTest(SBLiggerTestBak ligger)
        {
            ligger.Solve();
            Console.WriteLine($"Ra = {ligger.StartVerticalReaction:F3}, Rb = {ligger.EndVerticalReaction:F3}");

            List<double> posities = [0.0, ligger.Length * 0.5, ligger.Length];

            foreach (var pos in posities)
            {
                var shear = ligger.ShearAt(pos);
                if (shear.Left == shear.Right)
                    Console.WriteLine($"Vz (@{pos:F3}) = {shear.Left:F3}");
                else
                    Console.WriteLine($"Vz (@{pos:F3}) = {shear.Left:F3} / {shear.Right:F3}");
            }
            foreach (var pos in posities)
            {
                var moment = ligger.MomentAt(pos);
                if (moment.Left == moment.Right)
                    Console.WriteLine($"My (@{pos:F3}) = {moment.Left:F3}");
                else
                    Console.WriteLine($"My (@{pos:F3}) = {moment.Left:F3} / {moment.Right:F3}");
            }


            



        }


        public static void Run()
        {
            // Convention: downward loads are NEGATIVE numbers
            var beam = new SBLiggerTestBak(10.0)
            {
                StartSupport = SupportType.Pin,
                EndSupport = SupportType.Pin
            };

            beam.Loads.Add(new PointLoad(4.0, -20.0)); // 20 kN downward at x=4
            beam.Loads.Add(new DistributedLoad(2.0, 8.0, 0.0, -10.0)); // triangular from 0 at x=2 to -10 at x=8
            beam.Loads.Add(new MomentLoad(6.0, 15.0)); // positive sagging moment of +15 at x=6

            beam.Solve();

            Console.WriteLine($"Ra = {beam.StartVerticalReaction:F3}, Rb = {beam.EndVerticalReaction:F3}");
            var shear4 = beam.ShearAt(4.0);
            Console.WriteLine($"Shear @4 left/right = {shear4.Left:F3} / {shear4.Right:F3}");
            var moment6 = beam.MomentAt(6.0);
            Console.WriteLine($"Moment @6 left/right = {moment6.Left:F3} / {moment6.Right:F3}");
        }
    }


}

