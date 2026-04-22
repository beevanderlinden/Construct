namespace Construct.Application.Services
{
    using Construct.Domain.Entities;
    using Construct.Domain.Extensions;
    using System.Globalization;
    using static Construct.Domain.Entities.SvgHelper;

    public static class PlaatSvgGenerator
    {
        /// <summary>
        /// Genereert een complete SVG-XML string voor het bovenaanzicht van een PlaatEntity.
        /// </summary>
        public static string GenereerBovenaanzichtSvgXml(
            PlaatEntity plaat,
            BoundingBox bb,
            double actualWidthPx,
            double actualHeightPx,
            string style = "width:auto; height:auto;")
        {
            SvgHelper svgHelper = new();
            SvgDocumentInfo? info = new()
            {
                Id = plaat.Id.ToString(),
                Title = plaat.Merk ?? "PLAAT",
                Description = "plaat bovenaanzicht",
                Label = plaat.Merk ?? "LABEL",
            };

            List<BaseSvg> svgPaths = GenereerBovenaanzicht(plaat);

            SvgHelper.SvgViewBox viewBox = new(0, 0, plaat.Lengte, plaat.Breedte);

            List<SvgDimLine> dimLines =
            [
                // B — breedte (verticaal, links)
                new()
                {
                    Mode = DimLineMode.Vertical,
                    X1 = 0, Y1 = 0, X2 = 0, Y2 = plaat.Breedte,
                    OffsetLines = 2,
                    Text = $"B={plaat.Breedte:0}",
                    StrokeColor = "var(--neutral-foreground-rest, black)",
                },
                // L — lengte (horizontaal, onder)
                new()
                {
                    Mode = DimLineMode.Horizontal,
                    X1 = 0, Y1 = plaat.Breedte, X2 = plaat.Lengte, Y2 = plaat.Breedte,
                    OffsetLines = -2,
                    Text = $"L={plaat.Lengte:0}",
                    StrokeColor = "var(--neutral-foreground-rest, black)",
                },
            ];

            var vbWithMargins = viewBox.WithMarginsByText(
                leftLines: 3,
                rightLines: 3,
                topLines: 3,
                bottomLines: 3,
                fontSizePx: 12,
                lineHeight: 1.5,
                actualWidthPx: actualWidthPx,
                actualHeightPx: actualHeightPx);

            return svgHelper.GetSvgStringOptimal(info, vbWithMargins, svgPaths, dimLines, [], actualWidthPx, actualHeightPx, style);
        }

        /// <summary>
        /// Genereert de SVG-elementen voor een bovenaanzicht van een PlaatEntity.
        /// De plaat wordt weergegeven als een polygon (rechthoek), zodat later
        /// andere vormen eenvoudig ondersteund kunnen worden.
        /// </summary>
        /// <param name="plaat">De plaat waarvoor het bovenaanzicht wordt gegenereerd.</param>
        /// <returns>Lijst van SVG-elementen die het bovenaanzicht vormen.</returns>
        public static List<BaseSvg> GenereerBovenaanzicht(PlaatEntity plaat)
        {
            var elementen = new List<BaseSvg>();

            double breedte = plaat.Breedte;
            double lengte = plaat.Lengte;

            // Opleggingen achter de plaat (zodat de plaat er bovenop ligt)
            elementen.AddRange(GenereerOpleggingElementen(plaat));

            // Rechthoekige polygon: punten met de klok mee startend linksboven
            string points = RectanglePoints(0, 0, lengte, breedte);

            var polygon = new SvgPolygon(points)
            {
                Fill = "var(--neutral-fill-rest, #e0e0e0)",
                Stroke = "var(--neutral-foreground-rest, black)",
                StrokeWidth = 1,
                Opacity = 1,
                VectorEffect = "non-scaling-stroke",
            };

            elementen.Add(polygon);

            // Strooklijnen boven op de plaat
            elementen.AddRange(GenereerStrookHartlijnen(plaat));

            return elementen;
        }

        /// <summary>
        /// Genereert SVG-elementen voor alle opleggingen van de plaat.
        /// Vrije opleggingen: blauwe strip buiten de rand.
        /// Ingeklemde opleggingen: rode strip + diagonale arceringslijnen.
        /// </summary>
        private static List<BaseSvg> GenereerOpleggingElementen(PlaatEntity plaat)
        {
            var result = new List<BaseSvg>();
            if (plaat.Opleggingen.Count == 0) return result;

            double lengte = plaat.Lengte;
            double breedte = plaat.Breedte;

            // Stripdikte proportioneel aan de kleinste afmeting (~3%)
            double stripDikte = Math.Min(lengte, breedte) * 0.05;
            double hatchSpacing = stripDikte * 0.9;
            double hatchLen = stripDikte * 1.1;

            foreach (var oplegging in plaat.Opleggingen)
            {
                bool ingeklemd = oplegging.Conditie == PlaatOpleggingConditie.Ingeklemd;
                string kleur = ingeklemd ? "firebrick" : "steelblue";

                // Bepaal begin- en eindpositie langs de rand
                bool alongY = oplegging.Rand is PlaatRand.Links or PlaatRand.Rechts;
                double maxPos = alongY ? breedte : lengte;
                double startPos, endPos;

                if (oplegging is PlaatPuntOplegging punt)
                {
                    double halfB = punt.Breedte / 2;
                    startPos = Math.Max(0, punt.PositieOpRand - halfB);
                    endPos   = Math.Min(maxPos, punt.PositieOpRand + halfB);
                }
                else
                {
                    startPos = 0;
                    endPos   = maxPos;
                }

                double segLen = endPos - startPos;
                if (segLen <= 0) continue;

                // Gekleurde strip buiten de plaat
                SvgRect strip = oplegging.Rand switch
                {
                    PlaatRand.Links  => new(-stripDikte, startPos, stripDikte, segLen),
                    PlaatRand.Rechts => new(lengte,      startPos, stripDikte, segLen),
                    PlaatRand.Boven  => new(startPos, -stripDikte, segLen,     stripDikte),
                    PlaatRand.Onder  => new(startPos,  breedte,    segLen,     stripDikte),
                    _                => new(0, 0, 0, 0),
                };
                strip.Fill         = kleur;
                strip.Stroke       = kleur;
                strip.StrokeWidth  = 0;
                strip.Opacity      = 0.55;
                strip.VectorEffect = null; // strip schaalt mee met world units
                result.Add(strip);

                // Inklemming: diagonale arceringslijnen (/////), buiten de strip
                if (ingeklemd)
                {
                    int nHatch = Math.Max(2, (int)(segLen / hatchSpacing));
                    for (int i = 0; i <= nHatch; i++)
                    {
                        double t = startPos + segLen / nHatch * i;
                        double x1, y1, x2, y2;

                        switch (oplegging.Rand)
                        {
                            case PlaatRand.Links:
                                x1 = -stripDikte * 0.1;      y1 = t;
                                x2 = -stripDikte * 0.1 - hatchLen; y2 = t + hatchLen;
                                break;
                            case PlaatRand.Rechts:
                                x1 = lengte + stripDikte * 0.1;            y1 = t;
                                x2 = lengte + stripDikte * 0.1 + hatchLen; y2 = t + hatchLen;
                                break;
                            case PlaatRand.Boven:
                                x1 = t;            y1 = -stripDikte * 0.1;
                                x2 = t + hatchLen; y2 = -stripDikte * 0.1 - hatchLen;
                                break;
                            case PlaatRand.Onder:
                                x1 = t;            y1 = breedte + stripDikte * 0.1;
                                x2 = t + hatchLen; y2 = breedte + stripDikte * 0.1 + hatchLen;
                                break;
                            default: continue;
                        }

                        result.Add(new SvgLine
                        {
                            X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                            Stroke = kleur,
                            StrokeWidth = 1.5,
                            VectorEffect = "non-scaling-stroke",
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Genereert SVG-hartlijnen voor alle PlaatStroken (gestippeld + label).
        /// </summary>
        private static List<BaseSvg> GenereerStrookHartlijnen(PlaatEntity plaat)
        {
            var result = new List<BaseSvg>();
            if (plaat.PlaatStroken.Count == 0) return result;

            foreach (var ps in plaat.PlaatStroken)
            {
                result.Add(new SvgLine
                {
                    X1 = ps.SvgX1, Y1 = ps.SvgY1,
                    X2 = ps.SvgX2, Y2 = ps.SvgY2,
                    Stroke = "var(--accent-foreground-rest, #0078d4)",
                    StrokeWidth = 1.5,
                    StrokeDashArray = "6 3",
                    VectorEffect = "non-scaling-stroke",
                });

                result.Add(new SvgText(
                    text: ps.Naam,
                    x: ps.SvgMidX,
                    y: ps.SvgMidY,
                    anchor: "middle",
                    dominantBaseLine: "middle",
                    pts: 11)
                {
                    Fill = "var(--accent-foreground-rest, #0078d4)",
                });
            }

            return result;
        }

        /// <summary>
        /// Maakt een points-string voor een rechthoek als polygon.
        /// </summary>
        private static string RectanglePoints(double x, double y, double breedte, double hoogte)
        {
            static string F(double v) => v.ToString(CultureInfo.InvariantCulture);

            double x1 = x;
            double y1 = y;
            double x2 = x + breedte;
            double y2 = y + hoogte;

            // linksboven, rechtsboven, rechtsonder, linksonder
            return $"{F(x1)},{F(y1)} {F(x2)},{F(y1)} {F(x2)},{F(y2)} {F(x1)},{F(y2)}";
        }
    }
}
