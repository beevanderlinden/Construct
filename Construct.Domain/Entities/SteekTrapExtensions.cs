using Eurocode.Belastingen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Drawing;
using Tekla.Structures.RemotingHelper;

namespace Construct.Domain.Entities
{
    public static class SteekTrapExtensions
    {

        //public static List<Point> GetCrossSection(this )

        public static List<PointF> GetCrossSection(this SteekTrapEntity tr)
        {
            // begin met 0,0
            List<PointF> crossSection = [];
            crossSection.Add(new(0, 0)); // beginpunt

            for (int i = 0; i < tr.OptredeAantal1; i++)
            {
                crossSection.Add(new(crossSection.Last().X, crossSection.Last().Y + (float)tr.OptredeMaat)); // optrede
                crossSection.Add(new(crossSection.Last().X + (float)tr.AantredeMaat, crossSection.Last().Y)); // aantrede

            }

            // snijlijn schil

            crossSection.Add(new(0, 0)); // eindpunt


            //crossSection.Add(new((float)tr.AantredeMaat, 0)); // horizontale lijn
            //crossSection.Add(new(tr.AantredeMaat, tr.OptredeMaat)); // verticale lijn
            //crossSection.Add(new(0, tr.OptredeMaat)); // terug naar het beginpunt
            return crossSection;
        }

        public static List<PointF> GetHartlijn(this SteekTrapEntity tr)
        {
            // begin met 0,0
            List<PointF> hartlijn = [new()];

            switch (tr.SteekTrapType)
            {
                default:
                case SteekTrapTypeEnum.Standaard:
                    var dx = tr.LtProjZ;
                    var dy = tr.OptredeAantal1 * tr.OptredeMaat;
                    if (tr.GebruikEigenLengte)
                        dy = Math.Tan(tr.Hellingshoek * Math.PI / 180.0) * dx;
                    hartlijn.Add(new(hartlijn.Last().X + (float)(dx), hartlijn.Last().Y + (float)(dy))); // trap
                    return hartlijn;

                case SteekTrapTypeEnum.TrapBordes:
                    hartlijn.Add(new(hartlijn.Last().X + (float)(tr.OptredeAantal1 * tr.AantredeMaat), hartlijn.Last().Y + (float)(tr.OptredeAantal1 * tr.OptredeMaat))); // trap
                    hartlijn.Add(new(hartlijn.Last().X + (float)(tr.LengteBoven.GetValueOrDefault() - tr.AantredeMaat - tr.WelMaat), hartlijn.Last().Y)); // bordes (boven)
                    return hartlijn;

                case SteekTrapTypeEnum.TrapBordesTrap:
                    hartlijn.Add(new(hartlijn.Last().X + (float)(tr.OptredeAantal1 * tr.AantredeMaat), hartlijn.Last().Y + (float)(tr.OptredeAantal1 * tr.OptredeMaat))); // trap1 
                    hartlijn.Add(new(hartlijn.Last().X + (float)(tr.LengteTussen.GetValueOrDefault() - tr.AantredeMaat), hartlijn.Last().Y)); // bordes (tussen)
                    hartlijn.Add(new(hartlijn.Last().X + (float)(tr.OptredeAantal2 * tr.AantredeMaat), hartlijn.Last().Y + (float)(tr.OptredeAantal2 * tr.OptredeMaat))); // trap2 
                    return hartlijn;

                case SteekTrapTypeEnum.TrapBordesTrapBordes:
                    hartlijn.Add(new(hartlijn.Last().X + (float)(tr.OptredeAantal1 * tr.AantredeMaat), hartlijn.Last().Y + (float)(tr.OptredeAantal1 * tr.OptredeMaat))); // trap1
                    hartlijn.Add(new(hartlijn.Last().X + (float)(tr.LengteTussen.GetValueOrDefault() - tr.AantredeMaat), hartlijn.Last().Y)); // bordes (tussen)
                    hartlijn.Add(new(hartlijn.Last().X + (float)(tr.OptredeAantal2 * tr.AantredeMaat), hartlijn.Last().Y + (float)(tr.OptredeAantal2 * tr.OptredeMaat))); // trap2
                    hartlijn.Add(new(hartlijn.Last().X + (float)(tr.LengteBoven.GetValueOrDefault() - tr.AantredeMaat - tr.WelMaat), hartlijn.Last().Y)); // bordes (boven)
                    return hartlijn;

                case SteekTrapTypeEnum.BordesTrap:
                    hartlijn.Add(new(hartlijn.Last().X + (float)(tr.LengteOnder.GetValueOrDefault() + tr.WelMaat), hartlijn.Last().Y)); // bordes (onder)
                    hartlijn.Add(new(hartlijn.Last().X + (float)(tr.OptredeAantal1 * tr.AantredeMaat), hartlijn.Last().Y + (float)(tr.OptredeAantal1 * tr.OptredeMaat)));  // trap1
                    return hartlijn;

                case SteekTrapTypeEnum.BordesTrapBordes:
                    hartlijn.Add(new(hartlijn.Last().X + (float)(tr.LengteOnder.GetValueOrDefault() + tr.WelMaat), hartlijn.Last().Y)); // bordes (onder)
                    hartlijn.Add(new(hartlijn.Last().X + (float)(tr.OptredeAantal1 * tr.AantredeMaat), hartlijn.Last().Y + (float)(tr.OptredeAantal1 * tr.OptredeMaat))); // trap1
                    hartlijn.Add(new(hartlijn.Last().X + (float)(tr.LengteBoven.GetValueOrDefault() - tr.AantredeMaat - tr.WelMaat), hartlijn.Last().Y)); // bordes (boven)
                    return hartlijn;

                case SteekTrapTypeEnum.BordesTrapBordesTrap:
                    hartlijn.Add(new(hartlijn.Last().X + (float)(tr.LengteOnder.GetValueOrDefault() + tr.WelMaat), hartlijn.Last().Y)); // bordes (onder)
                    hartlijn.Add(new(hartlijn.Last().X + (float)(tr.OptredeAantal1 * tr.AantredeMaat), hartlijn.Last().Y + (float)(tr.OptredeAantal1 * tr.OptredeMaat))); // trap1
                    hartlijn.Add(new(hartlijn.Last().X + (float)(tr.LengteTussen.GetValueOrDefault() - tr.AantredeMaat), hartlijn.Last().Y)); // bordes (tussen)
                    hartlijn.Add(new(hartlijn.Last().X + (float)(tr.OptredeAantal2 * tr.AantredeMaat), hartlijn.Last().Y + (float)(tr.OptredeAantal2 * tr.OptredeMaat))); // trap2
                    return hartlijn;

                case SteekTrapTypeEnum.BordesTrapBordesTrapBordes:
                    hartlijn.Add(new(hartlijn.Last().X + (float)(tr.LengteOnder.GetValueOrDefault() + tr.WelMaat), hartlijn.Last().Y)); // bordes (onder)
                    hartlijn.Add(new(hartlijn.Last().X + (float)(tr.OptredeAantal1 * tr.AantredeMaat), hartlijn.Last().Y + (float)(tr.OptredeAantal1 * tr.OptredeMaat))); // trap1
                    hartlijn.Add(new(hartlijn.Last().X + (float)(tr.LengteTussen.GetValueOrDefault() - tr.AantredeMaat), hartlijn.Last().Y)); // bordes (tussen)
                    hartlijn.Add(new(hartlijn.Last().X + (float)(tr.OptredeAantal2 * tr.AantredeMaat), hartlijn.Last().Y + (float)(tr.OptredeAantal2 * tr.OptredeMaat))); // trap2
                    hartlijn.Add(new(hartlijn.Last().X + (float)(tr.LengteBoven.GetValueOrDefault() - tr.AantredeMaat - tr.WelMaat), hartlijn.Last().Y)); // bordes (boven)
                    return hartlijn;




            }
        }







        public static SectionForces GetSnedekrachten(this SteekTrapEntity tr, bool isBGT = false)
        {
            var krachten = tr.GetKrachten();

            if (isBGT)
            {
                return new() { My = krachten.Mfreq, Vz = krachten.Vfreq };
            }
            else
            {
                return new() { My = krachten.MEd, Vz = krachten.VEd };
            }





        }

        /// <summary>
        /// Tijdelijke berekening (zonder ligger of raamwerk)
        /// </summary>
        /// <param name="tr"></param>
        /// <returns></returns>
        public static KrachtenDemo GetKrachten(this SteekTrapEntity tr)
        {
            SteekTrapService service = new();
            var krachtenDemo = service.GetDemoKrachten(tr, "schil");
            return krachtenDemo;
        }


        public static double GetSchuine(this SteekTrapEntity tr)
        {
            return Math.Sqrt(Math.Pow(tr.AantredeMaat, 2) + Math.Pow(tr.OptredeMaat, 2));
        }
        public static double GetCos(this SteekTrapEntity tr)
        {
            return Math.Cos(tr.AantredeMaat / tr.GetSchuine());
        }

        public static double GetDsnOppTrede(this SteekTrapEntity tr)
        {
            var opp1 = tr.SchilDikte * tr.GetSchuine();
            var opp2 = tr.OptredeMaat * tr.AantredeMaat * 0.5;
            var opp3 = tr.WelMaat * tr.WelMaatVertikaal + (tr.OptredeMaat - tr.WelMaatVertikaal) * tr.WelMaat * 0.5;

            return opp1 + opp2 + opp3;
        }

        /// <summary>
        /// Schoenveterformule (Shoelace) op de 7 polygon-hoekpunten van één trede.
        /// Dient als algebraïsche verificatie van <see cref="GetDsnOppTrede"/>.
        /// Beide methoden leveren exact dezelfde oppervlakte op.
        ///
        /// Hoekpunten (relatief, P0 = soffit voor):
        ///   P0 (0, 0)                      soffit voor
        ///   P1 (0, −d)                     schilVert omhoog
        ///   P2 (welHor, −d)                wel naar rechts
        ///   P3 (0, −d−op+welVert)          neus onderkant
        ///   P4 (0, −d−op)                  neuspunt
        ///   P5 (aan, −d−op)                trede naar rechts
        ///   P6 (aan, −op)                  soffit achter
        /// waarbij d = SchilDikte × schuine / aan  +  welHor × op / aan (= SchilVertEff)
        /// </summary>
        public static double GetDsnOppTredePolygoon(this SteekTrapEntity tr)
        {
            double aan     = tr.AantredeMaat;
            double op      = tr.OptredeMaat;
            double welHor  = tr.WelMaat;
            double welVert = tr.WelMaatVertikaal;
            double schuine = tr.GetSchuine();
            double d       = tr.SchilDikte * schuine / aan + welHor * op / aan;

            double[] x = [0,    0,      welHor, 0,              0,      aan,    aan ];
            double[] y = [0,   -d,     -d,      -d - op + welVert, -d - op, -d - op, -op ];

            double sum = 0;
            for (int i = 0; i < 7; i++)
            {
                int j = (i + 1) % 7;
                sum += x[i] * y[j] - x[j] * y[i];
            }
            return Math.Abs(sum) * 0.5;
        }



        public static double GetGk(this SteekTrapEntity tr, bool isProjectieZ = true)
        {
            if (isProjectieZ)
            {
                return Math.Round(tr.GetDsnOppTrede() / tr.AantredeMaat / 1000 * 25, 2);
            }
            else
            {
                return Math.Round(tr.GetDsnOppTrede() / tr.GetSchuine() / 1000 * 25, 2);
            }
        }


        public static double GetBelastingTrapBoom(this SteekTrapEntity tr)
        {
            return tr.GetGk() * tr.GetLengteTotaal();
        }

        public static double GetPermanenteBelasting(this SteekTrapEntity tr, bool isProjectieZ = true)
        {
            return tr.GetGk(isProjectieZ) + tr.AfwerkingVlaklast;
        }

        public static double GetPermanenteBelastingBordes(this SteekTrapEntity trb)
        {
            List<double> diktes = [trb.DikteOnder ?? 200, trb.DikteBoven ?? 200, trb.DikteTussen ?? 200];
            return diktes.Max() / 1000 * 25;
        }


        public static OpgelegdeBelastingen? GetOpgelegdeBelasting(this SteekTrapEntity trap)
        {
            var bg2 = trap.Belastingen.BelastingGevallen.FirstOrDefault(bg => bg.Nr == 2);
            if (bg2 == null) return null;
            return bg2.OpgelegdeBelastingen;
        }


        public static double GetPermanenteBelastingBordesTussen(this SteekTrapEntity trb)
        {
            return (trb.DikteTussen ?? 200.00) / 1000 * 25;
        }
        public static double GetPermanenteBelastingBordesOnder(this SteekTrapEntity trb)
        {
            return (trb.DikteOnder ?? 200.00) / 1000 * 25;
        }
        public static double GetPermanenteBelastingBordesBoven(this SteekTrapEntity trb)
        {
            return (trb.DikteBoven ?? 200.00) / 1000 * 25;
        }




        public static double GetLengteTotaal(this SteekTrapEntity tr)
        {
           

            //var hartlijn = tr.GetHartlijn().OrderBy(p => p.X).ToImmutableList();
            var lengteTotaal = (tr.OptredeAantal1 -1) * tr.AantredeMaat + (tr.LengteBoven ?? tr.AantredeMaat) + (tr.LengteOnder ?? 0) ;
            
            if (tr.TandOpleggingBovenzijde is TandOplegging tand)
            {
                lengteTotaal -= tand.TandLengte / 2.0;
            }
            
            if (tr.TandOpleggingOnderzijde is TandOplegging tandOnder)
            {
                lengteTotaal -= tandOnder.TandLengte / 2.0;
            }


            // als door de referentie onder steekt (bijvoorbeeld bij bordes)
            // dan kan de lengte worden opgegeven.
            if (tr.OnderType == TredeOnderType.IsBordes ||
                tr.OnderType == TredeOnderType.OpBordes)
            {
                lengteTotaal += tr.LengteOnder ?? 0;
            }
            
           




            if (tr.Slankheid != null)
            {
                tr.Slankheid.LengteOverspanning = (int)(lengteTotaal * tr.SchuineMaat / tr.AantredeMaat);

                //tr.Slankheid.EffectieveDikte = 
            }


            return (int)lengteTotaal;
        }

        public static double GetHoogteTotaal(this SteekTrapEntity tr)
        {
            var hartlijn = tr.GetHartlijn().OrderBy(p => p.Y).ToImmutableList();
            return Math.Round(hartlijn.Last().Y - hartlijn.First().Y, 0);
        }


        public static double GetMomentVgmn1(this SteekTrapEntity tr, double q)
        {
            return 0.125 * q * Math.Pow(tr.GetLengteTotaal(), 2);
        }


        public static (double m, double d1, double d2) GetVergeetMeNietje(VergeetMeNietje vergeetMeNietje, double waarde, double l, double a)
        {
            double m = 0;
            double d1 = 0;
            double d2 = 0;
            double b = l - a;

            switch (vergeetMeNietje)
            {
                case VergeetMeNietje.VrijVrijPuntlast:
                    m = waarde * (a * b) / l;
                    d1 = waarde * b / l;
                    d2 = waarde * a / l;
                    break;

                case VergeetMeNietje.VrijVrijLijnlast:
                    m = waarde * l / 2 * (a - a * a / l);
                    d1 = d2 = waarde * l / 2 * (1 - 2 * a / l); // geen sprong in d-lijn
                    break;

            }

            return (m, d1, d2);
        }







        public enum VergeetMeNietje
        {

            VrijVrijPuntlast = 7,
            VrijVrijLijnlast = 8,
        };





    }





}
