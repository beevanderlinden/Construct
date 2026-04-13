namespace Construct.Application.Services
{
    using Construct.Domain.Entities;
    using Mechanica.LiggerSB;

    public enum VgnDiagramType { V, M, W }

    public static class VgnDiagramService
    {
        // SVG-coördinaatruimte (pixels/eenheden)
        private const double SvgW         = 300;
        private const double SvgH         = 120;
        private const double BaselineY    = 72;   // y-positie van de balk-as
        private const double MaxAmplitude = 52;   // max uitwijking boven/onder baseline
        private const double CircleR      = 0.8;
        private const double LabelPts     = 10;
        private const double Padding      = 8;    // marge rondom het diagram

        // ----------------------------------------------------------------
        //  Sampling
        // ----------------------------------------------------------------

        /// <summary>
        /// Geeft N+1 punten terug: x = 0, L/N, 2L/N … L.
        /// </summary>
        public static List<VmnSampleResult> Sample(VergeetMijNietje vmn, int n = 8)
        {
            var results = new List<VmnSampleResult>(n + 1);
            for (int i = 0; i <= n; i++)
            {
                double x = i * vmn.L / n;
                results.Add(new VmnSampleResult(x, vmn.GetV(x), vmn.GetM(x), vmn.GetW(x)));
            }
            return results;
        }

        // ----------------------------------------------------------------
        //  SVG opbouw
        // ----------------------------------------------------------------

        public static SvgDocument BuildDiagram(
            VergeetMijNietje vmn,
            List<VmnSampleResult> samples,
            VgnDiagramType type)
        {
            Func<VmnSampleResult, double> selector = type switch
            {
                VgnDiagramType.V => s => s.V,
                VgnDiagramType.M => s => s.M,
                VgnDiagramType.W => s => s.W,
                _                => s => 0
            };

            var (stroke, fill, unit) = type switch
            {
                VgnDiagramType.V => ("steelblue",  "lightblue",  "kN"),
                VgnDiagramType.M => ("firebrick",  "#ffaaaa",    "kNm"),
                VgnDiagramType.W => ("seagreen",   "#aaffcc",    "m"),
                _                => ("black",      "lightgray",  "")
            };

            double maxAbs = samples.Max(s => Math.Abs(selector(s)));
            if (maxAbs < 1e-12) maxAbs = 1;

            double scaleY = MaxAmplitude / maxAbs;

            // Lokale helper: model → SVG-punt
            Punt P(VmnSampleResult s) =>
                new(s.X / vmn.L * SvgW, BaselineY - selector(s) * scaleY);

            var diagPts = samples.Select(P).ToList();

            // -- Gevuld vlak (gesloten pad: diagram heen + baseline terug) --
            var closedPts = diagPts
                .Concat(samples.AsEnumerable().Reverse()
                    .Select(s => new Punt(s.X / vmn.L * SvgW, BaselineY)))
                .ToList();

            var fillPath = SvgGenerator.MakePath(closedPts, close: true, fill: fill, stroke: "none");
            fillPath.FillOpacity = 0.35;

            // -- Diagram-lijn --
            var linePath = SvgGenerator.MakePath(diagPts, close: false, fill: "none", stroke: stroke);
            linePath.StrokeWidth = 1.5;

            // -- Balk-as (nul-lijn) --
            var baseline = new SvgLine
            {
                X1 = 0, Y1 = BaselineY, X2 = SvgW, Y2 = BaselineY,
                Stroke = "var(--neutral-foreground-rest)",
                StrokeWidth = 1.5
            };

            // -- Steunkringen op sample-punten --
            var circles = diagPts.Select(p =>
            {
                var c = new SvgCircle(p.X, p.Y, CircleR) { Fill = stroke, Opacity = 0.85 };
                return (BaseSvg)c;
            }).ToList();

            // -- Label: meest extreme waarde --
            var extSample = samples.MaxBy(s => Math.Abs(selector(s)))!;
            double extVal  = selector(extSample);
            var    extPt   = P(extSample);
            bool   isAbove = extVal > 0;

            var valLabel = new SvgText(
                text:              $"{extVal:0.###} {unit}",
                x:                 Math.Clamp(extPt.X, 25, SvgW - 25),
                y:                 extPt.Y + (isAbove ? -4 : 4),
                dominantBaseLine:  isAbove ? "auto" : "hanging",
                pts:               LabelPts)
            { Fill = stroke };

            // -- Diagram-type label linksboven --
            var typeLabel = new SvgText(
                text:             type.ToString(),
                x:                5,
                y:                5,
                anchor:           "start",
                dominantBaseLine: "hanging",
                pts:              LabelPts)
            { Fill = stroke };

            // -- Eindpunt-labels op x-as --
            var lblA = new SvgText("A", x: 0,    y: BaselineY + 4, dominantBaseLine: "hanging", pts: LabelPts - 2) { Anchor = "middle", Fill = "var(--neutral-foreground-rest)" };
            var lblB = new SvgText("B", x: SvgW, y: BaselineY + 4, dominantBaseLine: "hanging", pts: LabelPts - 2) { Anchor = "middle", Fill = "var(--neutral-foreground-rest)" };

            // -- Document samenstellen --
            var doc = new SvgDocument(SvgW, SvgH)
            {
                ViewBoxMinX   = -Padding,
                ViewBoxMinY   = -Padding,
                ViewBoxWidth  = SvgW + 2 * Padding,
                ViewBoxHeight = SvgH + 2 * Padding,
                Style         = "width:100%; overflow:visible"
            };

            doc.Children.Add(fillPath);
            doc.Children.Add(linePath);
            doc.Children.Add(baseline);
            foreach (var c in circles) doc.Children.Add(c);
            doc.Children.Add(valLabel);
            //doc.Children.Add(typeLabel);
            doc.Children.Add(lblA);
            doc.Children.Add(lblB);

            return doc;
        }
    }
}
