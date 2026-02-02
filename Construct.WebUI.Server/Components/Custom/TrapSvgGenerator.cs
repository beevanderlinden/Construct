//namespace Construct.WebUI.Server.Components.Custom
//{
//    using Construct.Domain.Entities;
//    using Construct.Domain.Extensions;
//    using Microsoft.JSInterop;
//    using System.Text;
//    using static Construct.WebUI.Server.Components.Custom.SvgHelper;

//    public static class TrapSvgGenerator
//    {
//        public static List<SvgPath> GenerateTrapPaths(SteekTrapEntity trap, BoundingBox bb, bool zonderAfrondingen = true)
//        {
//            List<SvgPath> returnList = [];

//            // referentie onderkant
//            //returnList.Add(GenerateBottomRef(trap));
//            //returnList.Add(GenerateTopRef(trap));



//            // doorsnede
//            returnList.Add(GenerateTrapSvgPath(trap, bb, zonderAfrondingen));





//            return returnList;

//        }


//        public static async Task<(string SvgXml, string? Base64Png)> GenerateTrapSvgXmlWithPngAsync(
//           IJSRuntime jsRuntime,
//           SteekTrapEntity steekTrap,
//           BoundingBox boundingBox,
//           double widthPx,
//           double heightPx,
//           string style)
//        {
//            // ---- 1. Genereer SVG
//            string svgXml = GenerateTrapSvgXml(steekTrap, boundingBox, widthPx, heightPx, style);

//            // ---- 2. Vraag aan JS om deze SVG om te zetten naar base64 PNG
//            string? base64Png = null;
//            try
//            {
//                base64Png = await jsRuntime.InvokeAsync<string>(
//                    "svgHelpers.convertToBase64Png",
//                    svgXml,
//                    (int)widthPx,
//                    (int)heightPx
//                );
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"⚠️ Fout bij SVG → PNG conversie: {ex.Message}");
//            }

//            // ---- 3. Retourneer tuple
//            return (svgXml, base64Png);
//        }


//        public static (string SvgXml, string? Base64Png) GenerateTrapSvgXmlWithPng(
//            SteekTrapEntity steekTrap,
//            BoundingBox boundingBox,
//            double widthPx,
//            double heightPx,
//            string style)
//        {
//            // ---- 1. Genereer SVG XML
//            string svgXml = GenerateTrapSvgXml(steekTrap, boundingBox, widthPx, heightPx, style);

//            // ---- 2. Base64 PNG komt later van de Blazor JS interop
//            string? base64Png = null;





//            // ---- 3. Retourneer beide
//            return (svgXml, base64Png);
//        }





//        public static string GenerateTrapSvgXml(SteekTrapEntity trap, BoundingBox bb, double actualWidthPx, double actualHeightPx, string style = "width:auto; height:auto;")
//        {
//            SvgHelper svgHelper = new();
//            SvgDocumentInfo? info = new()
//            {
//                Description = "steektrap",
//                Label = trap.Merk,

//            };

//            // 💡maak de paden en maatlijnen
//            var svgPaths = GenerateTrapPaths(trap, bb);
//            var dimLines = GenerateTrapDimLines(trap);

//            // Bepaal de viewBox
//            var x = Math.Min(bb.MinX, 0);
//            var y = Math.Min(bb.MinY, -trap.HoogteTotaal);
//            var w = Math.Max(bb.Width, trap.LengteTotaal + trap.WelMaat); // tijdelijke oplossing, gebruik later een boundingBox voor in assemblage-entiteit.
//            var h = Math.Max(bb.Height, trap.HoogteTotaal);
//            Construct.Domain.Entities.SvgHelper.SvgViewBox viewBox = new(x, y, w, h);

//            // Maak een nieuwe viewbox aan met het aantal regelafstanden in rondom de tekening.
//            var vbWithMargins = viewBox.WithMarginsByText(
//                leftLines: 5,
//                rightLines: 5,
//                topLines: 4,
//                bottomLines: 1,
//                fontSizePx: 12,
//                lineHeight: 1.5,
//                actualWidthPx: actualWidthPx,
//                actualHeightPx: actualHeightPx
//            );


//            var teksten = GenerateTrapTeksten(trap, bb, vbWithMargins.GetScale(actualWidthPx, actualHeightPx));


//            var svg2 = svgHelper.GetSvgStringOptimal(info, vbWithMargins, svgPaths, dimLines, teksten, actualWidthPx, actualHeightPx, style);


//            return svg2;
//        }


//        public static List<SvgText> GenerateTrapTeksten(SteekTrapEntity trap, BoundingBox bb, double scale)
//        {
//            List<SvgText> returnList = [];


//            var x1 = trap.LengteTotaal / 2;
//            var x2 = x1 + trap.AantredeMaat;
//            var y1 = -trap.HoogteTotaal / 2;
//            var y2 = y1 - trap.OptredeMaat;
//            var txt = new SvgText($"\r\n{trap.WapeningSchil}", x1, y1, x2, y2);
//            {

//            }
//            ;
//            txt.AddToBoundingBox(bb, scale);

//            //returnList.Add(txt);

//            return returnList;

//        }

//        public static List<SvgDimLine> GenerateTrapDimLines(SteekTrapEntity trap)
//        {
//            List<SvgDimLine> returnList = [];
//            // totaal (ver.)
//            returnList.Add(new SvgDimLine
//            {
//                Mode = DimLineMode.Vertical,
//                X1 = 0,
//                Y1 = 0,
//                X2 = trap.LengteTotaal,
//                Y2 = -trap.HoogteTotaal,
//                Offset = trap.WelMaat,
//                OffsetLines = 3,
//                //Value = trap.HoogteTotaal,
//                Text = $"{trap.OptredeAantal1}×{trap.OptredeMaat:0.#} = {trap.HoogteTotaal:0}",
//                StrokeWidth = 1,
//                StrokeColor = "black"
//            });





//            // totaal (hor.)
//            returnList.Add(new SvgDimLine
//            {
//                Mode = DimLineMode.Horizontal,
//                X1 = 0,
//                Y1 = 0,
//                X2 = trap.LengteTotaal,
//                Y2 = -trap.HoogteTotaal,
//                Offset = 0,
//                OffsetLines = 2,
//                //Value = trap.AantredeMaat,
//                Text = $"{trap.LengteTotaal:0}",
//                StrokeWidth = 1,
//                StrokeColor = "black"
//            });


//            // diagonaal
//            returnList.Add(new SvgDimLine
//            {
//                Mode = DimLineMode.Aligned,
//                X1 = 0,
//                Y1 = 0,
//                X2 = trap.LengteTotaal,
//                Y2 = -trap.HoogteTotaal,
//                Offset = 0,
//                OffsetLines = -3,
//                Text = $"{trap.LengteSchuin:0}",

//            });




//            if (!trap.GebruikEigenLengte)
//            {

//                // schil
//                returnList.Add(new SvgDimLine
//                {
//                    Mode = DimLineMode.Aligned,
//                    X1 = trap.AantredeMaat,
//                    Y1 = -trap.OptredeMaat,
//                    X2 = trap.AantredeMaat + trap.SchilDikte * (trap.OptredeMaat / trap.SchuineMaat),
//                    Y2 = -trap.OptredeMaat + trap.SchilDikte * (trap.AantredeMaat / trap.SchuineMaat),
//                    Offset = 0,
//                    OffsetLines = -3,
//                    Text = $"{trap.SchilDikte:0}",

//                });


//                if (trap.TandOpleggingBovenzijde != null)
//                {
//                    returnList.Add(new SvgDimLine
//                    {
//                        Mode = DimLineMode.Horizontal,
//                        X1 = trap.LengteTotaal - trap.TandOpleggingBovenzijde.TandLengte,
//                        Y1 = -trap.HoogteTotaal + trap.TandOpleggingBovenzijde.TandHoogte,
//                        X2 = trap.LengteTotaal,
//                        Y2 = -trap.HoogteTotaal,
//                        Offset = -trap.TandOpleggingBovenzijde.TandHoogte,
//                        OffsetLines = -2,
//                    });

//                    returnList.Add(new SvgDimLine
//                    {
//                        Mode = DimLineMode.Vertical,
//                        X1 = trap.LengteTotaal - trap.TandOpleggingBovenzijde.TandLengte,
//                        Y1 = -trap.HoogteTotaal + trap.TandOpleggingBovenzijde.TandHoogte,
//                        X2 = trap.LengteTotaal,
//                        Y2 = -trap.HoogteTotaal,
//                        Offset = -trap.TandOpleggingBovenzijde.TandLengte,
//                        OffsetLines = -2,
//                    });

//                }

//            }


//            return returnList;
//        }

//        public static SvgPath GenerateTrapSvgPath(SteekTrapEntity trap, BoundingBox bb, bool zonderAfrondingen = true)
//        {
//            var pathData = GenerateTrapPath(trap, bb, zonderAfrondingen);
//            return new SvgPath
//            {
//                D = pathData,
//                Stroke = "black",
//                StrokeWidth = 2,
//                Fill = "lightgray",
//                Opacity = 1.0,
//                StrokeDashArray = "",
//                StrokeLineJoin = "miter",
//                StrokeLineCap = "butt",
//                FillRule = "nonzero"
//            };
//        }

//        public static SvgPath GenerateBottomRef(SteekTrapEntity trap)
//        {
//            return new SvgPath
//            {
//                D = "M -1000 0 l 9000 0",
//                Stroke = "black",
//                StrokeWidth = 1,
//                Fill = "lightgray",
//                Opacity = 1.0,
//                StrokeDashArray = "",
//                StrokeLineJoin = "miter",
//                StrokeLineCap = "butt",
//                FillRule = "nonzero"
//            };
//        }


//        public static SvgPath GenerateTopRef(SteekTrapEntity trap)
//        {
//            int top = (int)-trap.HoogteTotaal;

//            return new SvgPath
//            {
//                D = $"M -1000 {top} l 9000 0",
//                Stroke = "black",
//                StrokeWidth = 1,
//                Fill = "lightgray",
//                Opacity = 1.0,
//                StrokeDashArray = "",
//                StrokeLineJoin = "miter",
//                StrokeLineCap = "butt",
//                FillRule = "nonzero"
//            };
//        }



//        public static string GenerateTrapPath(SteekTrapEntity trap, BoundingBox bb, bool zonderAfrondingen = true)
//        {
//            var sb = new StringBuilder();

//            if (trap.GebruikEigenLengte)
//            {
//                // eigen opgave lengte => simpel pad, geen trap tekenen
//                sb.Append($"M {0} {0} ");
//                sb.Append($"l {trap.LengteTotaal.ToSvg()} {(-trap.HoogteTotaal).ToSvg()} ");
//                sb.Append("Z");
//                return sb.ToString();
//            }


//            var topRadius = zonderAfrondingen ? 0 : trap.TopRadius;
//            var bottomRadius = zonderAfrondingen ? 0 : trap.BottomRadius;
//            double wel = trap.WelMaat;
//            double welV = trap.WelMaatVertikaal;
//            double tandHoogte = trap.TandOpleggingBovenzijde?.TandHoogte ?? 0;
//            double tandLengte = trap.TandOpleggingBovenzijde?.TandLengte ?? 0;


//            // Startpunt links-onder
//            double x = 0, y = 0;
//            sb.Append($"M {x} {y} ");
//            bb.Add(x, y);

//            // bovencontour
//            for (int i = 0; i < trap.OptredeAantal1; i++)
//            {
//                // omhoog (optrede - TopRadius)
//                // x = -wel, y = optrede 
//                double optredeNetto = trap.OptredeMaat - topRadius - welV;
//                if (optredeNetto > 0)
//                {
//                    x = -wel;
//                    y = -optredeNetto;
//                    sb.Append($"l {x.ToSvg()} {y.ToSvg()} ");
//                    bb.Add(x, y);
//                }


//                if (welV > 0)
//                {
//                    x = 0;
//                    y = -welV;
//                    sb.Append($"l {x.ToSvg()} {y.ToSvg()} ");
//                    //bb.Add(x, y);
//                }

//                // boogje bovenzijde
//                //if (trap.TopRadius > 0)
//                //    sb.Append($"a {topRadius},{topRadius} 0 0 1 {topRadius},{-topRadius} ");

//                // rechts (aantrede - TopRadius - BottomRadius)
//                // x = wel + aantrede
//                double aantredeNetto = trap.AantredeMaat - topRadius - bottomRadius + wel;
//                if (aantredeNetto > 0)
//                {
//                    x = aantredeNetto;
//                    y = 0;
//                    sb.Append($"l {x.ToSvg()} {y.ToSvg()} ");

//                }

//                // boogje onderzijde (afronding trede)
//                //if (bottomRadius > 0)
//                //    sb.Append($"a {bottomRadius},{bottomRadius} 0 0 1 {bottomRadius},{bottomRadius} ");

//                //// omhoog (TopRadius + rest optrede)
//                //if (topRadius > 0)
//                //    sb.Append($"l 0,{-topRadius} ");
//            }

//            // eindpunt looplijn (bovenste voorzijde van trap)
//            double x2 = trap.AantredeMaat * trap.OptredeAantal1 - tandLengte;
//            double y2 = -trap.OptredeMaat * trap.OptredeAantal1 - tandHoogte;

//            // 100mm naar beneden, dan 100mm links
//            sb.Append($"l 0 {tandHoogte.ToSvg()} l {(-tandLengte).ToSvg()} 0 ");

//            // offsetlijn berekenen
//            var (q1x, q1y, q2x, q2y) = OffsetLoopLijn((0, 0), (trap.AantredeMaat, -trap.OptredeMaat), trap.SchilDikte);

//            // rechts snijpunt (verticale lijn door eindpunt)
//            var rightIntersect = IntersectLines(
//                (q1x, q1y), (q2x, q2y),
//                (x2, y2), (x2, y2 + 1000)
//            );

//            // links snijpunt (horizontale lijn door beginpunt)
//            var leftIntersect = IntersectLines(
//                (q1x, q1y), (q2x, q2y),
//                (0, 0), (1000, 0)
//            );

//            //CultureInfo culture = CultureInfo.InvariantCulture;

//            if (rightIntersect is { } r)
//            {
//                sb.Append($"L {r.X.ToSvg()} {r.Y.ToSvg()} ");
//                bb.Add(r.X, r.Y);

//            }
//            if (leftIntersect is { } l)
//            {
//                sb.Append($"L {l.X.ToSvg()} {l.Y.ToSvg()} ");
//                bb.Add(l.X, l.Y);
//            }

//            // sluiten
//            sb.Append("Z");

//            return sb.ToString();
//        }

//        private static (double, double, double, double) OffsetLoopLijn(
//            (double X, double Y) p1,
//            (double X, double Y) p2,
//            double offset)
//        {
//            double dx = p2.X - p1.X;
//            double dy = p2.Y - p1.Y;
//            double len = Math.Sqrt(dx * dx + dy * dy);

//            // +offset → visueel omlaag in SVG
//            double ox = -dy / len * offset;
//            double oy = dx / len * offset;

//            return (p1.X + ox, p1.Y + oy, p2.X + ox, p2.Y + oy);
//        }

//        private static (double X, double Y)? IntersectLines(
//            (double X, double Y) p1, (double X, double Y) p2,
//            (double X, double Y) q1, (double X, double Y) q2)
//        {
//            double dx1 = p2.X - p1.X;
//            double dy1 = p2.Y - p1.Y;
//            double dx2 = q2.X - q1.X;
//            double dy2 = q2.Y - q1.Y;

//            double det = dx1 * dy2 - dy1 * dx2;
//            if (Math.Abs(det) < 1e-9) return null; // evenwijdig

//            double t = ((q1.X - p1.X) * dy2 - (q1.Y - p1.Y) * dx2) / det;
//            return (p1.X + t * dx1, p1.Y + t * dy1);
//        }
//    }

//}
