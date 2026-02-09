namespace Construct.Domain.Entities
{
    using Construct.Domain.Extensions;
    using Microsoft.Extensions.Logging;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Text;


    public static class SvgExtensions
    {
        public static string ToSvgString(this IEnumerable<SvgDimLine> dimLineCollection, double scale)
        {
            var sb = new StringBuilder();
            var textSize = 12 / scale;
            foreach (var d in dimLineCollection)
            {
                var (x1o, y1o, x2o, y2o, angle) = d.GetOffsetPoints(12, 1.5, scale);

                sb.AppendLine($@"<line 
                x1=""{x1o.ToSvg()}"" y1=""{y1o.ToSvg()}"" 
                x2=""{x2o.ToSvg()}"" y2=""{y2o.ToSvg()}""
                stroke=""{d.StrokeColor}""
                stroke-width=""{d.StrokeWidth}""
                marker-start=""url(#circle-cross)""
                marker-end=""url(#circle-cross)""
                vector-effect=""non-scaling-stroke""
                />");

                //double textY = d.MidY - textOffset; // omhoog = kleinere Y in SVG (Y groeit naar beneden)

                // text boven de maatlijn
                var (midX, midY) = d.GetMidPoint(12, 1.5, scale);


                sb.AppendLine(
                    $@"<text x=""{midX.ToSvg()}"" y=""{(midY - 2 / scale).ToSvg()}"" 
                    text-anchor=""middle"" 
                    
                    font-size=""{textSize.ToSvg()}"" 
                    font-family=""Arial"" 
                    transform=""rotate({d.Angle.ToSvg()},{midX.ToSvg()},{midY.ToSvg()})"">
                    {d.DisplayValue}
                    </text>");

                // hulplijnen
                if (d.ShowExtensionLines)
                {
                    sb.AppendLine($@"<line 
                    x1=""{d.X1.ToSvg()}"" y1=""{d.Y1.ToSvg()}"" 
                    x2=""{x1o.ToSvg()}"" y2=""{y1o.ToSvg()}"" 
                    stroke=""{d.StrokeColor}"" stroke-width=""0.5"" stroke-dasharray=""2,2"" vector-effect=""non-scaling-stroke""/>");
                    sb.AppendLine($@"<line 
                    x1=""{d.X2.ToSvg()}"" y1=""{d.Y2.ToSvg()}""
                    x2=""{x2o.ToSvg()}"" y2=""{y2o.ToSvg()}"" 
                    stroke=""{d.StrokeColor}"" stroke-width=""0.5"" stroke-dasharray=""2,2"" vector-effect=""non-scaling-stroke""/>");
                }

            }
            return sb.ToString();
        }


    }


    public sealed class SvgLayout
    {
        public SvgHelper.SvgViewBox ViewBox { get; init; }
        public double Scale { get; init; }
        public double Margin { get; init; }
    }




    public class SvgHelper
    {
        public static SvgLayout CalculateBeamLayout(
            BoundingBox bb,
            double wPx,
            double hPx,
            double modelHeight,
            double modelWidth,
            int leftTextLines = 5,
            int rightTextLines = 5,
            int topTextLines = 0,
            int bottomTextLines = 0,
            double fontSizePx = 12,
            double lineHeight = 1.5)
        {
            if (wPx <= 0) wPx = 10;
            if (hPx <= 0) hPx = 10;

            var x = Math.Min(bb.MinX, 0);
            var y = Math.Min(bb.MinY, -modelHeight / 2.0);
            var w = Math.Max(bb.Width, modelWidth);
            var h = Math.Max(bb.Height, modelHeight);

            var baseViewBox = new SvgHelper.SvgViewBox(x, y, w, h);

            var vbWithMargins = baseViewBox.WithMarginsByText(
                leftLines: leftTextLines,
                rightLines: rightTextLines,
                topLines: topTextLines,
                bottomLines: bottomTextLines,
                fontSizePx: fontSizePx,
                lineHeight: lineHeight,
                actualWidthPx: wPx,
                actualHeightPx: hPx);

            double scale = vbWithMargins.GetScale(wPx, hPx);

            return new SvgLayout
            {
                ViewBox = vbWithMargins,
                Scale = scale,
                Margin = fontSizePx / scale
            };
        }


        public static SvgViewBox AddMarginPixels(SvgViewBox vb, double marginPx, double actualWidthPx, double actualHeightPx)
        {
            double scaleX = actualWidthPx / vb.Width;
            double scaleY = actualHeightPx / vb.Height;
            double marginX = marginPx / scaleX;
            double marginY = marginPx / scaleY;

            return new SvgViewBox(
                vb.X - marginX,
                vb.Y - marginY,
                vb.Width + 2 * marginX,
                vb.Height + 2 * marginY
            );
        }


        /// <summary>
        /// Genereert een verborgen SVG met een circle-cross marker die herbruikbaar is in alle SVG's.
        /// </summary>
        /// <returns>SVG-string</returns>
        public static string CreateHiddenCircleCrossSprite()
        {
            var sb = new StringBuilder();

            // wordt nu niet gebruikt (MAAR BEWAAR CODE, als voorbeeld voor gedecentraliseerde sprites!)
            // Hidden sprite container
            sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" style=\"position:absolute;width:0;height:0;overflow:hidden;\">");
            sb.AppendLine("  <defs>");
            sb.AppendLine("    <marker id=\"circle-cross\" viewBox=\"0 0 16 16\" markerUnits=\"strokeWidth\" markerWidth=\"16\" markerHeight=\"16\" refX=\"8\" refY=\"8\" orient=\"auto\">");
            sb.AppendLine("      <!-- Halve cirkel -->");
            sb.AppendLine("      <circle cx=\"8\" cy=\"8\" r=\"2.0\" fill=\"grey\" stroke=\"black\" stroke-width=\"0.5\"/>");
            sb.AppendLine("      <!-- Recht kruis (+) -->");
            sb.AppendLine("      <line x1=\"2\" y1=\"8\" x2=\"14\" y2=\"8\" stroke=\"black\" stroke-width=\"0.5\"/>");
            sb.AppendLine("      <line x1=\"8\" y1=\"2\" x2=\"8\" y2=\"14\" stroke=\"black\" stroke-width=\"0.5\"/>");
            sb.AppendLine("    </marker>");
            sb.AppendLine("  </defs>");
            sb.AppendLine("</svg>");

            return sb.ToString();
        }

        [Obsolete("Gebruik SvgDefs in plaats van deze methode.", false)]
        public static string AddDefs()
        {
            var sb = new StringBuilder();

            sb.AppendLine(" <defs> " +
                "<marker id=\"chevStart\" " +
                "viewBox=\"0 -6 10 12\" " +
                "refX=\"10\" refY=\"0\" " +
                "markerUnits=\"strokeWidth\" " +
                "markerWidth=\"6\" markerHeight=\"6\" " +
                "orient=\"auto-start-reverse\"> " +
                "<path d=\"M0,-6 L10,0 L0,6\" class=\"chevron\" /> " +
                "</marker> " +
                "<marker id=\"chevEnd\" " +
                "viewBox=\"0 -6 10 12\" " +
                "refX=\"10\" refY=\"0\" " +
                "markerUnits=\"strokeWidth\" " +
                "markerWidth=\"6\" markerHeight=\"6\" " +
                "orient=\"auto\"> " +
                "<path d=\"M0,-6 L10,0 L0,6\" class=\"chevron\" /> " +
                "</marker> " +
                "</defs>");

            return sb.ToString();
        }

        [Obsolete("Gebruik SvgPath.Render() in plaats van deze methode.", true)]
        public static string GetPathCollectionString(IEnumerable<SvgPath> pathCollection)
        {
            var sb = new StringBuilder();
            foreach (var path in pathCollection)
            {
                sb.AppendLine($@"<path d=""{path.D}""
                stroke=""{path.Stroke}""
                stroke-width=""{path.StrokeWidth.ToString("F2", CultureInfo.InvariantCulture)}""
                fill=""{path.Fill}""
                opacity=""{path.Opacity?.ToString("F2", CultureInfo.InvariantCulture)}""
                fill-opacity=""{path.FillOpacity?.ToString("F2", CultureInfo.InvariantCulture)}""
                stroke-dasharray=""{path.StrokeDashArray}""
                stroke-linejoin=""{path.StrokeLineJoin}""
                stroke-linecap=""{path.StrokeLineCap}""
                fill-rule=""{path.FillRule}"" 
                vector-effect=""{path.VectorEffect}""
                marker-start=""{path.MarkerStart}""
                marker-end=""{path.MarkerEnd}""
                />");
                //if (path.MarkerStart)
                //sb.Append("/>");
            }
            return sb.ToString();
        }

       
        public string GetSvgContentXml(IEnumerable<BaseSvg> svgObjects)
        {
            var sb = new StringBuilder();
            sb.AppendLine(AddDefs());
            foreach (var tag in svgObjects)
            {
                sb.AppendLine(tag.Render());
            }

            return sb.ToString();
        }


        public static string GetSvgContentString(IEnumerable<SvgPath> pathCollection, IEnumerable<SvgDimLine> dimLineCollection, IEnumerable<SvgText> textCollection, double scale)
        {
            var sb = new StringBuilder();



            sb.AppendLine(AddDefs());

            //var inspecteer = "";
            foreach (SvgPath path in pathCollection)
            {
                sb.AppendLine(path.Render());  
            }

            //sb.AppendLine(GetPathCollectionString(pathCollection));
            
            
            sb.AppendLine(dimLineCollection.ToSvgString(scale));
            foreach (var t in textCollection)
            {
                sb.AppendLine(t.ToSvg(scale));
            }

            return sb.ToString();

        }



        public string GetSvgStringOptimal(
            SvgDocumentInfo? documentInfo,
            SvgViewBox viewBox,
            IEnumerable<BaseSvg> paths,
            IEnumerable<SvgDimLine> dimLines,
            IEnumerable<SvgText> texts,
            double actualWidthPx = 1200,   // breedte in pixels (van style of container)
            double actualHeightPx = 600,   // hoogte in pixels (van style of contaier)


            string style = "width:100%; height:600px; border:2px solid red;",
            string status = ""
            )
        {
            var sb = new StringBuilder();

            //sb.AppendLine(CreateHiddenCircleCrossSprite());


            // schaalfactor
            double scale = viewBox.GetScale(actualWidthPx, actualHeightPx);

            //Console.WriteLine("viewBox.GetScale: " + scale);

            // Tekst altijd 12px
            double textSize = 12 / scale;

            // Marker altijd 8px
            //double markerSize = 8 / scale;
            //string ms = markerSize.ToSvg();
            //string halfsize = (markerSize / 2).ToSvg();

            sb.AppendSvgHeader(documentInfo, viewBox, null, null, style, status);

            // BEWAREN !!
            //sb.AppendLine($@"<svg xmlns=""http://www.w3.org/2000/svg""
            //preserveAspectRatio=""xMidYMid meet""
            //viewBox=""{viewBox}"" 
            //style=""{style}"">");
            // BEWAREN TOTDAT 100 procent getest

            // gebruik even getal! voor s
            int s = 8;
            int s2 = s / 2;
            int s3 = s / 4;
            string r = (0.5 * s3).ToSvg();

            sb.AppendLine("  <defs>");
            sb.AppendLine($"    <marker id=\"circle-cross\" viewBox=\"0 0 {s} {s}\" markerUnits=\"strokeWidth\" markerWidth=\"{s}\" markerHeight=\"{s}\" refX=\"{s2}\" refY=\"{s2}\" orient=\"auto\">");
            sb.AppendLine($"      <!-- Halve cirkel -->");
            sb.AppendLine($"      <circle cx=\"{s2}\" cy=\"{s2}\" r=\"{r}\" fill=\"none\" stroke=\"black\" stroke-width=\"0.5\"/>");
            sb.AppendLine($"      <!-- Recht kruis (+) -->");
            sb.AppendLine($"      <line x1=\"{s3}\" y1=\"{s2}\" x2=\"{s - s3}\" y2=\"{s2}\" stroke=\"black\" stroke-width=\"0.5\"/>");
            sb.AppendLine($"      <line x1=\"{s2}\" y1=\"{s3}\" x2=\"{s2}\" y2=\"{s - s3}\" stroke=\"black\" stroke-width=\"0.5\"/>");
            sb.AppendLine("    </marker>");
            sb.AppendLine("  </defs>");




            // defs
            double markerSize = 12 / scale; // totale marker-grootte
            string ms = markerSize.ToSvg();
            //string half = (markerSize / 2).ToSvg();

            // Kruis groter dan cirkel (150% van diameter)
            double circleR = 3;
            double crossOffset = circleR * 2; // 9 units
            double crossMin = 8 - crossOffset;  // 8 = center
            double crossMax = 8 + crossOffset;

            double textOffset = textSize * 0.4; // 1.2× textSize als marge boven de lijn

            //sb.AppendLine($@"<marker id=""circle-cross-marker"" 
            //viewBox=""0 0 16 16""
            //markerWidth=""{ms}"" markerHeight=""{ms}""
            //refX=""8"" refY=""8""
            //markerUnits=""userSpaceOnUse"" orient=""auto"">
            //<circle cx=""8"" cy=""8"" r=""{circleR}"" stroke=""black"" fill=""black"" vector-effect=""non-scaling-stroke""/>
            //<line x1=""{crossMin.ToSvg()}"" y1=""8"" x2=""{crossMax.ToSvg()}"" y2=""8"" stroke=""black"" stroke-width=""1"" vector-effect=""non-scaling-stroke""/>
            //<line x1=""8"" y1=""{crossMin.ToSvg()}"" x2=""8"" y2=""{crossMax.ToSvg()}"" stroke=""black"" stroke-width=""1"" vector-effect=""non-scaling-stroke""/>
            //</marker>");




            // ✅ NIEUW: Render alle BaseSvg objecten via hun Render() methode
            // Dit ondersteunt nu ook SvgCircle, SvgLine, SvgGroup, etc. (niet alleen SvgPath)
            foreach (var svgObj in paths)
            {
                // teksten moeten verschaald worden (runtime)
                if (svgObj is SvgText t)
                {
                    t.Scale = scale;
                }
                
                
                sb.AppendLine(svgObj.Render());
                
            }

            // DimLines + tekst
            foreach (var d in dimLines)
            {
                var (x1o, y1o, x2o, y2o, angle) = d.GetOffsetPoints(12, 1.5, scale);

                sb.AppendLine($@"<line 
                x1=""{x1o.ToSvg()}"" y1=""{y1o.ToSvg()}"" 
                x2=""{x2o.ToSvg()}"" y2=""{y2o.ToSvg()}""
                stroke=""{d.StrokeColor}""
                stroke-width=""{d.StrokeWidth}""
                marker-start=""url(#circle-cross)""
                marker-end=""url(#circle-cross)""
                vector-effect=""non-scaling-stroke""
                />");

                //double textY = d.MidY - textOffset; // omhoog = kleinere Y in SVG (Y groeit naar beneden)

                // text boven de maatlijn
                var (midX, midY) = d.GetMidPoint(12, 1.5, scale);


                sb.AppendLine(
                    $@"<text x=""{midX.ToSvg()}"" y=""{(midY - 2 / scale).ToSvg()}"" 
                    text-anchor=""middle"" 
                    
                    font-size=""{textSize.ToSvg()}"" 
                    font-family=""Arial"" 
                    transform=""rotate({d.Angle.ToSvg()},{midX.ToSvg()},{midY.ToSvg()})"">
                    {d.DisplayValue}
                    </text>");

                // hulplijnen
                if (d.ShowExtensionLines)
                {
                    sb.AppendLine($@"<line 
                    x1=""{d.X1.ToSvg()}"" y1=""{d.Y1.ToSvg()}"" 
                    x2=""{x1o.ToSvg()}"" y2=""{y1o.ToSvg()}"" 
                    stroke=""{d.StrokeColor}"" stroke-width=""0.5"" stroke-dasharray=""2,2"" vector-effect=""non-scaling-stroke""/>");
                    sb.AppendLine($@"<line 
                    x1=""{d.X2.ToSvg()}"" y1=""{d.Y2.ToSvg()}""
                    x2=""{x2o.ToSvg()}"" y2=""{y2o.ToSvg()}"" 
                    stroke=""{d.StrokeColor}"" stroke-width=""0.5"" stroke-dasharray=""2,2"" vector-effect=""non-scaling-stroke""/>");
                }

            }


            foreach (var t in texts)
            {
                sb.Append(t.ToSvg(scale));
            }


            sb.AppendSvgFooter();


            return sb.ToString();
        }



        public readonly record struct SvgViewBox(double X, double Y, double Width, double Height)
        {
            public override string ToString() =>
                string.Create(CultureInfo.InvariantCulture, $"{X} {Y} {Width} {Height}");

            public double ScaleX(double pixelWidth) => pixelWidth / Width;
            public double ScaleY(double pixelHeight) => pixelHeight / Height;
            public double GetScale(double pixelWidth, double pixelHeight)
                => Math.Min(ScaleX(pixelWidth), ScaleY(pixelHeight));

            /// <summary>
            /// Maakt een SvgViewBox met marge in aantallen tekstregels per kant.
            /// </summary>
            public SvgViewBox WithMarginsByText(
                int leftLines, int rightLines, int topLines, int bottomLines,
                double fontSizePx, double lineHeight,
                double actualWidthPx, double actualHeightPx)
            {

                // baseScale is de schaal zonder marges
                double baseScale = GetScale(actualWidthPx, actualHeightPx);

                if (baseScale == 0)
                    return new();


                // 1. Bepaal de extra ruimte voor tekst/maten
                double textHeightUnits = fontSizePx / baseScale;
                double lineSpaceUnits = textHeightUnits * lineHeight;

                double marginLeft1 = leftLines * lineSpaceUnits;
                double marginRight1 = rightLines * lineSpaceUnits;
                double marginTop1 = topLines * lineSpaceUnits;
                double marginBottom1 = bottomLines * lineSpaceUnits;

                // 2. Breid je viewBox uit
                var vb2 = new SvgViewBox(
                    X - marginLeft1,
                    Y - marginTop1,
                    Width + (marginLeft1 + marginRight1),
                    Height + (marginTop1 + marginBottom1)
                );

                var scale = vb2.GetScale(actualWidthPx, actualHeightPx);

                //double vbX = this.X - marginLeft;
                //double vbY = this.Y - marginTop;
                //double vbWidth = this.Width + marginLeft + marginRight;
                //double vbHeight = this.Height + marginTop + marginBottom;

                // 3. Bereken schaal
                //double scaleX = actualWidthPx / vbWidth;
                //double scaleY = actualHeightPx / vbHeight;
                //double scale = Math.Min(scaleX, scaleY);

                // schaal van pixels naar viewBox-units
                //double scale = Scale(actualWidthPx, actualHeightPx);

                // teksthoogte in viewBox-units
                textHeightUnits = fontSizePx / scale;
                lineSpaceUnits = textHeightUnits * lineHeight;




                // marges omgerekend naar viewBox-units
                //marginLeft = leftLines * lineSpaceUnits;
                //marginRight = rightLines * lineSpaceUnits;
                //marginTop = topLines * lineSpaceUnits;
                //marginBottom = bottomLines * lineSpaceUnits;

                // en dan nog 1x een correctie
                double correctionFactor = baseScale / scale;


                return new SvgViewBox(
                    this.X - marginLeft1 * correctionFactor,
                    this.Y - marginTop1 * correctionFactor,
                    this.Width + (marginLeft1 + marginRight1) * correctionFactor,
                    this.Height + (marginTop1 + marginBottom1) * correctionFactor
                );
            }
        }


    }

}
