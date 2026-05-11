namespace Construct.Application.Services
{
    using Construct.Domain.Entities;
    using Construct.Domain.Extensions;
    using Eurocode.Belastingen;
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

            // Lijnlasten boven op de plaat
            elementen.AddRange(GenereerLijnlasten2D(plaat));

            // Puntlasten boven op de plaat
            elementen.AddRange(GenereerPuntlasten2D(plaat));

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
        /// Genereert SVG-elementen voor lijnlasten in 2D bovenaanzicht.
        /// </summary>
        private static List<BaseSvg> GenereerLijnlasten2D(PlaatEntity plaat)
        {
            var result = new List<BaseSvg>();
            if (plaat.Lijnlasten.Count == 0) return result;

            double L = plaat.Lengte;
            double B = plaat.Breedte;

            double maxMag = plaat.Lijnlasten.Max(ll => ll.MaxAbsMagnitude);
            if (maxMag < 1e-9) return result;

            // Maximale pijllengte: 12% van de kortste afmeting
            double maxArrowLen = Math.Min(L, B) * 0.12;
            double arrowHeadSize = maxArrowLen * 0.15;

            static string LoadColor(int nr) => nr switch
            {
                1 => "#E67700",
                2 => "#C00000",
                _ => "#7030A0",
            };

            foreach (var ll in plaat.Lijnlasten)
            {
                ll.BerekenPunten(L, B);

                double lenA = Math.Abs(ll.MagnitudeA) / maxMag * maxArrowLen;
                double lenB = Math.Abs(ll.MagnitudeB) / maxMag * maxArrowLen;
                bool neerwaarts = ll.IsNeerwaarts;
                string kleur = LoadColor(ll.BelastingGevalNr);

                double sx = ll.S.Lx, sy = ll.S.By;
                double ex = ll.E.Lx, ey = ll.E.By;

                // Richting haaks op de lijn (naar binnen de plaat)
                double dx = ex - sx, dy = ey - sy;
                double len = Math.Sqrt(dx * dx + dy * dy);
                if (len < 1e-9) continue;

                double nx = -dy / len, ny = dx / len;  // 90° rotatie

                // Bepaal richting naar binnen plaat
                double midX = (sx + ex) / 2, midY = (sy + ey) / 2;
                double centerX = L / 2, centerY = B / 2;
                if ((midX - centerX) * nx + (midY - centerY) * ny < 0)
                {
                    nx = -nx; ny = -ny;
                }

                // Trapezoid/rechthoek voor het belastingdiagram
                result.Add(new SvgPolygon
                {
                    Points = $"{sx},{sy} {sx + nx * lenA},{sy + ny * lenA} {ex + nx * lenB},{ey + ny * lenB} {ex},{ey}",
                    Fill = kleur + "30",
                    Stroke = kleur,
                    StrokeWidth = 0.75,
                    VectorEffect = "non-scaling-stroke",
                });

                // Pijlen
                double segLen = len;
                int nPijlen = Math.Max(2, Math.Min(8, (int)(segLen / 400.0) + 2));

                for (int i = 0; i <= nPijlen; i++)
                {
                    double t = (double)i / nPijlen;
                    double px = sx + t * dx;
                    double py = sy + t * dy;
                    double pLen = lenA + t * (lenB - lenA);
                    if (pLen < 1e-9) continue;

                    // Pijl van (px, py) in richting (nx, ny)
                    double tipX = px + nx * pLen;
                    double tipY = py + ny * pLen;

                    result.Add(new SvgLine { X1 = px, Y1 = py, X2 = tipX, Y2 = tipY, Stroke = kleur, StrokeWidth = 0.85, VectorEffect = "non-scaling-stroke" });

                    // Pijlpunt
                    double headX = tipX - nx * arrowHeadSize;
                    double headY = tipY - ny * arrowHeadSize;
                    result.Add(new SvgLine { X1 = tipX, Y1 = tipY, X2 = headX - ny * arrowHeadSize * 0.4, Y2 = headY + nx * arrowHeadSize * 0.4, Stroke = kleur, StrokeWidth = 0.85, VectorEffect = "non-scaling-stroke" });
                    result.Add(new SvgLine { X1 = tipX, Y1 = tipY, X2 = headX + ny * arrowHeadSize * 0.4, Y2 = headY - nx * arrowHeadSize * 0.4, Stroke = kleur, StrokeWidth = 0.85, VectorEffect = "non-scaling-stroke" });
                }

                // Label
                double midLen = Math.Max(lenA, lenB);
                double labX = midX + nx * midLen * 0.5;
                double labY = midY + ny * midLen * 0.5;

                string qTekst = Math.Abs(ll.MagnitudeA - ll.MagnitudeB) < 1e-9
                    ? $"q={Math.Abs(ll.MagnitudeA):0.##}"
                    : $"q={Math.Abs(ll.MagnitudeA):0.##}–{Math.Abs(ll.MagnitudeB):0.##}";

                string labelTekst = string.IsNullOrWhiteSpace(ll.Naam) ? qTekst : $"{ll.Naam}:{qTekst}";

                result.Add(new SvgText(labelTekst, labX, labY, scale: 1)
                {
                    Anchor = "middle",
                    DominantBaseLine = "middle",
                    Fill = kleur,
                    Pts = 9,
                });
            }

            return result;
        }

        /// <summary>
        /// Genereert SVG-elementen voor puntlasten in 2D bovenaanzicht.
        /// </summary>
        private static List<BaseSvg> GenereerPuntlasten2D(PlaatEntity plaat)
        {
            var result = new List<BaseSvg>();
            if (plaat.Puntlasten.Count == 0) return result;

            double L = plaat.Lengte;
            double B = plaat.Breedte;

            double maxMag = plaat.Puntlasten.Max(pl => pl.AbsMagnitude);
            if (maxMag < 1e-9) return result;

            // Maximale pijllengte: 15% van de kortste afmeting
            double maxArrowLen = Math.Min(L, B) * 0.15;
            double arrowHeadSize = maxArrowLen * 0.18;
            double circlRadius = maxArrowLen * 0.08;

            static string LoadColor(int nr) => nr switch
            {
                1 => "#E67700",
                2 => "#C00000",
                _ => "#7030A0",
            };

            foreach (var pl in plaat.Puntlasten)
            {
                double pLen = Math.Abs(pl.Magnitude) / maxMag * maxArrowLen;
                string kleur = LoadColor(pl.BelastingGevalNr);

                double px = pl.PosX;
                double py = pl.PosY;

                // Pijl wijst naar beneden (y-richting)
                double tipY = py + pLen;

                // Schacht
                result.Add(new SvgLine { X1 = px, Y1 = py, X2 = px, Y2 = tipY, Stroke = kleur, StrokeWidth = 1.2, VectorEffect = "non-scaling-stroke" });

                // Pijlpunt (naar beneden)
                double headY = tipY - arrowHeadSize;
                result.Add(new SvgLine { X1 = px, Y1 = tipY, X2 = px - arrowHeadSize * 0.4, Y2 = headY, Stroke = kleur, StrokeWidth = 1.2, VectorEffect = "non-scaling-stroke" });
                result.Add(new SvgLine { X1 = px, Y1 = tipY, X2 = px + arrowHeadSize * 0.4, Y2 = headY, Stroke = kleur, StrokeWidth = 1.2, VectorEffect = "non-scaling-stroke" });

                // Cirkel bij de basis van de pijl
                result.Add(new SvgCircle
                {
                    Cx = px,
                    Cy = py,
                    R = circlRadius,
                    Fill = kleur + "40",
                    Stroke = kleur,
                    StrokeWidth = 0.75,
                    VectorEffect = "non-scaling-stroke",
                });

                // Label
                double labY = py - circlRadius - 8;
                string pTekst = $"P={Math.Abs(pl.Magnitude):0.##}";
                string labelTekst = string.IsNullOrWhiteSpace(pl.Naam) ? pTekst : $"{pl.Naam}:{pTekst}";

                result.Add(new SvgText(labelTekst, px, labY, scale: 1)
                {
                    Anchor = "middle",
                    DominantBaseLine = "auto",
                    Fill = kleur,
                    Pts = 9,
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

        // ════════════════════════════════════════════════════════════════
        //  ISOMETRISCHE WEERGAVE
        //
        //  Coördinatenstelsel (rechthoekige plaat):
        //    lx  : 0 .. Lengte  (Links → Rechts, naar rechts-voor in iso)
        //    by  : 0 .. Breedte (Boven → Onder,  naar links-voor in iso)
        //    z   : 0 .. Dikte   (omhoog)
        //
        //  Projectie (standaard 30°-assen):
        //    svgX = (lx − by) × cos30
        //    svgY = (lx + by) × sin30 − z
        // ════════════════════════════════════════════════════════════════

        private static readonly double Iso_Cos30 = Math.Cos(30.0 * Math.PI / 180.0); // ≈ 0.8660
        private static readonly double Iso_Sin30 = 0.5;

        private static (double x, double y) Iso(double lx, double by, double z = 0)
            => ((lx - by) * Iso_Cos30, (lx + by) * Iso_Sin30 - z);

        /// <summary>Gesloten ISO-vlak als SvgPath.</summary>
        private static SvgPath IsoFace(
            IEnumerable<(double lx, double by, double z)> punten,
            string fill,
            string stroke = "black",
            double strokeWidth = 1)
        {
            var sb = new System.Text.StringBuilder();
            bool first = true;
            foreach (var (lx, by, z) in punten)
            {
                var (sx, sy) = Iso(lx, by, z);
                sb.Append(first
                    ? $"M {sx.ToSvg()} {sy.ToSvg()} "
                    : $"L {sx.ToSvg()} {sy.ToSvg()} ");
                first = false;
            }
            sb.Append('Z');
            return new SvgPath { D = sb.ToString(), Fill = fill, Stroke = stroke, StrokeWidth = strokeWidth };
        }

        /// <summary>Lijn tussen twee ISO-punten.</summary>
        private static SvgLine IsoLine(
            double lx1, double by1, double z1,
            double lx2, double by2, double z2,
            string stroke = "black",
            double strokeWidth = 1,
            string strokeDashArray = "")
        {
            var (sx1, sy1) = Iso(lx1, by1, z1);
            var (sx2, sy2) = Iso(lx2, by2, z2);
            return new SvgLine
            {
                X1 = sx1, Y1 = sy1,
                X2 = sx2, Y2 = sy2,
                Stroke = stroke,
                StrokeWidth = strokeWidth,
                StrokeDashArray = strokeDashArray,
            };
        }

        private static readonly string[] IsoStrookKleuren =
        [
            "#4472C4", "#ED7D31", "#70AD47",
            "#FF0000", "#7030A0", "#00B0F0",
        ];

        /// <summary>
        /// Genereert alle SVG-elementen voor de isometrische weergave.
        /// </summary>
        /// <param name="toonDikte">False (standaard) = plat vlak op z=0. True = volledig 3D blok met zijvlakken en dikte-maat.</param>
        public static List<BaseSvg> GenereerIsometrischElements(
            PlaatEntity plaat,
            bool toonStroken = true,
            bool toonOpleggingen = true,
            bool toonDikte = false,
            bool toonMomentlijn = true)
        {
            var elements = new List<BaseSvg>();

            double L = plaat.Lengte;
            double B = plaat.Breedte;
            double D = plaat.MainSlab?.Dikte ?? 200;

            // z-niveau waarop het bovenvlak en stroken worden getekend
            double drawZ = toonDikte ? D : 0;

            // ── 1. Vlakken ──
            if (toonDikte)
            {
                // Rechter zijvlak (lx = L) — donkerste toon
                elements.Add(IsoFace(
                    [(L, 0, 0), (L, B, 0), (L, B, D), (L, 0, D)],
                    fill: "#787878", stroke: "black", strokeWidth: 1));

                // Voorvlak (by = 0) — middeltoon
                elements.Add(IsoFace(
                    [(0, 0, 0), (L, 0, 0), (L, 0, D), (0, 0, D)],
                    fill: "#AAAAAA", stroke: "black", strokeWidth: 1));
            }

            // Bovenvlak (z = drawZ) — lichtste toon (of plat vlak)
            elements.Add(IsoFace(
                [(0, 0, drawZ), (L, 0, drawZ), (L, B, drawZ), (0, B, drawZ)],
                fill: "#D4D4D4", stroke: "black", strokeWidth: 1));

            // ── 2. Opleggingen langs de randen (op drawZ-niveau) ──
            if (toonOpleggingen)
            {
                foreach (var opl in plaat.Opleggingen)
                {
                    string kleur = opl.Conditie == PlaatOpleggingConditie.Ingeklemd
                        ? "#C00000"   // rood = ingeklemd
                        : "#0070C0";  // blauw = vrij in rotatie
                    const double opleggDikte = 4;

                    if (opl is PlaatPuntOplegging pOpl)
                    {
                        double pos  = pOpl.PositieOpRand;
                        double half = pOpl.Breedte / 2;
                        var lijn = opl.Rand switch
                        {
                            PlaatRand.Links  => IsoLine(0, Math.Max(0, pos - half), drawZ, 0, Math.Min(B, pos + half), drawZ, kleur, opleggDikte),
                            PlaatRand.Rechts => IsoLine(L, Math.Max(0, pos - half), drawZ, L, Math.Min(B, pos + half), drawZ, kleur, opleggDikte),
                            PlaatRand.Boven  => IsoLine(Math.Max(0, pos - half), 0, drawZ, Math.Min(L, pos + half), 0, drawZ, kleur, opleggDikte),
                            PlaatRand.Onder  => IsoLine(Math.Max(0, pos - half), B, drawZ, Math.Min(L, pos + half), B, drawZ, kleur, opleggDikte),
                            _                => IsoLine(0, 0, drawZ, 0, 0, drawZ, kleur, opleggDikte),
                        };
                        elements.Add(lijn);
                    }
                    else
                    {
                        var lijn = opl.Rand switch
                        {
                            PlaatRand.Links  => IsoLine(0, 0, drawZ, 0, B, drawZ, kleur, opleggDikte),
                            PlaatRand.Rechts => IsoLine(L, 0, drawZ, L, B, drawZ, kleur, opleggDikte),
                            PlaatRand.Boven  => IsoLine(0, 0, drawZ, L, 0, drawZ, kleur, opleggDikte),
                            PlaatRand.Onder  => IsoLine(0, B, drawZ, L, B, drawZ, kleur, opleggDikte),
                            _                => IsoLine(0, 0, drawZ, 0, 0, drawZ, kleur, opleggDikte),
                        };
                        elements.Add(lijn);
                    }
                }
            }

            // ── 3. Stroken op het bovenvlak ──
            if (toonStroken && plaat.PlaatStroken.Count > 0)
            {
                int kleurIdx = 0;

                foreach (var ps in plaat.PlaatStroken)
                {
                    string kleur = IsoStrookKleuren[kleurIdx++ % IsoStrookKleuren.Length];
                    double halfBreed = ps.InvloedsBreedteMm / 2.0;

                    double lx1, by1, lx2, by2;
                    if (ps.Richting == PlaatStrookRichting.LangsLengte)
                    {
                        lx1 = ps.StartMm;
                        lx2 = ps.EindMm;
                        by1 = Math.Max(0, ps.PositieDwars - halfBreed);
                        by2 = Math.Min(B, ps.PositieDwars + halfBreed);
                    }
                    else
                    {
                        lx1 = Math.Max(0, ps.PositieDwars - halfBreed);
                        lx2 = Math.Min(L, ps.PositieDwars + halfBreed);
                        by1 = ps.StartMm;
                        by2 = ps.EindMm;
                    }

                    // Translucent overlay op bovenvlak — "40" ≈ 25 % alpha
                    elements.Add(IsoFace(
                        [(lx1, by1, drawZ), (lx2, by1, drawZ), (lx2, by2, drawZ), (lx1, by2, drawZ)],
                        fill: kleur + "40", stroke: kleur, strokeWidth: 0.5));

                    // Gestippelde hartlijn iets boven het vlak
                    if (ps.Richting == PlaatStrookRichting.LangsLengte)
                        elements.Add(IsoLine(ps.StartMm, ps.PositieDwars, drawZ + 1,
                                             ps.EindMm,   ps.PositieDwars, drawZ + 1,
                                             kleur, 1.5, "20,10"));
                    else
                        elements.Add(IsoLine(ps.PositieDwars, ps.StartMm, drawZ + 1,
                                             ps.PositieDwars, ps.EindMm,   drawZ + 1,
                                             kleur, 1.5, "20,10"));

                    // Naam-label op het bovenvlak
                    var (midLx, midBy) = ps.Richting == PlaatStrookRichting.LangsLengte
                        ? ((lx1 + lx2) / 2, ps.PositieDwars)
                        : (ps.PositieDwars, (by1 + by2) / 2);

                    var (lbX, lbY) = Iso(midLx, midBy, drawZ + 40);
                    elements.Add(new SvgText(ps.Naam, lbX, lbY, scale: 1)
                    {
                        Anchor = "middle",
                        DominantBaseLine = "auto",
                        Fill = kleur,
                        Pts = 10,
                    });
                }
            }

            // ── 4. Maatvoering (L, B, D) ──
            const string dimStroke = "var(--neutral-foreground-rest, black)";
            double dimGap = Math.Max(D * 0.5, 100);

            // Lengte — langs de voorrand (by=0), offset in het horizontale vlak (-by richting)
            {
                // -by richting in schermcoördinaten: (+Cos30, -Sin30) × dimGap
                double offX =  dimGap * Iso_Cos30;
                double offY = -dimGap * Iso_Sin30;

                var (aX1, aY1) = Iso(0, 0, drawZ);
                var (aX2, aY2) = Iso(L, 0, drawZ);
                double ox1 = aX1 + offX, oy1 = aY1 + offY;
                double ox2 = aX2 + offX, oy2 = aY2 + offY;
                double mX  = (ox1 + ox2) / 2;
                double mY  = (oy1 + oy2) / 2;

                elements.Add(new SvgLine { X1 = aX1, Y1 = aY1, X2 = ox1, Y2 = oy1, Stroke = dimStroke, StrokeWidth = 0.5, StrokeDashArray = "4,3" });
                elements.Add(new SvgLine { X1 = aX2, Y1 = aY2, X2 = ox2, Y2 = oy2, Stroke = dimStroke, StrokeWidth = 0.5, StrokeDashArray = "4,3" });
                elements.Add(new SvgLine { X1 = ox1, Y1 = oy1, X2 = ox2, Y2 = oy2, Stroke = dimStroke, StrokeWidth = 0.75 });
                elements.Add(new SvgText($"L = {L:0} mm", mX, mY, scale: 1)
                {
                    Anchor = "middle",
                    DominantBaseLine = "auto",
                    Pts = 10,
                    DY = -6,
                });
            }

            // Breedte — rechts langs de zijrand (lx=L, z=drawZ), offset naar rechts-onder in iso
            {
                double offX = dimGap * Iso_Cos30;
                double offY = dimGap * Iso_Sin30;

                var (aX1, aY1) = Iso(L, 0, drawZ);
                var (aX2, aY2) = Iso(L, B, drawZ);
                double ox1 = aX1 + offX, oy1 = aY1 + offY;
                double ox2 = aX2 + offX, oy2 = aY2 + offY;
                double mX  = (ox1 + ox2) / 2;
                double mY  = (oy1 + oy2) / 2;

                elements.Add(new SvgLine { X1 = aX1, Y1 = aY1, X2 = ox1, Y2 = oy1, Stroke = dimStroke, StrokeWidth = 0.5, StrokeDashArray = "4,3" });
                elements.Add(new SvgLine { X1 = aX2, Y1 = aY2, X2 = ox2, Y2 = oy2, Stroke = dimStroke, StrokeWidth = 0.5, StrokeDashArray = "4,3" });
                elements.Add(new SvgLine { X1 = ox1, Y1 = oy1, X2 = ox2, Y2 = oy2, Stroke = dimStroke, StrokeWidth = 0.75 });
                elements.Add(new SvgText($"B = {B:0} mm", mX, mY, scale: 1)
                {
                    Anchor = "start",
                    DominantBaseLine = "middle",
                    Pts = 10,
                    DX = 8,
                });
            }

            // Dikte — langs de voorste verticale rib (lx=L, by=0), alleen als toonDikte
            if (toonDikte)
            {
                double offX = dimGap * Iso_Cos30;

                var (aX1, aY1) = Iso(L, 0, 0);
                var (aX2, aY2) = Iso(L, 0, D);
                double ox = aX1 + offX;
                double mY = (aY1 + aY2) / 2;

                elements.Add(new SvgLine { X1 = aX1, Y1 = aY1, X2 = ox,  Y2 = aY1, Stroke = dimStroke, StrokeWidth = 0.5, StrokeDashArray = "4,3" });
                elements.Add(new SvgLine { X1 = aX2, Y1 = aY2, X2 = ox,  Y2 = aY2, Stroke = dimStroke, StrokeWidth = 0.5, StrokeDashArray = "4,3" });
                elements.Add(new SvgLine { X1 = ox,  Y1 = aY1, X2 = ox,  Y2 = aY2, Stroke = dimStroke, StrokeWidth = 0.75 });
                elements.Add(new SvgText($"d = {D:0} mm", ox + 6, mY, scale: 1)
                {
                    Anchor = "start",
                    DominantBaseLine = "middle",
                    Pts = 10,
                });

                // Materiaal-label op het voorvlak (alleen zichtbaar als het vlak er is)
                var (labX, labY) = Iso(L / 2, 0, D / 2);
                var matNaam = plaat.Materiaal?.UserFriendlyName ?? "beton";
                elements.Add(new SvgText(matNaam, labX, labY, scale: 1)
                {
                    Anchor = "middle",
                    DominantBaseLine = "middle",
                    Fill = "white",
                    Pts = 10,
                });
            }

            // ── 5. Lijnlasten ──
            elements.AddRange(GenereerLijnlastenIso(plaat, drawZ));

            // ── 6. Puntlasten ──
            elements.AddRange(GenereerPuntlastenIso(plaat, drawZ));

            // ── 7. Momentlijn per strook ──
            if (toonMomentlijn)
                elements.AddRange(GenereerMomentlijnIso(plaat, drawZ));

            return elements;
        }

        /// <summary>
        /// Genereert SVG-elementen voor alle <see cref="PlaatRandLijnlast"/>-objecten in isometrische weergave.
        /// Pijlen worden relatief geschaald aan de grootste magnitude in de lijst.
        /// </summary>
        private static List<BaseSvg> GenereerLijnlastenIso(PlaatEntity plaat, double drawZ)
        {
            var elements = new List<BaseSvg>();
            if (plaat.Lijnlasten.Count == 0) return elements;

            double L = plaat.Lengte;
            double B = plaat.Breedte;

            double maxMag = plaat.Lijnlasten.Max(ll => ll.MaxAbsMagnitude);
            if (maxMag < 1e-9) return elements;

            // Maximale pijlhoogte: 20 % van de kortste plaatafmeting
            double maxArrowHeight = Math.Min(L, B) * 0.20;
            double arrowHeadSize  = maxArrowHeight * 0.10;

            // Vaste kleuren per belastinggevalnummer (BG1=oranje, BG2=rood, rest=paars)
            static string LoadColor(int nr) => nr switch
            {
                1 => "#E67700",
                2 => "#C00000",
                _ => "#7030A0",
            };

            foreach (var ll in plaat.Lijnlasten)
            {
                // Zorg dat S/E actueel zijn
                ll.BerekenPunten(L, B);

                double hA = Math.Abs(ll.MagnitudeA) / maxMag * maxArrowHeight;
                double hB = Math.Abs(ll.MagnitudeB) / maxMag * maxArrowHeight;
                bool neerwaarts = ll.IsNeerwaarts;

                string kleur = LoadColor(ll.BelastingGevalNr);

                double sLx = ll.S.Lx, sBy = ll.S.By;
                double eLx = ll.E.Lx, eBy = ll.E.By;

                // ── Trapezoid (belastingdiagram) ──
                // Basislijn boven het plaatoppervlak; pijlpunten op drawZ-niveau
                elements.Add(IsoFace(
                    [(sLx, sBy, drawZ),
                     (sLx, sBy, drawZ + hA),
                     (eLx, eBy, drawZ + hB),
                     (eLx, eBy, drawZ)],
                    fill:        kleur + "28",   // ~16 % alpha
                    stroke:      kleur,
                    strokeWidth: 0.75));

                // ── Pijlen ──
                double segLen = Math.Sqrt((eLx - sLx) * (eLx - sLx) + (eBy - sBy) * (eBy - sBy));
                int nPijlen = Math.Max(2, Math.Min(8, (int)(segLen / 400.0) + 2));

                for (int i = 0; i <= nPijlen; i++)
                {
                    double t  = (double)i / nPijlen;
                    double pLx = sLx + t * (eLx - sLx);
                    double pBy = sBy + t * (eBy - sBy);
                    double h  = hA + t * (hB - hA);
                    if (h < 1e-9) continue;

                    double zTail = drawZ + h;
                    double zTip  = drawZ;

                    // Schacht: tail → tip
                    elements.Add(IsoLine(pLx, pBy, zTail, pLx, pBy, zTip, kleur, 0.85));

                    // Pijlpunt (ISO: z-richting is (0, −1) in schermcoördinaten)
                    var (tipX, tipY) = Iso(pLx, pBy, zTip);
                    // Vleugels gaan terug richting schacht (+y in screen = richting tail)
                    double wingDy = arrowHeadSize * Iso_Sin30 * 2;   // terug langs z-as
                    double wingDx = arrowHeadSize * Iso_Cos30 * 0.5; // lichte spreiding
                    if (!neerwaarts)
                    {
                        // Opwaarts: tip is boven, vleugels gaan omlaag in screen
                        var (tipXup, tipYup) = Iso(pLx, pBy, zTail);
                        elements.Add(new SvgLine { X1 = tipXup, Y1 = tipYup, X2 = tipXup - wingDx, Y2 = tipYup + wingDy, Stroke = kleur, StrokeWidth = 0.85 });
                        elements.Add(new SvgLine { X1 = tipXup, Y1 = tipYup, X2 = tipXup + wingDx, Y2 = tipYup + wingDy, Stroke = kleur, StrokeWidth = 0.85 });
                    }
                    else
                    {
                        // Neerwaarts: tip is beneden, vleugels gaan omhoog in screen
                        elements.Add(new SvgLine { X1 = tipX, Y1 = tipY, X2 = tipX - wingDx, Y2 = tipY - wingDy, Stroke = kleur, StrokeWidth = 0.85 });
                        elements.Add(new SvgLine { X1 = tipX, Y1 = tipY, X2 = tipX + wingDx, Y2 = tipY - wingDy, Stroke = kleur, StrokeWidth = 0.85 });
                    }
                }

                // ── Label ──
                double midLx = (sLx + eLx) / 2;
                double midBy = (sBy + eBy) / 2;
                double topH  = Math.Max(hA, hB);
                var (labX, labY) = Iso(midLx, midBy, drawZ + topH);

                string qTekst = Math.Abs(ll.MagnitudeA - ll.MagnitudeB) < 1e-9
                    ? $"q = {Math.Abs(ll.MagnitudeA):0.##} kN/m"
                    : $"q = {Math.Abs(ll.MagnitudeA):0.##}\u2013{Math.Abs(ll.MagnitudeB):0.##} kN/m";

                string labelTekst = string.IsNullOrWhiteSpace(ll.Naam)
                    ? qTekst
                    : $"{ll.Naam}: {qTekst}";

                elements.Add(new SvgText(labelTekst, labX, labY, scale: 1)
                {
                    Anchor          = "middle",
                    DominantBaseLine = "auto",
                    Fill            = kleur,
                    Pts             = 9,
                    DY              = -5,
                });
            }

            return elements;
        }

        /// <summary>
        /// Genereert SVG-elementen voor alle <see cref="PlaatPuntlast"/>-objecten in isometrische weergave.
        /// Pijlen worden relatief geschaald aan de grootste magnitude in de lijst.
        /// </summary>
        private static List<BaseSvg> GenereerPuntlastenIso(PlaatEntity plaat, double drawZ)
        {
            var elements = new List<BaseSvg>();
            if (plaat.Puntlasten.Count == 0) return elements;

            double L = plaat.Lengte;
            double B = plaat.Breedte;

            double maxMag = plaat.Puntlasten.Max(pl => pl.AbsMagnitude);
            if (maxMag < 1e-9) return elements;

            // Maximale pijlhoogte: 20 % van de kortste plaatafmeting
            double maxArrowHeight = Math.Min(L, B) * 0.20;
            double arrowHeadSize  = maxArrowHeight * 0.12;

            // Vaste kleuren per belastinggevalnummer (BG1=oranje, BG2=rood, rest=paars)
            static string LoadColor(int nr) => nr switch
            {
                1 => "#E67700",
                2 => "#C00000",
                _ => "#7030A0",
            };

            foreach (var pl in plaat.Puntlasten)
            {
                double h = Math.Abs(pl.Magnitude) / maxMag * maxArrowHeight;
                bool neerwaarts = pl.IsNeerwaarts;
                string kleur = LoadColor(pl.BelastingGevalNr);

                double pLx = pl.PosX;
                double pBy = pl.PosY;

                double zTail = neerwaarts ? drawZ + h : drawZ;
                double zTip  = neerwaarts ? drawZ : drawZ + h;

                // Schacht: tail → tip
                elements.Add(IsoLine(pLx, pBy, zTail, pLx, pBy, zTip, kleur, 1.2));

                // Pijlpunt (driehoek)
                var (tipX, tipY) = Iso(pLx, pBy, zTip);
                double wingDy = arrowHeadSize * Iso_Sin30 * 2;
                double wingDx = arrowHeadSize * Iso_Cos30 * 0.5;

                if (!neerwaarts)
                {
                    // Opwaarts: tip is boven, vleugels gaan omlaag in screen
                    var (tipXup, tipYup) = Iso(pLx, pBy, zTail);
                    elements.Add(new SvgLine { X1 = tipXup, Y1 = tipYup, X2 = tipXup - wingDx, Y2 = tipYup + wingDy, Stroke = kleur, StrokeWidth = 1.2 });
                    elements.Add(new SvgLine { X1 = tipXup, Y1 = tipYup, X2 = tipXup + wingDx, Y2 = tipYup + wingDy, Stroke = kleur, StrokeWidth = 1.2 });
                }
                else
                {
                    // Neerwaarts: tip is beneden, vleugels gaan omhoog in screen
                    elements.Add(new SvgLine { X1 = tipX, Y1 = tipY, X2 = tipX - wingDx, Y2 = tipY - wingDy, Stroke = kleur, StrokeWidth = 1.2 });
                    elements.Add(new SvgLine { X1 = tipX, Y1 = tipY, X2 = tipX + wingDx, Y2 = tipY - wingDy, Stroke = kleur, StrokeWidth = 1.2 });
                }

                // ── Label ──
                double topH = neerwaarts ? h : h;
                var (labX, labY) = Iso(pLx, pBy, drawZ + topH);

                string pTekst = $"P = {Math.Abs(pl.Magnitude):0.##} kN";
                string labelTekst = string.IsNullOrWhiteSpace(pl.Naam)
                    ? pTekst
                    : $"{pl.Naam}: {pTekst}";

                elements.Add(new SvgText(labelTekst, labX, labY, scale: 1)
                {
                    Anchor          = "middle",
                    DominantBaseLine = "auto",
                    Fill            = kleur,
                    Pts             = 9,
                    DY              = -5,
                });
            }

            return elements;
        }

        /// <summary>
        /// Genereert SVG-elementen voor de momentlijn van elke <see cref="PlaatStrook"/> in isometrische weergave.
        /// De momentwaarden worden als z-hoogte boven (of onder) het plaatoppervlak geprojecteerd.
        /// </summary>
        private static List<BaseSvg> GenereerMomentlijnIso(PlaatEntity plaat, double drawZ)
        {
            var elements = new List<BaseSvg>();
            if (plaat.PlaatStroken.Count == 0) return elements;

            // Maximale visuele hoogte: 20 % van de kortste plaatafmeting
            double maxMomentHeight = Math.Min(plaat.Lengte, plaat.Breedte) * 0.20;

            int kleurIdx = 0;
            foreach (var ps in plaat.PlaatStroken)
            {
                var beam = ps.Strook?.Beam;
                if (beam == null) continue;

                var results = beam.ResultCollection
                    .ForCombinationType(BelastingCombinatieTypeEnum.Fundamenteel_B)
                    .ToList();
                if (results.Count == 0) continue;

                // Verzamel alle momentpunten voor schaalberekening
                var allPoints = results
                    .SelectMany(r => r.MomentDiagram)
                    .ToList();
                if (allPoints.Count == 0) continue;

                double maxAbsM = allPoints.Max(p => Math.Abs(p.M));
                if (maxAbsM < 1e-6) continue;

                double scaleZ = maxMomentHeight / maxAbsM;
                string kleur  = IsoStrookKleuren[kleurIdx++ % IsoStrookKleuren.Length];

                // Mapping: liggerposition t (m) → ISO-coördinaten (lx, by) in mm
                (double lx, double by) PosToIso(double tM)
                {
                    double tMm = tM * 1000.0;
                    return ps.Richting == PlaatStrookRichting.LangsLengte
                        ? (ps.StartMm + tMm, ps.PositieDwars)
                        : (ps.PositieDwars,  ps.StartMm + tMm);
                }

                // Teken per result een gesloten polygoon (baseline + momentlijn)
                foreach (var r in results)
                {
                    if (r.MomentDiagram.Count == 0) continue;

                    // Punten: start op baseline, door momentlijn, terug langs baseline
                    var (startLx, startBy) = PosToIso(0);
                    var (eindLx,  eindBy)  = PosToIso(beam.Length);

                    var vlakPunten = new List<(double lx, double by, double z)>
                    {
                        (startLx, startBy, drawZ)
                    };

                    foreach (var (t, M) in r.MomentDiagram)
                    {
                        var (lx, by) = PosToIso(t);
                        vlakPunten.Add((lx, by, drawZ + M * scaleZ));
                    }

                    vlakPunten.Add((eindLx, eindBy, drawZ));

                    elements.Add(IsoFace(vlakPunten, kleur + "30", kleur, 0.75));
                }

                // Basislijn (gestippeld)
                {
                    var (bLx0, bBy0) = PosToIso(0);
                    var (bLxN, bByN) = PosToIso(beam.Length);
                    elements.Add(IsoLine(bLx0, bBy0, drawZ, bLxN, bByN, drawZ, kleur, 0.5, "6,4"));
                }

                // Min/max labels
                var minP = allPoints.OrderBy(p => p.M).First();
                var maxP = allPoints.OrderByDescending(p => p.M).First();

                foreach (var (t, M) in new[] { (minP.x, minP.M), (maxP.x, maxP.M) })
                {
                    if (Math.Abs(M) < 0.001) continue;
                    var (lx, by) = PosToIso(t);
                    var (labX, labY) = Iso(lx, by, drawZ + M * scaleZ);
                    elements.Add(new SvgText($"{M:0.#}", labX, labY, scale: 1)
                    {
                        Anchor           = "middle",
                        DominantBaseLine = M < 0 ? "hanging" : "auto",
                        Fill             = kleur,
                        Pts              = 9,
                        DY               = M < 0 ? 4 : -4,
                    });
                }
            }

            return elements;
        }

        /// <summary>
        /// Genereert een volledige SVG-string met de isometrische weergave van een <see cref="PlaatEntity"/>.
        /// </summary>
        public static string GenereerIsometrischSvgXml(
            PlaatEntity plaat,
            BoundingBox bb,
            double widthPx,
            double heightPx,
            bool toonStroken = true,
            bool toonOpleggingen = true,
            bool toonDikte = false,
            bool toonMomentlijn = true,
            string style = "width:auto; height:auto;")
        {
            var svgHelper = new SvgHelper();

            var info = new SvgDocumentInfo
            {
                Id          = plaat.Id.ToString(),
                Title       = plaat.Merk ?? "PLAAT",
                Description = "isometrische weergave",
                Label       = plaat.Merk ?? "LABEL",
            };

            double L = plaat.Lengte;
            double B = plaat.Breedte;
            double D = plaat.MainSlab?.Dikte ?? 200;
            double dimGap = Math.Max(D * 0.5, 100);
            double drawZ  = toonDikte ? D : 0;

            var elements = GenereerIsometrischElements(plaat, toonStroken, toonOpleggingen, toonDikte, toonMomentlijn);

            // Bounding box van het ISO-model incl. maatvoeringsruimte
            // L-offset gaat in -by richting → bovenrand (minY) schuift omhoog met Sin30 × dimGap
            double maxArrowH = plaat.Lijnlasten.Count > 0
                ? plaat.Lijnlasten.Max(ll => ll.MaxAbsMagnitude) > 1e-9
                    ? Math.Min(L, B) * 0.20
                    : 0
                : 0;
            double minX = -B * Iso_Cos30 - dimGap * 0.5;
            double maxX =  L * Iso_Cos30 + dimGap * 2.5;
            double minY =  drawZ == 0
                ? -(dimGap * Iso_Sin30 + dimGap * 0.5 + maxArrowH)
                : -(D + dimGap + maxArrowH);
            double maxY =  (L + B) * Iso_Sin30 + dimGap * 0.5;

            bb.MinX      = minX;
            bb.MinY      = minY;
            bb.MaxXValue = maxX;
            bb.MaxYValue = maxY;

            var viewBox = new SvgHelper.SvgViewBox(minX, minY, maxX - minX, maxY - minY);
            var vbWithMargins = viewBox.WithMarginsByText(
                leftLines:   2,
                rightLines:  4,
                topLines:    2,
                bottomLines: 3,
                fontSizePx:  12,
                lineHeight:  1.5,
                actualWidthPx:  widthPx,
                actualHeightPx: heightPx);

            return svgHelper.RenderBaseSvgs(info, vbWithMargins, [.. elements], widthPx, heightPx, style);
        }
    }
}
