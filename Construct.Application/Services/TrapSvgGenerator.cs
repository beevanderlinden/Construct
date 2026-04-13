namespace Construct.Application.Services
{
    using Construct.Application.Diagrams;
    using Construct.Application.Interfaces.Beam;
    using Construct.Domain.Entities;
    using Construct.Domain.Extensions;
    using Eurocode.Belastingen;
    using ExportFactory.MigraDocContentModels;
    //using Kaskon.Toolbox.PrefabModels;
    using Mechanica.LiggerSB;
    using Mechanica.SimpleBeam;
    using Clipper2Lib;
    using Microsoft.Graph.AppCatalogs.TeamsApps.Item.AppDefinitions.Item.Bot;
    using Microsoft.Graph.Models;
    using Microsoft.Graph.Models.TermStore;
    using Microsoft.JSInterop;
    using MigraDoc.DocumentObjectModel;
    using Plotly.Blazor.LayoutLib.MapBoxLib.LayerLib;
    using Plotly.Blazor.Traces.ConeLib;
    using Render;
    using Svg.Pathing;
    using System;
    //using Svg;
    //using Svg;
    //using Svg;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using Tekla.Structures.Model;
    using Tekla.Structures.Model.UI;
    using static Construct.Domain.Entities.SvgHelper;
    using static Tekla.Structures.Filtering.Categories.ReinforcingBarFilterExpressions;
    using BEAM = Mechanica.SimpleBeam;
    using SBL = Mechanica.LiggerSB;
    using Plotly.Blazor.ConfigLib;

    public struct PuntXY
    {
        public PuntXY()      {      }

        public PuntXY(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; set; }
        public double Y { get; set; }
    }


    public class HoekTrapSvgGenerator
    {
        /// <summary>
        /// Genereert de SVG-elementen voor een hoektrap (L-vorm bovenaanzicht).
        /// Retourneert het L-vormige pad en zeven ankercirkels.
        /// </summary>
        public static List<BaseSvg> GenerateHoektrapSvg(HoekTrapEntity trap)
        {
            List<BaseSvg> elements = [];

            // L-vormig polygon met 6 punten
            List<Punt> polygon =
            [
                new(0, 0),
                new(trap.L1, 0),
                new(trap.L1, trap.B1),
                new(trap.B2, trap.B1),
                new(trap.B2, trap.L2),
                new(0, trap.L2)
            ];

            var path = SvgGenerator.MakePath(polygon, close: true);
            path.Fill = "lightgray";
            elements.Add(path);

            // Ankerpunten als cirkels (diameter = 100, radius = 50)
            double r = 50;
            double offset = 100;

            List<(double cx, double cy)> centers =
            [
                (offset,           offset),
                (trap.L1 - offset, offset),
                (trap.L1 - offset, trap.B1 * 0.5),
                (trap.L1 - offset, trap.B1 - offset),
                (offset,           trap.L2 - offset),
                (trap.B2 * 0.5,    trap.L2 - offset),
                (trap.B2 - offset, trap.L2 - offset)
            ];

            foreach (var (cx, cy) in centers)
            {
                elements.Add(new SvgCircle
                {
                    Cx = cx,
                    Cy = cy,
                    R = r,
                    Fill = "none",
                    Stroke = "var(--neutral-foreground-rest, black)"
                });
            }

            return elements;
        }

        /// <summary>
        /// Genereert een volledige SVG-string voor een hoektrap inclusief maatlijnen (L1, L2, B1, B2).
        /// </summary>
        public static string GenerateHoektrapSvgXml(
            HoekTrapEntity trap,
            BoundingBox bb,
            double widthPx,
            double heightPx,
            string style = "width:auto; height:auto;")
        {
            SvgHelper svgHelper = new();
            SvgDocumentInfo? info = new()
            {
                Id = trap.Id.ToString(),
                Title = trap.Merk ?? "HOEKTRAP",
                Description = "hoektrap",
                Label = trap.Merk ?? "LABEL",
            };

            var svgElements = GenerateHoektrapSvg(trap);

            List<SvgDimLine> dimLines =
            [
                // L1 — horizontale totaalbreedte (boven)
                new()
                {
                    Mode = DimLineMode.Horizontal,
                    X1 = 0, Y1 = 0, X2 = trap.L1, Y2 = 0,
                    OffsetLines = 3,
                    Text = $"{trap.L1:0}",
                    StrokeColor = "var(--neutral-foreground-rest, black)"
                },
                // L2 — verticale totaalhoogte (links)
                new()
                {
                    Mode = DimLineMode.Vertical,
                    X1 = 0, Y1 = 0, X2 = 0, Y2 = trap.L2,
                    OffsetLines = 3,
                    Text = $"{trap.L2:0}",
                    StrokeColor = "var(--neutral-foreground-rest, black)"
                },
                // B1 — hoogte rechter deel (rechts)
                new()
                {
                    Mode = DimLineMode.Vertical,
                    X1 = trap.L1, Y1 = 0, X2 = trap.L1, Y2 = trap.B1,
                    OffsetLines = -3,
                    Text = $"{trap.B1:0}",
                    StrokeColor = "var(--neutral-foreground-rest, black)"
                },
                // B2 — breedte onderste deel (onder)
                new()
                {
                    Mode = DimLineMode.Horizontal,
                    X1 = 0, Y1 = trap.L2, X2 = trap.B2, Y2 = trap.L2,
                    OffsetLines = -3,
                    Text = $"{trap.B2:0}",
                    StrokeColor = "var(--neutral-foreground-rest, black)"
                },
            ];

            var x = Math.Min(bb.MinX, 0);
            var y = Math.Min(bb.MinY, 0);
            var w = Math.Max(bb.Width, trap.L1);
            var h = Math.Max(bb.Height, trap.L2);
            SvgHelper.SvgViewBox viewBox = new(x, y, w, h);

            var vbWithMargins = viewBox.WithMarginsByText(
                leftLines: 5,
                rightLines: 5,
                topLines: 5,
                bottomLines: 5,
                fontSizePx: 12,
                lineHeight: 1.5,
                actualWidthPx: widthPx,
                actualHeightPx: heightPx
            );

            bb.MinX = vbWithMargins.X;
            bb.MinY = vbWithMargins.Y;
            bb.MaxXValue = vbWithMargins.X + vbWithMargins.Width;
            bb.MaxYValue = vbWithMargins.Y + vbWithMargins.Height;

            var status = trap.Akkoord ? "" : "has-warning";

            return svgHelper.RenderBaseSvgs(info, vbWithMargins, [.. svgElements, .. dimLines], widthPx, heightPx, style);
        }
    }



    public static class PuntExtensions
    {
        public static List<VerticalExtent> ToVerticalExtents(
            this IEnumerable<Punt> punten)
        {
            return [.. punten
                .GroupBy(p => p, PuntXComparer.Instance)
                .Where(g => g.Count() > 1)
                .Select(g =>
                {
                    var min = g.MinBy(p => p.Y);
                    var max = g.MaxBy(p => p.Y);

                    return new VerticalExtent(
                        X: (min.X + max.X) * 0.5,
                        MinY: min.Y,
                        MaxY: max.Y);
                })];
        }
    }



    public struct Punt : IEquatable<Punt>
    {
        /// <summary>
        /// Maximale toegestane afwijking bij vergelijking van coördinaten.
        /// </summary>
        public const double Tolerance = 1e-9;

        public Punt()
        {
            X = 0;
            Y = 0;
        }
        public Punt(double x = 0, double y = 0)
        {
            X = x;
            Y = y;
        }

        public double X { get; set; }
        public double Y { get; set; }

        public readonly bool Equals(Punt other)
        {
            return Math.Abs(X - other.X) <= Tolerance
                && Math.Abs(Y - other.Y) <= Tolerance;
        }

        public override readonly bool Equals(object? obj)
            => obj is Punt other && Equals(other);

        public override readonly int GetHashCode()
        {
            // Kwantiseren op tolerantieniveau
            long xHash = (long)Math.Round(X / Tolerance);
            long yHash = (long)Math.Round(Y / Tolerance);

            return System.HashCode.Combine(xHash, yHash);
        }

        public static bool operator ==(Punt left, Punt right)
            => left.Equals(right);

        public static bool operator !=(Punt left, Punt right)
            => !left.Equals(right);
    }

    public sealed class PuntXComparer : IEqualityComparer<Punt>
    {
        public static readonly PuntXComparer Instance = new();

        private PuntXComparer() { }

        public bool Equals(Punt a, Punt b)
            => Math.Abs(a.X - b.X) <= Punt.Tolerance;

        public int GetHashCode(Punt p)
        {
            // Kwantiseer X op tolerantieniveau
            long bucket = (long)Math.Floor(p.X / Punt.Tolerance);
            return bucket.GetHashCode();
        }
    }


    public readonly record struct VerticalExtent(
    double X,
    double MinY,
    double MaxY)
    {
        public double Height => MaxY - MinY;
    }


    public static class SvgGenerator
    {

        public static List<BaseSvg> GetSvgAxis(double x1, double x2, double y1, double y2, string titleX = "y", string titleY = "z", double scale = 1)
        {
            SvgLine hor = new() { X1 = x1, X2 = x2, Y1 = 0, Y2 = 0 };
            SvgLine vert = new() { X1 = 0, X2 = 0, Y1 = y1, Y2 = y2 };
            SvgText txtX1 = new() { X = x1, Y = 0 , Scale = scale, Text = titleX};
            SvgText txtX2 = new() { X = x2, Y = 0, Scale = scale, Text = titleX };
            SvgText txtY1 = new() { X = 0, Y = y1, Scale = scale, Text = titleY };
            SvgText txtY2 = new() { X = 0, Y = y2, Scale = scale, Text = titleY };


            return [hor, vert, txtX1, txtX2, txtY1, txtY2];
        }

        public static List<SvgText> MakeTestTextData(IEnumerable<Punt> punten, double scaleY = 1, double scale = 100)
        {
            List<SvgText> list = [];
            foreach (var p in punten)
            {
                list.Add(new SvgText() { X = p.X, Y= p.Y * scaleY, Pts = 6, Text = "*", Scale = scale });
            }
            return list;
        }


        public static List<SvgCircle> MakeDiagramCircles(IEnumerable<Punt> pts, double scaleY, double r)
        {
            List<SvgCircle> list = [];
            foreach (var p in pts)
            {
                var circle = new SvgCircle()
                {
                    Cx = p.X,
                    Cy = p.Y * scaleY,
                    R = r,
                    Fill = "blue",
                    Opacity = 0.4,
                };
                circle.Content.Title = $"x:{p.X:0.000} y:{p.Y:0.000}";

                list.Add(circle);
            }
            return list;
        }


        public static List<SvgCircle> MakeCircles(IEnumerable<Punt> punten, double scaleY = 1, double radius = 3, double scale = 1, string? fill = "blue")
        {
            List<SvgCircle> list = [];
            var radiusScaled = radius / scale;
            foreach (var p in punten)
            {
                list.Add(new SvgCircle() { Cx = p.X, Cy = p.Y * scaleY, R = radiusScaled, Fill=fill });
            }
            return list;
        }


        public static SvgPath MakeArrow(Punt punt, Punt kont)
        {
            SvgPath arrow = new();
            var sb = new StringBuilder();
            // naar de punt
            sb.Append($"M {punt.X:F4} {punt.Y:F4} ");
            // naar de kont
            sb.Append($"L {kont.X:F4} {kont.Y:F4} ");
            arrow.D = sb.ToString();
            return arrow;

        }

        public static SvgPath MakePath(IEnumerable<Punt> punten, double scaleY = 1, bool close = true, string fill = "none", string stroke = "var(--neutral-foreground-rest, black)")
        {
            SvgPath path = new()
            {
                Fill = fill,
                Stroke = stroke
            };
            var sb = new StringBuilder();

            var array = punten.ToArray();
            var p0 = array.First();

            p0.Y *= scaleY;
            sb.Append($"M {p0.X.ToString("F4", CultureInfo.InvariantCulture)} {p0.Y.ToString("F4", CultureInfo.InvariantCulture)} ");


            for (int i = 1; i< array.Length; i++)
            {
                var p = array[i];
                p.Y *= scaleY;
                sb.Append($"L {p.X.ToString("F4", CultureInfo.InvariantCulture)} {p.Y.ToString("F4", CultureInfo.InvariantCulture)} ");
            }

            if (close)
                sb.Append('Z');

            path.D = sb.ToString();
            return path;
        }


        public static List<BaseSvg> ToSvg(this BEAM.ILoad load, double scale, double yBasisPx, double heightPx = 20, bool showValue = false)
        {
            List<BaseSvg> result = [];
            if (load == null) return result;

            

            // =========================
            // 1. Voorbereiding
            // =========================
            double hFig = heightPx / scale;
            double yBasis = yBasisPx / scale;

            var group = new SvgGroup
            {
                Class = "beam-load",
                Data =
        {
            ["load-id"] = load.Id
        }
            };

            // =========================
            // 2. Puntlast
            // =========================
            if (load is BEAM.PointLoad pl)
            {
                double dir = Math.Sign(pl.Magnitude);
                double yTop = yBasis + dir * hFig;

                // pijl
                var path = MakePath(
                [
                    new Punt(pl.Position, yBasis),
                    new Punt(pl.Position, yTop)
                ], close:false);

                path.MarkerStart = "chevStart";


                group.Add(path);

                // pointer (verplaatsen puntlast)
                group.Add(MakePointer(
                    pl.Position,
                    Math.Min(yBasis, yTop),
                    Math.Max(yBasis, yTop),
                    load.Id,
                    "position"));

                // tekst = body-handle
                if (showValue)
                {
                    group.Add(new SvgText(
                        $"{Math.Abs(pl.Magnitude):0.0}",
                        x: pl.Position,
                        y: yTop - 2/scale,
                        dominantBaseLine: "base",
                        angle: 0,
                        scale: scale));
                }

                result.Add(group);
                return result;
            }

            // =========================
            // 3. Verdeelde last
            // =========================
            if (load is BEAM.DistributedLoad dl)
            {
                if (dl.StartMagnitude < 0 && dl.EndMagnitude > 0)
                {
                    throw new NotSupportedException(
                        "Lijnlast mag niet door nul heen lopen.");
                }

                double hMax = Math.Max(
                    Math.Abs(dl.StartMagnitude),
                    Math.Abs(dl.EndMagnitude));

                // geometrie
                var punten = new List<Punt>
        {
            new(dl.StartPosition, yBasis),
            new(dl.StartPosition, yBasis + dl.StartMagnitude / hMax * hFig),
            new(dl.EndPosition,   yBasis + dl.EndMagnitude   / hMax * hFig),
            new(dl.EndPosition,   yBasis)
        };

                // lastvlak
                var path = MakePath(punten, close: false);
                path.Fill = "lightgrey";
                path.MarkerStart = "chevStart";
                path.MarkerEnd = "chevEnd";
                group.Add(path);

                // basislijn
                group.Add(new SvgLine
                {
                    X1 = dl.StartPosition,
                    Y1 = yBasis,
                    X2 = dl.EndPosition,
                    Y2 = yBasis
                });

                // start pointer
                group.Add(MakePointer(
                    dl.StartPosition,
                    yBasis,
                    yBasis - hFig,
                    load.Id,
                    "start"));

                // end pointer
                group.Add(MakePointer(
                    dl.EndPosition,
                    yBasis,
                    yBasis - hFig,
                    load.Id,
                    "end"));

                // label = body-handle
                string label = showValue
                    ? $"{Math.Abs(Math.Max(Math.Abs(dl.StartMagnitude), Math.Abs(dl.EndMagnitude))):0.0}"
                    : dl.Name;


                var labelText = new SvgText(
                    label,
                    x: dl.LabelPosition.X,
                    y: yBasis - hFig / 2,
                    scale: scale)
                {
                    Anchor = dl.LabelPosition.Anchor,
                    Cursor = "move",
                    Class = "load-label",
                    Data =
                    {
                        ["load-id"] = load.Id,
                        ["handle"] = "move"
                    }
                };

                // interactieve events
                labelText.InteractiveAttributes["onpointerdown"] = "onLoadPointerDown(evt)";
                labelText.InteractiveAttributes["onpointermove"] = "onLoadPointerMove(evt)";
                labelText.InteractiveAttributes["onpointerup"] = "onLoadPointerUp(evt)";

                group.Add(labelText);

                result.Add(group);
            }

            return result;
        }

        private static SvgLine MakePointer(
  double x,
  double yTop,
  double yBottom,
  string loadId,
  string handle)
        {
            var pointer = new SvgLine
            {
                X1 = x,
                Y1 = yTop,
                X2 = x,
                Y2 = yBottom,
                Class = "load-pointer",
                Cursor = "ew-resize",
                Data =
                {
                    ["load-id"] = loadId,
                    ["handle"] = handle
                }
            };

            pointer.InteractiveAttributes["onpointerdown"] = "onLoadPointerDown(evt)";

            return pointer;
        }



        public static void AddToSection(this SvgDocument svgDoc, SectionContent section, SBLigger beam)
        {
            var tag = svgDoc.Tag;

            switch (svgDoc.Name.Trim().ToLower())
            {
                case "figuur":
                    if (tag is Eurocode.Belastingen.BelastingGeval geval)
                    {
                        section.Elements.Add(new ParagraphContent($"Belastinggeval ({geval.Naam} | {geval.Type})", "Kop 3"));
                        section.Elements.Add(new ParagraphContent(svgDoc.Render()));

                        var loads = beam.Loads.Where(l => l.LoadCase == geval);

                        // ✅ Gebruik de herbruikbare helper methode
                        var tableContent = LoadTableHelper.GetTabelBelastingen(loads);

                        section.Elements.Add(tableContent.Clone());
                    }
                    break;

                case "vz":
                    section.Elements.Add(new ParagraphContent(svgDoc.Render()));
                    break;

                case "my":
                    section.Elements.Add(new ParagraphContent(svgDoc.Render()));
                    break;
            }
        }



        public static List<SvgDocument> ToSvgDocumentsOpt(
            this SBLigger beam, int w, int h, string style, 
            List<BelastingGeval> belastingGevallen,
            List<BelastingCombinatieTypeEnum> belastingCombinatieTypes
           )
        {
            List<SvgDocument> collection = [];
            
            foreach (var bg in belastingGevallen ?? [])
            {
                BoundingBox boundingBox = new();
                var xml = SvgGenerator.GenerateBeamSvgContent(
                        beam: beam,
                        bb: boundingBox,
                        wPx: w,
                        hPx: h,
                        geval: bg
                    );

                SvgDocument svgDoc = new(300, 300)
                {
                    Name = "figuur",
                    Tag = bg,
                    Style = style,
                    ViewBoxMinX = boundingBox.MinX,
                    ViewBoxMinY = boundingBox.MinY,
                    ViewBoxWidth = boundingBox.Width,
                    ViewBoxHeight = boundingBox.Height,
                };
                svgDoc.RawFragments.Clear();
                svgDoc.RawFragments.Add(xml);

                collection.Add(svgDoc);
            }
            
            foreach (var combinatieType in belastingCombinatieTypes)
            {



                BoundingBox boundingBox = new();
                var xml = SvgGenerator.GenerateBeamDiagramSvgContent(
                        beam: beam,
                        bb: boundingBox,
                        wPx: w,
                        hPx: h,
                        diagramContext: new DiagramContext() { 
                            DiagramType =  DiagramType.ShearForceVz, 
                            LoadCombinationType = combinatieType, 
                            Source = DiagramSource.LoadCombinationType}
                    );

                SvgDocument svgDoc = new(300, 300)
                {
                    Name = "vz",
                    Tag = combinatieType,
                    Style = style,
                    ViewBoxMinX = boundingBox.MinX,
                    ViewBoxMinY = boundingBox.MinY,
                    ViewBoxWidth = boundingBox.Width,
                    ViewBoxHeight = boundingBox.Height,
                };
                svgDoc.RawFragments.Clear();
                svgDoc.RawFragments.Add(xml);

                collection.Add(svgDoc);

                //-----------------------------------------
                //-- My
                //-----------------------------------------

                boundingBox = new();
                xml = SvgGenerator.GenerateBeamDiagramSvgContent(
                        beam: beam,
                        bb: boundingBox,
                        wPx: w,
                        hPx: h,
                        diagramContext: new DiagramContext()
                        {
                            DiagramType = DiagramType.BendingMomentMy,
                            LoadCombinationType = combinatieType,
                            Source = DiagramSource.LoadCombinationType
                        }
                    );

                svgDoc = new(300, 300)
                {
                    Name = "my",
                    Style = style,
                    Tag = combinatieType,
                    ViewBoxMinX = boundingBox.MinX,
                    ViewBoxMinY = boundingBox.MinY,
                    ViewBoxWidth = boundingBox.Width,
                    ViewBoxHeight = boundingBox.Height,
                };
                svgDoc.RawFragments.Clear();
                svgDoc.RawFragments.Add(xml);
                collection.Add(svgDoc);



            }
                      

            return collection;
        }


        public static List<BaseSvg> GenerateBeamShearDiagram(
            double scale, Mechanica.SimpleBeam.SBLigger beam, 
            List<BeamResult> results, 
            string fill, 
            string stroke,
            bool combineerGrafieken = !true)
        {
            List<BaseSvg> svgTags = [];
            
            if (beam == null || results.Count == 0) 
                return svgTags;

            
            List<Punt> points = [];
            List<List<Punt>> ptsCollection = [];

            foreach (var r in results)
            {
                if (r != null)
                {
                    List<Punt> vPts = [];
                    foreach (var (x, V) in r.ShearDiagram)
                    {
                        vPts.Add(new(x, V));
                        points.Add(new(x, V));
                    }
                    ptsCollection.Add(vPts);
                }
            }
            //points.Add(new(beam.Length, 0));

            var verticalExtents = points.ToVerticalExtents();



            var grafiekAmplitude = 50.0 / scale;
            var scaleY = grafiekAmplitude /
                   points
                   .Select(p => Math.Abs(p.Y))
                   .DefaultIfEmpty(1)
                   .Max();

            // Keuze: individueel of gecombineerd (envelope)
            if (!combineerGrafieken)
            {
                // ORIGINEEL: Teken alle lijnen individueel
                foreach (var pts in ptsCollection)
                {
                    svgTags.Add(MakePath(pts, -scaleY, close: true, fill: fill, stroke: stroke));
                }
            }
            else
            {
                // NIEUW: Envelope aanpak voor dwarskrachten
                var allShearPoints = new List<(double x, double v)>();
                
                foreach (var r in results)
                {
                    if (r?.ShearDiagram != null)
                    {
                        allShearPoints.AddRange(r.ShearDiagram);
                    }
                }

                if (allShearPoints.Count > 0)
                {
                    var envelope = BuildEnvelopePolygonOpt(allShearPoints);
                    
                    if (envelope.Count > 0)
                    {
                        var svgPunten = envelope.Select(p => new Punt(p.x, p.m)).ToList();
                        var envelopePath = MakePath(svgPunten, -scaleY, true);
                        envelopePath.Fill = fill;
                        envelopePath.Stroke = stroke;
                        envelopePath.StrokeWidth = 2;
                        svgTags.Add(envelopePath);
                        
                        Console.WriteLine($"✅ Shear EnvelopeOpt: {envelope.Count} punten");
                    }
                }
            }

            // Vertical extent lijnen en labels (altijd tonen)
            //var verticalExtents = points.ToVerticalExtents();
            
            foreach (var e in verticalExtents)
            {
                // lijn tekenen
                svgTags.Add(new SvgLine()
                {
                    X1 = e.X,
                    Y1 = e.MinY * -scaleY,
                    X2 = e.X,
                    Y2 = e.MaxY * -scaleY,
                    Stroke = stroke,
                    StrokeWidth = 0.5,
                });

                bool minRelevant = Math.Abs(e.MinY) >= 1.0;
                bool maxRelevant = Math.Abs(e.MaxY) >= 1.0;
                double dX = 4.0 / scale;

                // Case 1: beide relevant → twee labels
                if (minRelevant && maxRelevant && e.MinY < 0 && e.MaxY > 0)
                {
                    svgTags.Add(new SvgText(
                        $"{e.MinY:0.#}",
                        e.X,
                        e.MinY * -scaleY,
                        angle: 0,
                        scale: scale,
                        anchor: "end",
                        dx: -dX,
                        dominantBaseLine: "middle"));



                    svgTags.Add(new SvgText(
                        $"{e.MaxY:0.#}",
                        e.X,
                        e.MaxY * -scaleY,
                        angle: 0,
                        scale: scale,
                        dx: dX,
                        anchor: "start", dominantBaseLine: "middle"));
                }
                // Case 2: slechts één relevant → grootste absolute waarde
                else
                {
                    double? valueToShow = null;

                    if (minRelevant && maxRelevant)
                        valueToShow = Math.Abs(e.MinY) >= Math.Abs(e.MaxY) ? e.MinY : e.MaxY;
                    else if (minRelevant)
                        valueToShow = e.MinY;
                    else if (maxRelevant)
                        valueToShow = e.MaxY;

                    if (valueToShow.HasValue)
                    {
                        string anchor = (valueToShow <= 0) ? "end" : "start";
                        double angle = -90;
                        
                        if (e.X == 0 || e.X == beam.Length)
                        {
                            angle = 0;
                            if (anchor == "start")
                                anchor = "end";
                            else
                                anchor = "start";
                        }
                            

                        var dx = dX;

                        if (anchor == "end")
                            dx = -dX;

                        // alleen waarde aan begin eind tonen (VOOR NU!)
                        // dus alleen als de hoek 0 graden is.
                        // mogelijk later aanpassen
                        if (angle == 0)
                            svgTags.Add(new SvgText(
                                $"{valueToShow.Value:0.#}",
                                e.X,
                                valueToShow.Value * -scaleY,
                                angle: angle,
                                dx: dx,
                                scale: scale, anchor: anchor,
                                dominantBaseLine: "middle"));
                    }
                }
            }


            return svgTags;

        }

        public static List<BaseSvg> GenerateBeamDeflectionDiagram(double scale, SBLigger beam, List<BeamResult> results, string stroke)
        {
            List<BaseSvg> svgTags = [];
            if (beam == null) return svgTags;

            SvgLine line = new()
            {
                X1 = 0,
                Y1 = 0,
                X2 = beam.Length,
                Y2 = 0,
                Stroke = "gray",
                StrokeDashArray = "4 2",
                StrokeWidth = 0.5,
            };
            svgTags.Add(line);
            if (results.Count == 0) return svgTags;

            List<List<Punt>> deflectionCollection = [];

            foreach (var r in results)
            {
                List<Punt> pts = [];

                foreach (var (x,W) in r.DeflectionDiagram)
                {
                    pts.Add(new(x, W));
                }
                deflectionCollection.Add(pts);
            }

            var grafiekAmplitude = 100.0 / scale;

            List<Punt> graphData = [];
            foreach (var list in deflectionCollection)
            {
                graphData.AddRange(list);
            }

            var scaleY = grafiekAmplitude /
                graphData
                .Select(p => Math.Abs(p.Y))
                .DefaultIfEmpty(1)
                .Max();

            foreach (var list in deflectionCollection)
            {
                var p = MakePath(list, -scaleY, false);
                p.Stroke = stroke;
                svgTags.Add(p);

                //foreach (var xW in list)
                //{
                 //   svgTags.Add(new SvgText($"{(xW.Y* 1000):0.#}", xW.X, xW.Y * -scaleY, scale: scale));
                //}
            }


            var minDataPoint = graphData.OrderBy(p => p.Y).FirstOrDefault();
            var maxDataPoint = graphData.OrderBy(p => p.Y).LastOrDefault();


            if (Math.Abs(minDataPoint.Y) > 0.00)
            {
                SvgText txtW = new((minDataPoint.Y * 1000).ToString("0.#"), x: minDataPoint.X, y: minDataPoint.Y * -scaleY, scale: scale)
                {
                    DY = 3.0 / scale
                };
                if (minDataPoint.Y < 0)
                    txtW.DominantBaseLine = "hanging";
                else txtW.DominantBaseLine = "base";
                svgTags.Add(txtW);
            }

            if (maxDataPoint.Y > 0.00)
            {
                SvgText txtW = new((maxDataPoint.Y * 1000).ToString("0.#"), x: maxDataPoint.X, y: maxDataPoint.Y * -scaleY, scale: scale)
                {
                    DY = 3.0 / scale,
                    DominantBaseLine = "base"
                };
                svgTags.Add(txtW);
            }

            


            return svgTags;
        }
        
        public static List<BaseSvg> GenerateBeamMomentDiagram(double scale, Mechanica.SimpleBeam.SBLigger beam, List<BeamResult> results, string fill, string stroke, bool combineerGrafieken = true)
        {
            List<BaseSvg> svgTags = [];
            if (beam != null)
            {
                if (results.Count == 0) return svgTags;

                // 1. Teken altijd M=0 baseline
                svgTags.Add(new SvgLine()
                {
                    X1 = 0,
                    Y1 = 0,
                    X2 = beam.Length,
                    Y2 = 0,
                    Stroke = "gray",
                    StrokeDashArray = "4 2",
                    StrokeWidth = 0.5,
                });

                // Bouw polygonen van beide diagrammen
                List<List<Punt>> momentPointsCollection = [];

                foreach (var r in results)
                {
                    List<Punt> points = [new(0, 0)];
                    if (r != null)
                    {
                        foreach (var (x, M) in r.MomentDiagram)
                        {
                            points.Add(new(x, M));
                        }
                    }
                    points.Add(new(beam.Length, 0));
                    momentPointsCollection.Add(points);

                    List<Punt> accidental = [new(0, 0)];
                    if (r != null)
                    {
                        foreach (var (x,MomAcc) in r.MomentDiagramAccidentalFixity)
                        {
                            accidental.Add(new(x, MomAcc));
                        }
                        accidental.Add(new(beam.Length, 0));
                    }
                    momentPointsCollection.Add(accidental);
                }

                var grafiekAmplitude = 100.0 / scale;

                List<Punt> momentData = [];
                foreach (var list in momentPointsCollection)
                {
                    momentData.AddRange(list);
                }

                var orderedList = momentData.OrderBy(md => md.Y).ToList();
                var min = orderedList.FirstOrDefault().Y;
                var max = orderedList.LastOrDefault().Y;
                var delta = Math.Abs(min) + Math.Abs(max);


                var scaleY = grafiekAmplitude /
                   delta;

                // 2. Keuze: individueel of gecombineerd tekenen
                if (!combineerGrafieken)
                {
                    // ORIGINEEL: Teken alle lijnen individueel (meerdere door elkaar)
                    foreach (var list in momentPointsCollection)
                    {
                        var p = MakePath(list, -scaleY, true);
                        p.Fill = fill;
                        p.Stroke = stroke;
                        svgTags.Add(p);
                    }
                }
                else
                {
                    // NIEUW: Envelope aanpak - voor elke X de min/max Y bepalen
                    var allPolygons = new List<List<(double x, double m)>>();

                    // Verzamel alle punten van alle results
                    var allPoints = new List<(double x, double m)>();
                    
                    foreach (var r in results)
                    {
                        if (r?.MomentDiagram != null)
                        {
                            allPoints.AddRange(r.MomentDiagram);
                        }
                        if (r?.MomentDiagramAccidentalFixity != null)
                        {
                            allPoints.AddRange(r.MomentDiagramAccidentalFixity);
                        }
                    }

                    if (allPoints.Count > 0)
                    {
                        // Bouw envelope polygon met OPTIMALE methode
                        var envelope = BuildEnvelopePolygonOpt(allPoints);
                        
                        if (envelope.Count > 0)
                        {
                            var svgPunten = envelope.Select(p => new Punt(p.x, p.m)).ToList();
                            var envelopePath = MakePath(svgPunten, -scaleY, true);
                            envelopePath.Fill = fill;
                            envelopePath.Stroke = stroke;
                            envelopePath.StrokeWidth = 1;
                            svgTags.Add(envelopePath);
                            
                        }
                    }

                    /* 🐛 DEBUG CODE - UITGECOMMENT
                    // OUDE CLIPPER2 AANPAK MET ZERO-CROSSING DETECTION
                    List<List<(double x, double m)>> allPolygons = [];

                    // Bouw voor elk result aparte polygonen (kan meerdere zijn per diagram!)
                    foreach (var r in results)
                    {
                        if (r != null)
                        {
                            // Voeg normale momentdiagram polygonen toe (kan meerdere zijn bij sign changes)
                            if (r.MomentDiagram.Count > 0)
                            {
                                allPolygons.AddRange(BuildMomentPolygons(r.MomentDiagram.ToList(), beam.Length));
                            }
                            
                            // Voeg accidental fixity polygonen toe
                            if (r.MomentDiagramAccidentalFixity.Count > 0)
                            {
                                allPolygons.AddRange(BuildMomentPolygons(r.MomentDiagramAccidentalFixity.ToList(), beam.Length));
                            }
                        }
                    }

                    // 🐛 DEBUG: Teken ALLE individuele polygonen met verschillende kleuren
                    string[] debugColors = ["red", "blue", "green", "orange", "purple", "brown", "pink", "cyan"];
                    for (int i = 0; i < allPolygons.Count; i++)
                    {
                        var poly = allPolygons[i];
                        var svgPunten = poly.Select(p => new Punt(p.x, p.m)).ToList();
                        var debugPath = MakePath(svgPunten, -scaleY, true);
                        debugPath.Fill = debugColors[i % debugColors.Length];
                        debugPath.FillOpacity = 0.3;
                        debugPath.Stroke = debugColors[i % debugColors.Length];
                        debugPath.StrokeWidth = 2;
                        svgTags.Add(debugPath);
                    }

                    // Merged polygon (dikke zwarte lijn eroverheen)
                    if (allPolygons.Count > 0)
                    {
                        var mergedPolygon = MergeMultiplePolygonsWithClipper(allPolygons);

                        if (mergedPolygon.Count > 0)
                        {
                            var svgPunten = mergedPolygon.Select(p => new Punt(p.x, p.m)).ToList();
                            var p = MakePath(svgPunten, -scaleY, true);
                            p.Fill = "none";
                            p.Stroke = "black";
                            p.StrokeWidth = 3;
                            p.StrokeDashArray = "5,5";
                            svgTags.Add(p);
                        }
                    }
                    */
                }

                // 3. Toon min/max waarden
                List<Punt> keyPoints = [];

                keyPoints.Add(momentData.Where(d => d.X == 0).OrderBy(d => d.Y).LastOrDefault());
                keyPoints.Add(momentData.Where(d => d.X == beam.Length).OrderBy(d => d.Y).LastOrDefault());
                keyPoints.Add(momentData.OrderBy(p => p.Y).FirstOrDefault());

                
                foreach (var kp in keyPoints)
                {
                    if (Math.Abs(kp.Y) > 0.001)
                    {
                        SvgText txtM = new(kp.Y.ToString("0.#"), x: kp.X, y: kp.Y * -scaleY, scale: scale)
                        {
                            DY = Math.Sign(kp.Y) * -3.0 / scale,
                            DominantBaseLine = kp.Y < 0 ? "hanging" : "base"
                        };
                        svgTags.Add(txtM);
                    }
                }

              
            }
            return svgTags;
        }

        /// <summary>
        /// Bouwt een omhullende polygon - OPTIMALE VERSIE.
        /// Simpele logica: sorteer op X, bottom line = min(Y,0), top line reversed = max(Y,0).
        /// </summary>
        private static List<(double x, double m)> BuildEnvelopePolygonOpt(
            List<(double x, double m)> allPoints)
        {
            if (allPoints.Count == 0) return [];

            // Sorteer alle punten op X-positie
            var sortedPoints = allPoints.OrderBy(p => p.x).ToList();

            // Groepeer per X om min/max te vinden
            var byX = sortedPoints
                .GroupBy(p => p.x)
                .Where(g => g.Count() > 2)
                .OrderBy(g => g.Key)
                .ToList();

            var envelope = new List<(double x, double m)>();

            // BOTTOM LINE: links → rechts, neem min(Y, 0)
            foreach (var group in byX)
            {
                double x = group.Key;
                double minY = group.Min(p => p.m);
                
                // Neem kleinste Y, maar maximaal 0
                double yBottom = Math.Min(minY, 0);
                envelope.Add((x, yBottom));
            }

            // TOP LINE: rechts → links, neem max(Y, 0)
            foreach (var group in byX.Reverse<IGrouping<double, (double x, double m)>>())
            {
                double x = group.Key;
                double maxY = group.Max(p => p.m);
                
                // Neem grootste Y, maar minimaal 0
                double yTop = Math.Max(maxY, -0);
                envelope.Add((x, yTop));
            }

            Console.WriteLine($"📐 EnvelopeOpt: {byX.Count} unieke X → {envelope.Count} punten (bottom+top)");
            
            return envelope;
        }

       
        public static List<BaseSvg> AddChartTitle(string title, double x = 0, double y = 0, double scale = 1)
        {
            return [new SvgText(title, x, y, scale: scale) { Anchor = "end", DX = -4.0 / scale, DominantBaseLine = "middle" }];
        }

        public static List<BeamResult> FilterResults(Mechanica.SimpleBeam.SBLigger beam, DiagramContext context)
        {
            switch (context.Source)
            {
                // bepaal eerst welke data we gaan tekenen
                case Diagrams.DiagramSource.LoadCase:
                    if (context.LoadCase != null)
                        return [.. beam.ResultCollection.ForLoadCase(context.LoadCase)];
                    else return [];
                case Diagrams.DiagramSource.LoadCombination:
                    if (context.LoadCombination != null)
                        return [.. beam.ResultCollection.ForCombination(context.LoadCombination)];
                    else return [];

                default:
                case Diagrams.DiagramSource.LoadCombinationType:
                    if (context.LoadCombinationType != null)
                        return [.. beam.ResultCollection.ForCombinationType(context.LoadCombinationType.Value)];
                    else return [];


            }
        }


        public static string GenerateBeamDiagramSvgContent(Mechanica.SimpleBeam.SBLigger beam, BoundingBox bb, double wPx, double hPx, Diagrams.DiagramContext diagramContext)
        {
            if (beam == null) return "";
            SvgHelper svgHelper = new();
            List<BaseSvg> svgTags = [];
            
            double modelHeight = 0.1;
            double modelWidth = beam.Length;
            // 1️⃣ layout bepalen (NIEUW)
            var layout = CalculateBeamLayout(
                bb, wPx, hPx,
                modelHeight: modelHeight,
                modelWidth: modelWidth);



            double scale = layout.Scale;
            double scaleX = layout.ViewBox.ScaleX(wPx);


            // 2️⃣ boundingbox init
            bb.MinX = layout.ViewBox.X;
            bb.MinY = layout.ViewBox.Y;
            bb.MaxXValue = layout.ViewBox.X + layout.ViewBox.Width;
            bb.MaxYValue = layout.ViewBox.Y + layout.ViewBox.Height;


            var results = FilterResults(beam, diagramContext);
            
            switch (diagramContext.DiagramType)
            {
                case DiagramType.ShearForceVz:
                    svgTags.AddRange(AddChartTitle("Vz", scale: scale));
                    svgTags.AddRange(SvgGenerator.GenerateBeamShearDiagram(scale, beam, results, "lightblue", "darkblue"));
                    break;
                case DiagramType.BendingMomentMy:
                    svgTags.AddRange(AddChartTitle("My", scale: scale));
                    svgTags.AddRange(SvgGenerator.GenerateBeamMomentDiagram(scale, beam, results, "lightblue", "darkblue"));
                    break;
                case DiagramType.DeflectionW:
                    svgTags.AddRange(AddChartTitle("w", scale: scale));
                    svgTags.AddRange(SvgGenerator.GenerateBeamDeflectionDiagram(scale, beam, results, "darkblue"));
                    break;
            }


            // bounding box bijwerken adhv svgTags
            var bbCombined = SvgPathBoundingBoxCalculator.GetCombinedBoundingBox(svgTags.OfType<SvgPath>());
            var marginY = 24.0 / scale;
            bb.MinY = bbCombined.MinY - marginY;
            bb.MaxYValue = bbCombined.MaxY + marginY;

            diagramContext.Scale = scale;
            diagramContext.ScaleX = scaleX;

            return svgHelper.GetSvgContentXml(svgTags);


        }



        /// <summary>
        /// Tekent
        /// </summary>
        /// <param name="beam">De SBLigger</param>
        /// <param name="bb">De BoundingBox</param>
        /// <param name="wPx">Width in px</param>
        /// <param name="hPx">Height in px</param>
        /// <param name="geval">Het belastinggeval (indien van toepassing)</param>
        /// <returns></returns>
        public static string GenerateBeamSvgContent(Mechanica.SimpleBeam.SBLigger beam, BoundingBox bb, double wPx, double hPx, BelastingGeval? geval = null)
        {
            SvgHelper svgHelper = new();
            List<BaseSvg> svgTags = [];

            //baseSvgs = new List<BaseSvg>();
            if (beam == null) return "";

            double modelHeight = 0.1;
            double modelWidth = beam.Length;

            // 1️⃣ layout bepalen (NIEUW)
            var layout = CalculateBeamLayout(
                bb, wPx, hPx,
                modelHeight: modelHeight,
                modelWidth: modelWidth);

            double scale = layout.Scale;
            var margin = layout.Margin;

            // 2️⃣ boundingbox init
            bb.MinX = layout.ViewBox.X;
            bb.MinY = layout.ViewBox.Y;
            bb.MaxXValue = layout.ViewBox.X + layout.ViewBox.Width;
            bb.MaxYValue = layout.ViewBox.Y + layout.ViewBox.Height;


            // positions
            var positions = beam.GetPositions(0);

            // als er een geval opgegeven is, teken de belastingen
            if (geval != null)
            {
                var belastingen = beam.Loads
                        .Where(l => l.LoadCase == geval)
                        .ToList();


                svgTags.Add(new SvgText($"{geval?.Naam}", 0, 0, scale: scale) { Anchor = "end", DX = -12.0 / scale });

                var layoutService = new LoadLayoutService();
                layoutService.PlaceLoadsInLayers(belastingen, verticalStep: 20);

                List<double> loadPosXs = [];
                foreach (var b in belastingen)
                {
                    var svgs = b.ToSvg(scale, -b.OffsetY, 20, showValue: true);
                    svgTags.AddRange(svgs);
                    loadPosXs.Add(b.Range.Start);
                    loadPosXs.Add(b.Range.End);
                }

                // absolute maatvoering als gedraaide tekst
                foreach (double pos in loadPosXs.Distinct())
                {
                    //svgTags.Add(new SvgText($"{pos:0.000}", x: pos, y: 30 / scale, angle: -90, scale: scale, pts: 8) { DominantBaseLine = "middle", Anchor = "end"});
                }

            }



            double posY = 20 / scale;

            // maatlijn
            svgTags.Add(new SvgDimLine() { Text = $"{beam.Length:0.000 m}", X2 = beam.Length, Offset = posY, Scale = scale, MarkerStart = "chevStart", MarkerEnd = "chevEnd" });

            // profiel (materiaal)
            svgTags.Add(new SvgText()
            {
                DominantBaseLine = "hanging",
                X = beam.Length * 0.5,
                Y = posY,
                DY = 4.0 / scale,
                Text = $"{beam.Profiel?.Naam} ({beam.Materiaal?.UserFriendlyName})",
                Scale = scale
            });


            // var steunpunten
            var size1 = posY / 2.0;
            var size2 = size1 * 1.2;
            AddSupportToPathCollection(svgTags, size1, size2, beam.StartSupport, 0);
            AddSupportToPathCollection(svgTags, size1, size2, beam.EndSupport, beam.Length);

            // teken ook de lijn ligger zelf (als dikkere lijn met ronde eindjes)
            svgTags.Add(new SvgLine(){X2 = beam.Length, StrokeWidth = 2.0});

            // bounding box bijwerken adhv svgTags
            var bbCombined = SvgPathBoundingBoxCalculator.GetCombinedBoundingBox(svgTags.OfType<SvgPath>());
            var marginY = 5.0 / scale;
            bb.MinY = bbCombined.MinY - margin;
            bb.MaxYValue = bbCombined.MaxY + margin;

            return svgHelper.GetSvgContentXml(svgTags);

        }




        [Obsolete("Vervangen door LOSSE svgs zie GenerateBeamDiagram")]
        public static string GenerateStrookSvgXml(StrookEntity strook, 
            BoundingBox bb, 
            double actualWidthPx, 
            double actualHeightPx, 
            string style="width:auto; height: auto;", 
            bool toonMomentenlijn = false,
            bool toonDwarskrachtenlijn = false,
            bool toonFiguren = true,
            BelastingGeval? loadCase = null
            )
        {
            SvgHelper svgHelper = new();

            if (strook == null) 
                return "";

            // maak svgObjecten
            List<BaseSvg> svgTags = [];
            string fill = "lightblue";
            string stroke = "darkblue";

            // 💡maak de paden en maatlijnen
            List<Punt> punten = [new(), new(strook.Beam.Length, 0)];


            // Bepaal de viewBox
            double hoogte = 0.1;

            var x = Math.Min(bb.MinX, 0);
            var y = Math.Min(bb.MinY, -hoogte / 2.0);
            var w = Math.Max(bb.Width, strook.Beam.Length);
            var h = Math.Max(bb.Height, hoogte);
            SvgHelper.SvgViewBox viewBox = new(x, y, w, h);

            // Maak een nieuwe viewbox aan met het aantal regelafstanden in rondom de tekening.
            var vbWithMargins = viewBox.WithMarginsByText(
                leftLines: 5,
                rightLines: 5,
                topLines: 0,
                bottomLines: 0,
                fontSizePx: 12,
                lineHeight: 1.5,
                actualWidthPx: actualWidthPx,
                actualHeightPx: actualHeightPx
            );

           

            //
            double scale = vbWithMargins.GetScale(actualWidthPx, actualHeightPx);

            // we weten nu de schaal, dus nog 1x de viewbox maken

            var nieuweY = vbWithMargins.Y; // dit is de y op -0.5m (inclusief marge)
            // maar dit moet worden de 

            //x = vbWithMargins.X;
            //y = -100 / scale;
            //w = vbWithMargins.Width;
            //h = 200 / scale;


            // ALS WE DIT DOEN! Dan werkt het PERFECT.. ALLEEN GAAT HET SCHERM KNIPPEREN/BLINKEN
            // WE MOETEN DUS ZIEN TE VOORKOMEN DAT DIT GEBEURT
            // 
            //vbWithMargins = new(x, y, w, h);

            bb.MinX = x;
            bb.MinY = y;
            bb.MaxXValue = vbWithMargins.X + vbWithMargins.Width;
            bb.MaxYValue = h;

            bb.MinX = vbWithMargins.X;
            bb.MinY = vbWithMargins.Y;
            bb.MaxYValue = vbWithMargins.Height;

            // de marge is 12px
            double margin = 12.0 / scale;

            // positions
            var positions = strook.Beam.GetPositions(0);

            if (toonFiguren)
            {

            var belastingen = strook.Beam.Loads
                    .Where(l=>l.LoadCase == loadCase)
                    .ToList();

            var distributed = strook.Beam.Loads
                .Where(l => l.StartX != l.EndX)
                .OrderBy(l => l.StartX)
                .ToList();

            var pointLoads = strook.Beam.Loads
                .Where(l => l.StartX == l.EndX)
                .OrderBy(l => l.StartX)
                .ToList();

                svgTags.Add(new SvgText($"{loadCase?.Naam} ({loadCase?.Type})", 0, 0, scale: scale) { Anchor = "end", DX = -12.0 / scale });


            var layoutService = new LoadLayoutService();


                layoutService.PlaceLoadsInLayers(belastingen, verticalStep: 20);

                var distinctLoads = belastingen.GetSimilarShape();

                foreach (var b in distinctLoads)
                {
                    var svgs = b.ToSvg(scale, -b.OffsetY, 20, showValue: true);
                    svgTags.AddRange(svgs);
                }

            


            double posY = 20 / scale;

                // maatlijn
                SvgDimLine maat1 = new()
                {
                    Text = $"{strook.Beam.Length:0.000 m}",
                    X2 = strook.Beam.Length,
                    Offset = posY,
                    Scale = scale,
                    MarkerStart = "chevStart",
                    MarkerEnd = "chevEnd"
                };

                //

                svgTags.Add(maat1);
                //

               

            foreach (double pos in positions)
            {
                    SvgText absolute = new($"{pos:0.000}", pos, 1.2 * posY, angle: -90, scale: scale)
                    {
                        DominantBaseLine = "middle",
                        Anchor = "end"
                    };
                    svgTags.Add(absolute);
            }


            // var steunpunten
            var size1 = posY / 2.0;
            var size2 = size1 * 1.2;
            AddSupportToPathCollection(svgTags, size1, size2, strook.Beam.StartSupport, 0);
            AddSupportToPathCollection(svgTags, size1, size2, strook.Beam.EndSupport, strook.Beam.Length);

                // teken ook de lijn ligger zelf (als dikkere lijn met ronde eindjes)
                svgTags.Add(new SvgLine()
                { 
                    X2 = strook.Beam.Length,
                    StrokeWidth = 2.0,
                });



            }

            if (toonDwarskrachtenlijn)
            {
                svgTags.Add(new SvgText("Vz", 0, 0, scale: scale) { Anchor = "end" ,DX = -12.0/scale});

                // is dit al gedaan?
                var results = strook.Beam.ResultCollectionLegacy
                    .Where(r => 
                    r.Value.Combination.Type == Eurocode.Belastingen.BelastingCombinatieTypeEnum.Fundamenteel_B |
                    r.Value.Combination.Type == Eurocode.Belastingen.BelastingCombinatieTypeEnum.Fundamenteel_A
                    ).ToList();
                var result = results.FirstOrDefault();

                if (results.Count == 0) return "";

                var beamResult = result.Value;


                List<List<Punt>> shearPointsCollection = [];

                foreach (var r in results)
                {
                    List<Punt> points = [new(0,0)];
                    if (r.Value != null)
                    {
                        foreach (var entryPoint in r.Value.ShearDiagram)
                        {
                            points.Add(new(entryPoint.x, entryPoint.V));
                        }
                    }
                    points.Add(new(strook.Beam.Length, 0));
                    shearPointsCollection.Add(points);
                }


                var grafiekAmplitude = 75.0 / scale;

                var shear = new List<Punt>();
                foreach (var spc in shearPointsCollection)
                {
                    shear.AddRange(spc);
                }

                var scaleY = grafiekAmplitude /
                  shear
                     .Select(p => Math.Abs(p.Y))
                     .DefaultIfEmpty(100)
                     .Max();

                //var shearPath = MakePath(shear, -scaleY, false);
                //var shearTest = MakeCircles(shear, scaleY: -scaleY, scale: scale, radius: 3);

                var bullits = MakeDiagramCircles(shear, -scaleY, 3 / scale);


                
                //shearPath.Fill = fill;
                //shearPath.StrokeWidth = 1;
                //shearPath.Stroke = stroke;

                //svgPaths.Add(shearPath);
                //tags.Add(shearPath);
                //tags.AddRange(shearTest);
                svgTags.AddRange(bullits);

                foreach (var list in shearPointsCollection)
                {
                    var p = MakePath(list, -scaleY, true);
                    p.Fill = fill;
                    p.Stroke = stroke;

                    svgTags.Add(p);
                }


                var shearOrder = shear.OrderBy(s => s.Y).ToList();



                var shear1 = shearOrder.First();
                var v1 = shear1.Y;

                SvgText txtV1 = new((shear1.Y).ToString("0.00"), x: shear1.X, y: shear1.Y * -scaleY, scale: scale)
                {
                    DY = 3.0 / scale
                };
                if (v1 < 0)
                    txtV1.DominantBaseLine = "hanging";
                else txtV1.DominantBaseLine = "base";
                svgTags.Add(txtV1);

                var shear2 = shearOrder.Last();
                var v2 = shear2.Y;
                SvgText txtV2 = new(shear2.Y.ToString("0.00"), x: shear2.X, y: shear2.Y * -scaleY, scale: scale)
                {
                    DY = -3.0 / scale
                };
                if (v2 < 0)
                    txtV2.DominantBaseLine = "hanging";
                else txtV2.DominantBaseLine = "base";
                svgTags.Add(txtV2);



            }

            if (toonMomentenlijn)
            {

                var results = strook.Beam.ResultCollectionLegacy
                  .Where(r =>
                  r.Value.Combination.Type == Eurocode.Belastingen.BelastingCombinatieTypeEnum.Fundamenteel_B |
                  r.Value.Combination.Type == Eurocode.Belastingen.BelastingCombinatieTypeEnum.Fundamenteel_A
                  ).ToList();

                if (results.Count == 0) return "";

                List<List<Punt>> momentPointsCollection = [];

                foreach (var r in results)
                {
                    List<Punt> points = [new(0, 0)];
                    List<Punt> pointsAccidental = [new(0, 0)];
                    if (r.Value != null)
                    {
                        foreach (var entryPoint in r.Value.MomentDiagram)
                        {
                            points.Add(new(entryPoint.x, entryPoint.M));
                        }
                        foreach (var entryPoint in r.Value.MomentDiagramAccidentalFixity)
                        {
                            pointsAccidental.Add(new(entryPoint.x, entryPoint.M));
                        }
                    }
                    points.Add(new(strook.Beam.Length, 0));
                    momentPointsCollection.Add(points);
                    momentPointsCollection.Add(pointsAccidental);


                }




                // mesh
                int numberOfMeshes = 5;
                positions = strook.Beam.GetPositions(numberOfMeshes);






                // bereken de strook
                List<Punt> mom = [new(0, 0)];
                //double step = strook.Beam.Length / 20;

                var shearZeroPos = strook.Beam.GetShearZeroPosition();
                var minM = strook.Beam.MomentAt(shearZeroPos);

                foreach (var pos in positions)
                {
                    //var shi = strook.Beam.ShearAt(pos);

                    mom.Add(new(pos, strook.Beam.MomentAt(pos)));
                }


                mom.Add(new(strook.Beam.Length, 0));




                var grafiekAmplitude = 50.0 / scale;

                List<Punt> momentData = [];
                foreach (var list in momentPointsCollection)
                {
                    momentData.AddRange(list);
                }



                var scaleY = grafiekAmplitude / 
                    momentData
                    .Select(p => Math.Abs(p.Y))
                    .DefaultIfEmpty(1)
                    .Max();



                var bullits = MakeDiagramCircles(momentData, -scaleY, 3 / scale);
                svgTags.AddRange(bullits);

                foreach (var list in momentPointsCollection)
                {
                    var p = MakePath(list, -scaleY, true);
                    p.Fill = fill;
                    p.Stroke = stroke;

                    svgTags.Add(p);
                }



                var momPath = MakePath(mom, -scaleY);
                momPath.Fill = fill;
                momPath.Stroke = stroke;
                momPath.StrokeWidth = 1;
                //svgPaths.Add(momPath);

                //tags.Add(momPath);

                var minDataPoint = momentData.OrderBy(p => p.Y).FirstOrDefault();
                var maxDataPoint = momentData.OrderBy(p => p.Y).LastOrDefault();
                
                
                if (Math.Abs(minDataPoint.Y) > 0.001)
                {
                    SvgText txtM = new(minDataPoint.Y.ToString("0.00"), x: minDataPoint.X, y: minDataPoint.Y * -scaleY, scale: scale)
                    {
                        DY = 3.0 / scale
                    };
                    if (minDataPoint.Y < 0)
                        txtM.DominantBaseLine = "hanging";
                    else txtM.DominantBaseLine = "base";
                    svgTags.Add(txtM);
                }

                if (Math.Abs(maxDataPoint.Y) > 0.001)
                {
                    SvgText txtM = new(maxDataPoint.Y.ToString("0.00"), x: maxDataPoint.X, y: maxDataPoint.Y * -scaleY, scale: scale)
                    {
                        DY = -3.0 / scale
                    };
                    if (maxDataPoint.Y < 0)
                        txtM.DominantBaseLine = "hanging";
                    else txtM.DominantBaseLine = "base";
                    svgTags.Add(txtM);
                }
            }


            // loop alle onderdelen na op hoogte voor de bb
            var bbCombined = SvgPathBoundingBoxCalculator.GetCombinedBoundingBox(svgTags);

            vbWithMargins = new SvgHelper.SvgViewBox()
            {
                X = vbWithMargins.X,
                Y = bbCombined.MinY,
                Height = bbCombined.Height,
                Width = vbWithMargins.Width
            };

            var marginY = 5.0 / scale;
            bb.MinY = bbCombined.MinY -margin;
            bb.MaxYValue = bbCombined.MaxY +margin;

            // de inner svg xml
            string xml = svgHelper.GetSvgContentXml(svgTags);
            return xml;



        }

        public static void AddSupportToPathCollection(List<BaseSvg> paths, double size1, double size2, BEAM.SupportType type, double pos)
        {
            //var size1 = posY / 2.0;
            //var size2 = size1 * 1.2;
            switch (type)
            {
                case BEAM.SupportType.Pin:
                    SvgGroup g = new();



                    List<Punt> sp = [new(pos, 0), new(pos + size1 / 2.0, size1), new(pos -size1 / 2.0, size1)];
                    
                    
                    g.Add(MakePath(sp));
                    List<Punt> spLine = [new(pos -size2 / 2.0, size2), new(pos + size2 / 2.0, size2)];
                    g.Add(MakePath(spLine));

                    paths.Add(g);
                    
                    break;
                case BEAM.SupportType.Fixed:
                    List<Punt> vLine = [new(pos, size2), new(pos, -size2)];
                    var inklemming = MakePath(vLine);
                    inklemming.StrokeWidth = 3;
                    paths.Add(inklemming);


                    break;
                case BEAM.SupportType.None:
                    // do nothing
                    break;
            }

           

        }


        public static string GenerateKolomSvgXml(KolomEntity kolom, BoundingBox bb, double actualWidthPx, double actualHeightPx, string style="width:auto; height:auto;", bool toonMaatlijnen = true)
        {
            SvgHelper svgHelper = new();
            SvgDocumentInfo? info = new()
            {
                Id = kolom.Id.ToString(),
                Title = kolom.Merk ?? "KOLOM",
                Description = "kolom",
                Label = kolom.Merk ?? "LABEL",
            };

            // 💡maak de paden en maatlijnen
            List<Punt> punten = [new(), new(kolom.Breedte, 0), new(kolom.Breedte, -kolom.Hoogte), new(0, -kolom.Hoogte)];
            List<BaseSvg> svgPaths = [MakePath(punten)]; // ✅ BaseSvg i.p.v. SvgPath voor toekomstige uitbreidingen
            List<SvgDimLine> dimLines = TrapSvgGenerator.GenerateKolomDimLines(kolom);
            List<SvgText> teksten = [];
            // Bepaal de viewBox
            var x = Math.Min(bb.MinX, 0);
            var y = Math.Min(bb.MinY, -kolom.Lengte);
            var w = Math.Max(bb.Width, kolom.Profiel.B); // tijdelijke oplossing, gebruik later een boundingBox voor in assemblage-entiteit.
            var h = Math.Max(bb.Height, kolom.Lengte);
            SvgHelper.SvgViewBox viewBox = new(x, y, w, h);

            // Maak een nieuwe viewbox aan met het aantal regelafstanden in rondom de tekening.
            var vbWithMargins = viewBox.WithMarginsByText(
                leftLines: 5,
                rightLines: 5,
                topLines: 5,
                bottomLines: 5,
                fontSizePx: 12,
                lineHeight: 1.5,
                actualWidthPx: actualWidthPx,
                actualHeightPx: actualHeightPx
            );




            var status = "";
            //bordes.Akkoord ? "" : "has-warning";

            var svg2 = svgHelper.GetSvgStringOptimal(info, vbWithMargins, svgPaths, dimLines, teksten, actualWidthPx, actualHeightPx, style, status);

            if (toonMaatlijnen)
            {
                // later
            }


            return svg2;

        }


        public static string GenerateBordesSvgXml(BordesEntity bordes, BoundingBox bb, double actualWidthPx, double actualHeightPx, string style = "width:auto; height:auto;", bool toonMaatlijnen = true, bool toonWapening = false)
        {
            SvgHelper svgHelper = new();
            SvgDocumentInfo? info = new()
            {
                Id = bordes.Id.ToString(),
                Title = bordes.Merk ?? "TRAP",
                Description = "steektrap",
                Label = bordes.Merk ?? "LABEL",

            };

            // 💡maak de paden en maatlijnen
            List<Punt> punten = [new(),new(bordes.Lengte, 0), new(bordes.Lengte, -bordes.Breedte), new(0, -bordes.Breedte) ];
            var basisPath = MakePath(punten);
            
            // Als wapening getoond wordt, geometrie transparant maken
            if (toonWapening)
            {
                basisPath.FillOpacity = 0.1;
                basisPath.Opacity = 0.1;
            }
            
            List<BaseSvg> svgPaths = [basisPath]; // ✅ Wijziging: BaseSvg i.p.v. SvgPath voor wapening support

            if(bordes.Trap1?.AansluitendElement != null || bordes.Trap1!.GebruikEigenOpgave)
            {
                var c = bordes.Trap1;
                double left = c.Pos.Start;
                double bot = 0;
                double right = c.Pos.End;
                double top = -c.Breedte;

                List<Punt> trap1 = [new(left, bot),new(right,bot), new(right,top), new(left,top)];

                var path1 = MakePath(trap1);
                path1.Fill = "yellow";
                path1.FillOpacity = 0.3;
                
                // Transparant maken bij wapening weergave
                if (toonWapening)
                {
                    path1.FillOpacity = 0.1;
                    path1.Opacity = 0.1;
                }

                svgPaths.Add(path1);
            }
            if (bordes.Trap2?.AansluitendElement != null || bordes.Trap2!.GebruikEigenOpgave)
            {
                var c = bordes.Trap2;
                double left = c.Pos.Start;
                double bot = 0;
                double right = c.Pos.End;
                double top = -c.Breedte;

                List<Punt> trap2 = [new(left, bot), new(right, bot), new(right, top), new(left, top)];

                var path2 = MakePath(trap2);
                path2.Fill = "yellow";
                path2.FillOpacity = 0.3;
                
                // Transparant maken bij wapening weergave
                if (toonWapening)
                {
                    path2.FillOpacity = 0.1;
                    path2.Opacity = 0.1;
                }
                
                svgPaths.Add(path2);
            }


            var vsY1 = -Math.Max(bordes.Trap1.Breedte, bordes.Trap2.Breedte);


            
            var vsY2 = vsY1 - bordes.BreedteVersterkteStrook;
            var vsHartY = (vsY1 + vsY2) / 2.0;


            List<Punt> vs = [new(0, vsY1), new(bordes.Lengte, vsY1), new(bordes.Lengte, vsY2), new (0, vsY2)];
            var vsPath = MakePath(vs);
            vsPath.Fill = "darkblue";
            vsPath.FillOpacity = 0.1;
            vsPath.Stroke = "red";
            vsPath.StrokeDashArray = "60,20,10,20";
            
            // Bij wapening: versterkte strook ook transparant
            if (toonWapening)
            {
                vsPath.Opacity = 0.1;
            }
            
            svgPaths.Add(vsPath);


            var hartY = (vsY1 + vsY2) / 2.0;
            List<Punt> hart = [new(-50, hartY), new(bordes.Lengte + 50, hartY)];
            var hartPath = MakePath(hart);
            hartPath.StrokeWidth = 0.5;
            hartPath.StrokeDashArray = "2,10";
            
            // Bij wapening: hartlijn ook transparant
            if (toonWapening)
            {
                hartPath.Opacity = 0.1;
            }
            
            svgPaths.Add(hartPath);

            var yStart = bordes.Trap1?.Breedte ??  100;
            yStart += 40;
            var breedteOplegging = 120; 

            List<Punt> opLinks = [new(0, -yStart), new(breedteOplegging, -yStart), new(breedteOplegging, -bordes.Breedte), new (0, -bordes.Breedte)];
            var opLinksPath = MakePath(opLinks);
            opLinksPath.Stroke = "red";
            opLinksPath.StrokeDashArray = "5,5";
            opLinksPath.Fill = "none";
            
            // Bij wapening: oplegging ook transparant
            if (toonWapening)
            {
                opLinksPath.Opacity = 0.1;
            }
            
            svgPaths.Insert(0, opLinksPath);

            List<Punt> opRechts = [new(bordes.Lengte, -yStart), new(bordes.Lengte -breedteOplegging, -yStart), new(bordes.Lengte - breedteOplegging, -bordes.Breedte), new(bordes.Lengte, -bordes.Breedte)];
            var opRechtsPath = MakePath(opRechts);
            opRechtsPath.Stroke = "red";
            opRechtsPath.StrokeDashArray = "5,5";
            opRechtsPath.Fill = "none";
            
            // Bij wapening: oplegging ook transparant
            if (toonWapening)
            {
                opRechtsPath.Opacity = 0.1;
            }
            
            svgPaths.Insert(0, opRechtsPath);




            // ✅ Initialiseer dimLines en teksten
            List<SvgDimLine> dimLines = [];
            List<SvgText> teksten = [];

            // ✅ Wapening en verdeellijnen alleen toevoegen als toonWapening = true
            if (toonWapening)
            {
                var (wapeningElements, verdeelLijnen) = TrapSvgGenerator.GenerateBordesWapeningMetVerdeelLijnen(
                    bordes, 
                    toonVerdeelLijnen: true,
                    verdeelLijnXPositie: 0.15); // 15% van de lengte vanaf links
                
                svgPaths.AddRange(wapeningElements);
                dimLines.AddRange(verdeelLijnen);
            }

            // ✅ Normale maatlijnen en teksten NIET toevoegen als wapening getoond wordt
            if (!toonWapening && toonMaatlijnen)
            {
                dimLines.AddRange(TrapSvgGenerator.GenerateBordesDimLines(bordes, vsHartY));
                teksten = TrapSvgGenerator.GenerateBordesSvgText(bordes);
            }

            // Bepaal de viewBox
            var x = Math.Min(bb.MinX, 0);
            var y = Math.Min(bb.MinY, -bordes.Breedte);
            var w = Math.Max(bb.Width, bordes.Lengte); // tijdelijke oplossing, gebruik later een boundingBox voor in assemblage-entiteit.
            var h = Math.Max(bb.Height, bordes.Breedte);
            SvgHelper.SvgViewBox viewBox = new(x, y, w, h);

            // Maak een nieuwe viewbox aan met het aantal regelafstanden in rondom de tekening.
            var vbWithMargins = viewBox.WithMarginsByText(
                leftLines: 5,
                rightLines: 5,
                topLines: 5,
                bottomLines: 5,
                fontSizePx: 12,
                lineHeight: 1.5,
                actualWidthPx: actualWidthPx,
                actualHeightPx: actualHeightPx
            );




            var status = "";
            //bordes.Akkoord ? "" : "has-warning";

            // ✅ Geen OfType<SvgPath> filter meer nodig! GetSvgStringOptimal accepteert nu BaseSvg
            var svg2 = svgHelper.GetSvgStringOptimal(info, vbWithMargins, svgPaths, dimLines, teksten, actualWidthPx, actualHeightPx, style, status);


            return svg2;
        }


    }


    public static class TrapSvgGenerator
    {
        /// <summary>
        /// Genereert een buigvorm (polyline) voor wapeningsdetails met optionele beenlengte-annotaties.
        /// </summary>
        /// <param name="punten">Lijst van (x,y) coördinaten die de buigvorm definiëren</param>
        /// <param name="x">X-positie voor plaatsing van de buigvorm</param>
        /// <param name="y">Y-positie voor plaatsing van de buigvorm</param>
        /// <param name="schaal">Schaalfactor (default: null = automatisch schalen naar boundingBox)</param>
        /// <param name="boundingBox">Rechthoek waarbinnen de buigvorm moet passen (width, height)</param>
        /// <param name="rotatie">Rotatie in graden (0=horizontaal, 90=rechtsom, -90=linksom, 180=gespiegeld)</param>
        /// <param name="toonBeenLengtes">Toon lengtes van elk been van de buigvorm</param>
        /// <param name="kleur">Kleur van de lijn</param>
        /// <param name="strokeWidth">Lijndikte</param>
        /// <returns>Lijst van BaseSvg objecten (polyline + optioneel teksten)</returns>
        public static List<BaseSvg> GenerateBuigvorm(
            List<(double x, double y)> punten,
            double x = 0,
            double y = 0,
            double? schaal = null,
            (double width, double height)? boundingBox = null,
            double rotatie = 0,
            bool toonBeenLengtes = false,
            string kleur = "black",
            double strokeWidth = 2)
        {
            List<BaseSvg> result = [];
            
            if (punten == null || punten.Count < 2)
                return result;

            // Bereken lokale bounding box van de vorm
            double minX = punten.Min(p => p.x);
            double maxX = punten.Max(p => p.x);
            double minY = punten.Min(p => p.y);
            double maxY = punten.Max(p => p.y);
            
            double vormWidth = maxX - minX;
            double vormHeight = maxY - minY;
            
            // Bepaal schaal
            double effectieveSchaal = schaal ?? 1.0;
            
            if (!schaal.HasValue && boundingBox.HasValue)
            {
                // Automatisch schalen naar bounding box
                double schaalX = boundingBox.Value.width / vormWidth;
                double schaalY = boundingBox.Value.height / vormHeight;
                effectieveSchaal = Math.Min(schaalX, schaalY);
            }
            
            // Normaliseer punten (startpunt op 0,0)
            var genormaliseerdePunten = punten
                .Select(p => (x: (p.x - minX) * effectieveSchaal, y: (p.y - minY) * effectieveSchaal))
                .ToList();
            
            // Rotatie toepassen (in radialen)
            double theta = rotatie * Math.PI / 180.0;
            double cosTheta = Math.Cos(theta);
            double sinTheta = Math.Sin(theta);
            
            var geroteerdePunten = genormaliseerdePunten
                .Select(p => (
                    x: p.x * cosTheta - p.y * sinTheta,
                    y: p.x * sinTheta + p.y * cosTheta
                ))
                .ToList();
            
            // Translatie naar eindpositie
            var finalePunten = geroteerdePunten
                .Select(p => new Punt(x + p.x, y + p.y))
                .ToList();
            
            // Maak polyline path
            var buigvormPath = SvgGenerator.MakePath(finalePunten, scaleY: 1, close: false);
            buigvormPath.Stroke = kleur;
            buigvormPath.StrokeWidth = strokeWidth;
            buigvormPath.Fill = "none";
            result.Add(buigvormPath);
            
            // Optioneel: beenlengte labels
            if (toonBeenLengtes)
            {
                for (int i = 0; i < finalePunten.Count - 1; i++)
                {
                    var p1 = finalePunten[i];
                    var p2 = finalePunten[i + 1];
                    
                    // Bereken lengte van dit been
                    double dx = p2.X - p1.X;
                    double dy = p2.Y - p1.Y;
                    double lengte = Math.Sqrt(dx * dx + dy * dy) / effectieveSchaal; // Terug naar originele schaal
                    
                    // Middenpunt van been
                    double midX = (p1.X + p2.X) / 2.0;
                    double midY = (p1.Y + p2.Y) / 2.0;
                    
                    // Hoek van been (voor tekstrotatie)
                    double beenHoek = Math.Atan2(dy, dx) * 180.0 / Math.PI;
                    
                    // Normaliseer hoek voor leesbaarheid (voorkom ondersteboven tekst)
                    if (beenHoek > 90) beenHoek -= 180;
                    if (beenHoek < -90) beenHoek += 180;
                    
                    var lengteTekst = new SvgText(
                        $"{lengte:0}",
                        x: midX,
                        y: midY,
                        angle: beenHoek,
                        scale: 1.0
                    )
                    {
                        Pts = 6, // Kleine tekst
                        Anchor = "middle",
                        DominantBaseLine = "middle",
                        Fill = kleur,
                        DY = -3 // Iets boven de lijn
                    };
                    
                    result.Add(lengteTekst);
                }
            }
            
            return result;
        }

        /// <summary>
        /// Genereert driehoeken voor wapeningslaag-indicatoren
        /// </summary>
        /// <param name="x">X-positie startpunt</param>
        /// <param name="y">Y-positie basislijn</param>
        /// <param name="aantalOnder">Aantal driehoeken voor onderwapening (naar boven wijzend ▲)</param>
        /// <param name="aantalBoven">Aantal driehoeken voor bovenwapening (naar beneden wijzend ▼)</param>
        /// <param name="grootte">Grootte van de driehoeken</param>
        /// <param name="rotatie">Rotatie in graden (0=horizontaal, 90=verticaal rechtsom, -90=verticaal linksom)</param>
        /// <param name="kleurOnder">Kleur voor onderwapening driehoeken</param>
        /// <param name="kleurBoven">Kleur voor bovenwapening driehoeken</param>
        /// <param name="spacing">Horizontale afstand tussen driehoeken (default = grootte)</param>
        /// <returns>Lijst van SvgPolygon objecten</returns>
        public static List<SvgPolygon> GenerateWapeningDriehoeken(
            double x, 
            double y, 
            int aantalOnder,
            int aantalBoven,
            double grootte = 40,
            double rotatie = 0,
            string kleurOnder = "black",
            string kleurBoven = "black",
            double? spacing = null)
        {
            List<SvgPolygon> driehoeken = [];
            double afstand = spacing ?? grootte;
            
            // Helper functie voor het maken van een driehoek
            SvgPolygon MaakDriehoek(double xPos, double yPos, bool naarBoven, string kleur)
            {
                string points;
                
                if (rotatie == 0)
                {
                    // Horizontale wapening
                    if (naarBoven)
                    {
                        // ▲ Driehoek wijst naar boven
                        points = $"{xPos},{yPos - grootte} " +
                                $"{xPos - grootte/2},{yPos} " +
                                $"{xPos + grootte/2},{yPos}";
                    }
                    else
                    {
                        // ▼ Driehoek wijst naar beneden
                        points = $"{xPos},{yPos + grootte} " +
                                $"{xPos - grootte/2},{yPos} " +
                                $"{xPos + grootte/2},{yPos}";
                    }
                }
                else if (Math.Abs(rotatie - 90) < 0.1 || Math.Abs(rotatie + 270) < 0.1)
                {
                    // Verticale wapening (90° rechtsom) - driehoek wijst naar rechts
                    if (naarBoven) // eigenlijk "naar rechts" in dit geval
                    {
                        // ► Driehoek wijst naar rechts
                        points = $"{xPos + grootte},{yPos} " +
                                $"{xPos},{yPos - grootte/2} " +
                                $"{xPos},{yPos + grootte/2}";
                    }
                    else
                    {
                        // ◄ Driehoek wijst naar links
                        points = $"{xPos - grootte},{yPos} " +
                                $"{xPos},{yPos - grootte/2} " +
                                $"{xPos},{yPos + grootte/2}";
                    }
                }
                else if (Math.Abs(rotatie + 90) < 0.1 || Math.Abs(rotatie - 270) < 0.1)
                {
                    // Verticale wapening (-90° linksom) - driehoek wijst naar links
                    if (naarBoven) // eigenlijk "naar links" in dit geval
                    {
                        // ◄ Driehoek wijst naar links
                        points = $"{xPos - grootte},{yPos} " +
                                $"{xPos},{yPos - grootte/2} " +
                                $"{xPos},{yPos + grootte/2}";
                    }
                    else
                    {
                        // ► Driehoek wijst naar rechts
                        points = $"{xPos + grootte},{yPos} " +
                                $"{xPos},{yPos - grootte/2} " +
                                $"{xPos},{yPos + grootte/2}";
                    }
                }
                else
                {
                    // Fallback voor andere hoeken - gebruik horizontaal
                    if (naarBoven)
                    {
                        points = $"{xPos},{yPos - grootte} " +
                                $"{xPos - grootte/2},{yPos} " +
                                $"{xPos + grootte/2},{yPos}";
                    }
                    else
                    {
                        points = $"{xPos},{yPos + grootte} " +
                                $"{xPos - grootte/2},{yPos} " +
                                $"{xPos + grootte/2},{yPos}";
                    }
                }
                
                return new SvgPolygon
                {
                    Points = points,
                    Fill = kleur,
                    Stroke = kleur,
                    StrokeWidth = 1
                };
            }
            
            double currentX = x;
            double currentY = y;
            
            // Genereer driehoeken voor onderwapening (laag 1 en 2)
            for (int i = 0; i < aantalOnder; i++)
            {
                if (rotatie == 0)
                {
                    // Horizontaal: driehoeken naast elkaar
                    driehoeken.Add(MaakDriehoek(currentX, y, true, kleurOnder));
                    currentX += afstand;
                }
                else
                {
                    // Verticaal: driehoeken onder elkaar
                    driehoeken.Add(MaakDriehoek(x, currentY, true, kleurOnder));
                    currentY += afstand;
                }
            }
            
            // Genereer driehoeken voor bovenwapening
            for (int i = 0; i < aantalBoven; i++)
            {
                if (rotatie == 0)
                {
                    // Horizontaal: verder naast de onderwapening
                    driehoeken.Add(MaakDriehoek(currentX, y, false, kleurBoven));
                    currentX += afstand;
                }
                else
                {
                    // Verticaal: verder onder de onderwapening
                    driehoeken.Add(MaakDriehoek(x, currentY, false, kleurBoven));
                    currentY += afstand;
                }
            }
            
            return driehoeken;
        }

        public static List<BaseSvg> GenerateSimpleLoadScheme(double x1, double y1,
            double x2, double y2, 
            List<string> distrubutedLoads, string pointLoad,
            string profiel,
            double scale = 1.0)
        {
            List<BaseSvg> result = [];

            // 1. Ligger als lijn
            result.Add(new SvgLine
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                StrokeWidth = 3
            });

            // 2. Gelijkzijdige driehoeken op steunpunten (punt naar boven ▲)
            double triH = 1.4 * 12.0 / scale;
            double triHalfBase = triH / Math.Sqrt(3.0);
            

            foreach (var (sx, sy) in new[] { (x1, y1), (x2, y2) })
            {
                string triPts =
                    $"{sx.ToString("F4", CultureInfo.InvariantCulture)},{sy.ToString("F4", CultureInfo.InvariantCulture)} " +
                    $"{(sx - triHalfBase).ToString("F4", CultureInfo.InvariantCulture)},{(sy + triH).ToString("F4", CultureInfo.InvariantCulture)} " +
                    $"{(sx + triHalfBase).ToString("F4", CultureInfo.InvariantCulture)},{(sy + triH).ToString("F4", CultureInfo.InvariantCulture)}";

                result.Add(new SvgPolygon
                {
                    Points = triPts,
                    Fill = "none",
                    Stroke = "var(--neutral-foreground-rest, black)",
                    StrokeWidth = 1
                });
            }

            // 3. Rechthoek met belastinglabel boven het midden van de ligger
            double xMid = (x1 + x2) / 2.0;
            double rectW = (x2 - x1);
            double rectH = 2.0 * 12.0 / scale;
            double rectX = x1;
            double rectY = Math.Min(y1, y2) - 0.0 * rectH;

            foreach (var tekst in distrubutedLoads)
            {
                if (string.IsNullOrWhiteSpace(tekst))
                {
                    continue; // overslaan
                }
                rectY -= rectH;

                result.Add(new SvgRect
                {
                    X = rectX,
                    Y = rectY,
                    Width = rectW,
                    Height = rectH,
                    Fill = "none",
                    Stroke = "var(--neutral-foreground-rest, black)",
                    StrokeWidth = 1
                });



                result.Add(new SvgText(
                    text: tekst,
                    x: xMid,
                    y: rectY + rectH / 2.0,
                    scale: scale
                )
                {
                    Anchor = "middle",
                    DominantBaseLine = "middle"
                });
            }

            // 4. Puntlast pijl in het midden van de ligger
            if (!string.IsNullOrWhiteSpace(pointLoad))
            {
                double yMid = rectY;
                double arrowH = rectH * 1.0;

                // schacht
                result.Add(new SvgLine
                {
                    X1 = xMid,
                    Y1 = yMid - arrowH,
                    X2 = xMid,
                    Y2 = yMid,
                    Stroke = "var(--neutral-foreground-rest, black)",
                    StrokeWidth = 1
                });

                // pijlpunt (▼ wijst naar de ligger)
                string arrowPts =
                    $"{xMid.ToString("F4", CultureInfo.InvariantCulture)},{yMid.ToString("F4", CultureInfo.InvariantCulture)} " +
                    $"{(xMid - triHalfBase * 0.5).ToString("F4", CultureInfo.InvariantCulture)},{(yMid - triH * 0.5).ToString("F4", CultureInfo.InvariantCulture)} " +
                    $"{(xMid + triHalfBase * 0.5).ToString("F4", CultureInfo.InvariantCulture)},{(yMid - triH * 0.5).ToString("F4", CultureInfo.InvariantCulture)}";

                result.Add(new SvgPolygon
                {
                    Points = arrowPts,
                    Fill = "var(--neutral-foreground-rest, black)",
                    Stroke = "var(--neutral-foreground-rest, black)",
                    StrokeWidth = 1
                });

                result.Add(new SvgText(
                    pointLoad,
                    x: xMid,
                    y: yMid - arrowH,
                    scale: scale
                )
                {
                    Anchor = "middle",
                    DominantBaseLine = "base"
                });
            }

            if (!string.IsNullOrEmpty(profiel))
            {
                var y = (y1 + y2) / 2.0;

                var angle = Math.Atan2(y2-y1, x2-x1) * (180 / Math.PI);

               
                var txtP = new SvgText(
                    text: profiel,
                    x: xMid,
                    y: y,
                    scale: scale,
                    angle: angle
                )
                {
                    Anchor = "middle",
                    DominantBaseLine = "hanging"
                };
                result.Add(txtP);
            }




            return result;
        }


        public static List<BaseSvg> GenerateTrapPaths(SteekTrapEntity trap, BoundingBox bb)
        {
            List<BaseSvg> returnList = [];

            // referentie onderkant
            //returnList.Add(GenerateBottomRef(trap));
            //returnList.Add(GenerateTopRef(trap));



            // doorsnede
            returnList.Add(GenerateTrapSvgPath(trap, bb));

            // hulplijn (looplijn)
            var looplijn = GenerateLooplijn(trap);
            if (looplijn != null) returnList.Add(looplijn);

            // onderwapening
            //var onderWapening = GenerateTrapOnderWapeningPath(trap);
            //if (onderWapening != null) returnList.Add(onderWapening);


            // trapboom
            if (trap.DragendeTrapBomen)
            {
                var trapBoom = GenerateTrapboom(trap);
                if (trapBoom != null) returnList.Add(trapBoom);
            }
           



            return returnList;

        }


        public static async Task<(string SvgXml, string? Base64Png)> GenerateTrapSvgXmlWithPngAsync(
           IJSRuntime jsRuntime,
           SteekTrapEntity steekTrap,
           BoundingBox boundingBox,
           double widthPx,
           double heightPx,
           string style)
        {
            // ---- 1. Genereer SVG
            string svgXml = GenerateTrapSvgXml(steekTrap, boundingBox, widthPx, heightPx, style);

            // ---- 2. Vraag aan JS om deze SVG om te zetten naar base64 PNG
            string? base64Png = null;
            try
            {
                base64Png = await jsRuntime.InvokeAsync<string>(
                    "svgHelpers.convertToBase64Png",
                    svgXml,
                    (int)widthPx,
                    (int)heightPx
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Fout bij SVG → PNG conversie: {ex.Message}");
            }

            // ---- 3. Retourneer tuple
            return (svgXml, base64Png);
        }


   


        public static (string SvgXml, string? Base64Png) GenerateTrapSvgXmlWithPng(
            SteekTrapEntity steekTrap,
            BoundingBox boundingBox,
            double widthPx,
            double heightPx,
            string style)
        {
            // ---- 1. Genereer SVG XML
            string svgXml = GenerateTrapSvgXml(steekTrap, boundingBox, widthPx, heightPx, style);

            // ---- 2. Base64 PNG komt later van de Blazor JS interop
            string? base64Png = null;

            // ---- 3. Retourneer beide
            return (svgXml, base64Png);
        }

       




        /// <summary>
        /// Genereert een bovenaanzicht (BAZ) van de steektrap.
        /// Toont een rechthoek met verticale lijnen voor elk hoekpunt uit de doorsnede.
        /// </summary>
        public static string GenerateTrapBazSvgXml(
            SteekTrapEntity trap, 
            BoundingBox bb, 
            double actualWidthPx, 
            double actualHeightPx,
            double trapBreedte = 1000,
            string style = "width:auto; height:auto;", 
            bool toonMaatlijnen = true)
        {
            SvgHelper svgHelper = new();
            SvgDocumentInfo? info = new()
            {
                Id = trap.Id.ToString(),
                Title = $"{trap.Merk ?? "TRAP"} - Bovenaanzicht",
                Description = "steektrap bovenaanzicht",
                Label = trap.Merk ?? "BAZ",
            };

            List<BaseSvg> svgElements = [];
            
            // ✅ Hoofdrechthoek (beton element bovenaanzicht)
            var rect = new SvgRect
            {
                X = 0,
                Y = 0,
                Width = trap.LtProjZ,
                Height = trapBreedte,
                Fill = "lightgray",
                Stroke = "black",
                StrokeWidth = 2
            };
            svgElements.Add(rect);
            
            if (!trap.GebruikEigenLengte)
            {
                // ✅ Extraheer X-posities uit doorsnede hoekpunten
                var xPositions = ExtractXPositionsFromDoorsnede(trap);
                
                foreach (var (xPos, isVerborgen) in xPositions)
                {
                    var lijn = new SvgLine
                    {
                        X1 = xPos,
                        Y1 = 0,
                        X2 = xPos,
                        Y2 = trapBreedte,
                        Stroke = isVerborgen ? "gray" : "black",
                        StrokeWidth = isVerborgen ? 0.5 : 1,
                        StrokeDashArray = isVerborgen ? "5,5" : ""
                    };
                    svgElements.Add(lijn);
                }
            }
            
            // Maatlijnen
            List<SvgDimLine> dimLines = [];
            if (toonMaatlijnen)
            {
                dimLines = GenerateTrapBazDimLines(trap, trapBreedte);
            }
            
            // ViewBox berekenen
            var x = Math.Min(bb.MinX, 0);
            var y = Math.Min(bb.MinY, 0);
            var w = Math.Max(bb.Width, trap.LtProjZ);
            var h = Math.Max(bb.Height, trapBreedte);
            SvgHelper.SvgViewBox viewBox = new(x, y, w, h);
            
            var vbWithMargins = viewBox.WithMarginsByText(
                leftLines: 3,
                rightLines: 3,
                topLines: 3,
                bottomLines: 3,
                fontSizePx: 12,
                lineHeight: 1.5,
                actualWidthPx: actualWidthPx,
                actualHeightPx: actualHeightPx
            );
            
            bb.MinX = vbWithMargins.X;
            bb.MinY = vbWithMargins.Y;
            bb.MaxXValue = vbWithMargins.X + vbWithMargins.Width;
            bb.MaxYValue = vbWithMargins.Y + vbWithMargins.Height;
            
            List<SvgText> teksten = [];
            var status = trap.Akkoord ? "" : "has-warning";
            
            var svg = svgHelper.GetSvgStringOptimal(
                info, vbWithMargins, svgElements, dimLines, teksten, 
                actualWidthPx, actualHeightPx, style, status);
            
            return svg;
        }

        /// <summary>
        /// Extraheert X-posities van hoekpunten uit de doorsnede.
        /// Retourneert (X, IsVerborgen) tuples.
        /// </summary>
        private static List<(double X, bool IsVerborgen)> ExtractXPositionsFromDoorsnede(SteekTrapEntity trap)
        {
            var positions = new List<(double X, bool IsVerborgen)>();
            
            if (trap.GebruikEigenLengte)
            {
                // Simpel geval: alleen start en eind
                positions.Add((0, false));
                positions.Add((trap.LtProjZ, false));
                return positions;
            }
            
            double wel = trap.WelMaat;
            
            // ✅ Voorkant (altijd zichtbaar)
            positions.Add((0, false));
            
            // ✅ Hoekpunten van elke trede
            for (int i = 0; i < trap.OptredeAantal1; i++)
            {
                double xTrede = i * trap.AantredeMaat;
                
                // Boven optrede (lijn aan bovenkant trede - ZICHTBAAR vanaf trede 1)
                if (i > 0)
                {
                    positions.Add((xTrede - wel, false));
                }
                
                // Onder optrede (lijn aan onderkant trede - VERBORGEN)
                double xOnderTrede = xTrede + trap.AantredeMaat;
                
                // Laatste trede heeft geen verborgen lijn
                if (i < trap.OptredeAantal1 - 1)
                {
                    positions.Add((xOnderTrede - wel, true));
                }
            }
            
            // ✅ Achterkant (altijd zichtbaar)
            double tandLengte = trap.TandOpleggingBovenzijde?.TandLengte ?? 0;
            positions.Add((trap.LtProjZ - tandLengte, false));
            
            if (tandLengte > 0)
            {
                // Tand voorkant
                positions.Add((trap.LtProjZ, false));
            }
            
            return [.. positions.OrderBy(p => p.X)];
        }

        /// <summary>
        /// Genereert maatlijnen voor het bovenaanzicht.
        /// </summary>
        private static List<SvgDimLine> GenerateTrapBazDimLines(SteekTrapEntity trap, double breedte)
        {
            List<SvgDimLine> dimLines = [];
            
            // Totale lengte (horizontaal)
            dimLines.Add(new SvgDimLine
            {
                Mode = DimLineMode.Horizontal,
                X1 = 0,
                Y1 = 0,
                X2 = trap.LtProjZ,
                Y2 = 0,
                OffsetLines = -2,
                Text = $"{trap.LtProjZ:0}",
                StrokeColor = "var(--neutral-foreground-rest, black)"
            });
            
            // Breedte (verticaal)
            dimLines.Add(new SvgDimLine
            {
                Mode = DimLineMode.Vertical,
                X1 = 0,
                Y1 = 0,
                X2 = 0,
                Y2 = breedte,
                OffsetLines = 2,
                Text = $"{breedte:0}",
                StrokeColor = "var(--neutral-foreground-rest, black)"
            });
            
            // Optioneel: aantredemaatjes
            if (!trap.GebruikEigenLengte && trap.OptredeAantal1 > 1)
            {
                dimLines.Add(new SvgDimLine
                {
                    Mode = DimLineMode.Horizontal,
                    X1 = 0,
                    Y1 = breedte,
                    X2 = trap.AantredeMaat,
                    Y2 = breedte,
                    OffsetLines = 2,
                    Text = $"{trap.AantredeMaat:0}",
                    StrokeColor = "var(--neutral-foreground-rest, black)"
                });
            }
            
            return dimLines;
        }

        public static string GenerateTrapSvgXml(SteekTrapEntity trap, BoundingBox bb, 
            double actualWidthPx, double actualHeightPx, string style = "width:auto; height:auto;", 
            bool toonMaatlijnen = true, 
            bool toonBaz = !true, 
            double trapBreedte = 1000)
        {

            bool toonSchema = true;


            SvgHelper svgHelper = new();
            SvgDocumentInfo? info = new()
            {
                Id = trap.Id.ToString(),
                Title = trap.Merk ?? "TRAP",
                Description = "steektrap",
                Label = trap.Merk ?? "LABEL",

            };

            List<BaseSvg>? svgPaths = [];

            // 💡maak de paden en maatlijnen voor doorsnede
            if (!toonSchema)
            {
                svgPaths = GenerateTrapPaths(trap, bb);
            }



            if (toonSchema)
            {
                var xPre = Math.Min(bb.MinX, 0);
                var yPre = Math.Min(bb.MinY, -trap.HoogteTotaal);
                var wPre = Math.Max(bb.Width, trap.LtProjZ + trap.WelMaat);
                var hPre = Math.Max(bb.Height, trap.HoogteTotaal);
                var vbPrelim = new SvgHelper.SvgViewBox(xPre, yPre, wPre, hPre)
                    .WithMarginsByText(
                        leftLines: 5, rightLines: 5, topLines: 0, bottomLines: 8,
                        fontSizePx: 12, lineHeight: 1.5,
                        actualWidthPx: actualWidthPx, actualHeightPx: actualHeightPx);
                double scalePrelim = vbPrelim.GetScale(actualWidthPx, actualHeightPx);
                var prof = $"1000×{(trap.MainSlab?.Dikte ?? 0):0 mm}";

                svgPaths.AddRange(
                    GenerateSimpleLoadScheme(0, 0, trap.LtProjZ, -trap.HoogteTotaal, 
                    [
                        "e.g. = " + trap.Krachten.EigenGewicht.ToString("0.0 kN/m"),
                        (trap.AfwerkingVlaklast > 0? $"afwerking = {trap.AfwerkingVlaklast:0.0 kN/m}" : ""),
                        "q_k = " + trap.Krachten.Lijnlast_qk.ToString("0.0 kN/m")
                        ], 
                    "Q_k = " + trap.Krachten.Puntlast_Qk.ToString("0.0 kN"), prof, scalePrelim));
            }


            List<SvgDimLine> dimLines = [];

            if (toonSchema)
            {
                dimLines = GenerateSimpleSchemaDimLines(trap);
            }


            if (toonMaatlijnen && !toonSchema)
                dimLines = GenerateTrapDimLines(trap, toonSchema);

            // ✅ Voeg BAZ toe onder de doorsnede indien gewenst
            double bazYOffset = 0;
            List<BaseSvg> bazElements = [];
            
            if (toonBaz)
            {
                // Offset berekenen: hoogte doorsnede + ruimte tussen views
                bazYOffset = trap.HoogteTotaal + 200; // 200mm ruimte tussen doorsnede en BAZ
                
                // BAZ rechthoek
                var bazRect = new SvgRect
                {
                    X = 0,
                    Y = bazYOffset,
                    Width = trap.LtProjZ,
                    Height = trapBreedte,
                    Fill = "lightgray",
                    Stroke = "black",
                    StrokeWidth = 2
                };
                bazElements.Add(bazRect);
                
                // BAZ verticale lijnen (hoekpunten)
                if (!trap.GebruikEigenLengte)
                {
                    var xPositions = ExtractXPositionsFromDoorsnede(trap);
                    
                    foreach (var (xPos, isVerborgen) in xPositions)
                    {
                        var lijn = new SvgLine
                        {
                            X1 = xPos,
                            Y1 = bazYOffset,
                            X2 = xPos,
                            Y2 = bazYOffset + trapBreedte,
                            Stroke = isVerborgen ? "gray" : "black",
                            StrokeWidth = isVerborgen ? 0.5 : 1,
                            StrokeDashArray = isVerborgen ? "5,5" : ""
                        };
                        bazElements.Add(lijn);
                    }
                }
                
                // BAZ maatlijnen
                if (toonMaatlijnen)
                {
                    var bazDimLines = GenerateTrapBazDimLines(trap, trapBreedte);
                    
                    // Offset alle maatlijnen met bazYOffset
                    foreach (var dimLine in bazDimLines)
                    {
                        dimLine.Y1 += bazYOffset;
                        dimLine.Y2 += bazYOffset;
                    }
                    
                    dimLines.AddRange(bazDimLines);
                }
                
                // Voeg BAZ elementen toe aan svgPaths
                svgPaths.AddRange(bazElements);
                
                // Voeg "Bovenaanzicht" tekst toe
                var bazTitel = new SvgText(
                    "Bovenaanzicht",
                    x: trap.LtProjZ / 2,
                    y: bazYOffset - 50,
                    scale: 1.0
                )
                {
                    Anchor = "middle",
                    DominantBaseLine = "auto"
                };
                svgPaths.Add(bazTitel);
            }

            // Bepaal de viewBox (inclusief BAZ indien van toepassing)
            var x = Math.Min(bb.MinX, 0);
            var y = Math.Min(bb.MinY, -trap.HoogteTotaal);
            var w = Math.Max(bb.Width, trap.LtProjZ + trap.WelMaat);
            var h = Math.Max(bb.Height, trap.HoogteTotaal);
            
            if (toonBaz)
            {
                // Vergroot viewbox om BAZ te omvatten
                h = Math.Max(h, bazYOffset + trapBreedte);
                w = Math.Max(w, trap.LtProjZ);
            }
            
            SvgHelper.SvgViewBox viewBox = new(x, y, w, h);

            // Maak een nieuwe viewbox aan met het aantal regelafstanden in rondom de tekening.
            var vbWithMargins = viewBox.WithMarginsByText(
                leftLines: 6,
                rightLines: 6,
                topLines: 10,
                bottomLines: 6,
                fontSizePx: 12,
                lineHeight: 1.5,
                actualWidthPx: actualWidthPx,
                actualHeightPx: actualHeightPx
            );

            List<SvgText> teksten = [];
            if (!toonSchema)
            {
                teksten = GenerateTrapTeksten(trap, bb, vbWithMargins.GetScale(actualWidthPx, actualHeightPx), false);
            }

            var status = trap.Akkoord ? "" : "has-warning";

            var svg2 = svgHelper.GetSvgStringOptimal(info, vbWithMargins, svgPaths, dimLines, teksten, actualWidthPx, actualHeightPx, style, status);

            return svg2;
        }


        public static List<SvgText> GenerateTrapTeksten(SteekTrapEntity trap, BoundingBox bb, double scale, bool toonWapTekst = true)
        {
            List<SvgText> returnList = [];


           
           

            //returnList.Add(txt);

            if (!trap.GebruikEigenLengte)
            {
                var dx = 20 - trap.WelMaat * 0.5;
                var dy = -trap.OptredeMaat - 20;
                for (int n = 0; n < trap.OptredeAantal1; n++)
                {
                    var x = 0 + n * trap.AantredeMaat + dx;
                    var y = 0 - n * trap.OptredeMaat + dy;
                    bb.Add(x, y);
                    returnList.Add(new()
                    {
                        X = x,
                        Y = y,
                        Text = $"{n + 1}",
                        DominantBaseLine = "base",
                        Anchor = "left"
                    });
                }
            }

            if (toonWapTekst)
            {
                // toon wapeningstekst bij de trap
                var x1 = trap.LtProjZ / 2;
                var y1 = -trap.HoogteTotaal / 2 + 2 * trap.SchilDikte;
                var dy = 12 / scale;

                var wapTekstBoven = new SvgText($"boven : {trap.MainSlab?.PlaatWapening?.Boven?.BasisWapening.SanitizedTekst()}", x1, y1)
                {
                    Fill = "var(--accent-foreground-rest, black)",
                    FontFamily = "consolas",
                    Anchor = "left",
                    DominantBaseLine = "hanging"
                };
                bb.Add(x1, y1);
                returnList.Add(wapTekstBoven);

                y1 += dy;
                var wapTekst = new SvgText($"onder : {trap.MainSlab?.PlaatWapening?.Onder?.BasisWapening.SanitizedTekst()}", x1, y1)
                {
                    FontFamily = "consolas",
                    Fill = "var(--accent-foreground-rest, black)",
                    Anchor = "left",
                    DominantBaseLine = "hanging"
                };
                bb.Add(x1, y1);
                returnList.Add(wapTekst);

                y1 += dy;
                var wapTekstDetails = new SvgText($"details : HS{trap.TandOpleggingBovenzijde?.WapeningAlgemeen.SanitizedTekst()}", x1, y1)
                {
                    Fill = "var(--accent-foreground-rest, black)",
                    FontFamily = "consolas",
                    Anchor = "left",
                    DominantBaseLine = "hanging"
                };
                bb.Add(x1, y1);
                returnList.Add(wapTekstDetails);
            }

            return returnList;

        }



        public static List<SvgText> GenerateBordesSvgText(BordesEntity bordes)
        {
            List<SvgText> list = [];

            if (bordes.Trap1 != null)
            {
                var c = bordes.Trap1;
                
                if (c.AansluitendElement != null || c.GebruikEigenOpgave)
                    list.Add(new SvgText($"q={c.Reacties.G:0.0} ({c.Reacties.Q:0.0})" , c.Randafstand, 10, c.Randafstand + c.Lengte, 10) { DominantBaseLine = "hanging"});
                
                double yVS = c.Breedte + bordes.BreedteVersterkteStrook / 2.0;
                list.Add(new SvgText("versterkte strook",x: 0, y: -yVS, x2:bordes.Lengte, y2:-yVS, scale:1));
            }

            if (bordes.Trap2 != null)
            {
                var c = bordes.Trap2;

                if (c.AansluitendElement != null || c.GebruikEigenOpgave)
                    list.Add(new SvgText( $"q={c.Reacties.G:0.0} ({c.Reacties.Q:0.0})", bordes.Lengte - c.Randafstand - c.Lengte, 10, bordes.Lengte -  c.Randafstand, 10) { DominantBaseLine = "hanging"});
            }

            
            list.Add(new SvgText("basisstrook",x: 0, y: -bordes.Breedte + 500, x2:bordes.Lengte, y2:-bordes.Breedte +500, scale:1));



            return list;
        }


        /// <summary>
        /// Genereert wapening als SVG elementen voor een bordes.
        /// Toont wapening voor basisstrook (onder) en versterkte strook (boven).
        /// </summary>
        /// <param name="bordes">Het bordes waarvoor wapening wordt gegenereerd</param>
        /// <param name="toonVerdeelLijnen">Toon maatlijnen voor wapeningsposities</param>
        /// <param name="verdeelLijnXPositie">X-positie voor verticale verdeellijnen (relatief: 0.0=links, 0.5=midden, 1.0=rechts)</param>
        /// <returns>Tuple met wapening elementen en optionele verdeellijnen</returns>
        public static (List<BaseSvg> WapeningElements, List<SvgDimLine> VerdeelLijnen) GenerateBordesWapeningMetVerdeelLijnen(
            BordesEntity bordes, 
            bool toonVerdeelLijnen = true,
            double verdeelLijnXPositie = 0.15)
        {
            List<BaseSvg> wapeningElements = [];
            List<SvgDimLine> verdeelLijnen = [];
            double scale = 1.0; // teksten worden runtime verschaald, hier op 1 laten staan.

            // ✅ DEBUG: Log om te zien of deze methode wordt aangeroepen
            //Console.WriteLine($"[DEBUG] GenerateBordesWapeningMetVerdeelLijnen aangeroepen voor bordes {bordes.Merk}");
            //Console.WriteLine($"[DEBUG] PlaatWapening is null: {bordes.PlaatWapening == null}");
            
            if (bordes.PlaatWapening == null)
            {
                Console.WriteLine($"⚠️ [DEBUG] PlaatWapening is NULL! Geen wapening toegevoegd.");
                return (wapeningElements, verdeelLijnen);
            }
            
            //Console.WriteLine($"[DEBUG] PlaatWapening.Onder is null: {bordes.PlaatWapening.Onder == null}");
            //Console.WriteLine($"[DEBUG] PlaatWapening.Boven is null: {bordes.PlaatWapening.Boven == null}");

            // Bereken X-positie voor verticale verdeellijnen
            double xVerdeel = verdeelLijnXPositie;
            string kleurBoven = "gray";
            string kleurOnder = "black";

            // ===============================================
            // 1️⃣ BASISSTROOK WAPENING (ONDER)
            // ===============================================
            if (bordes.PlaatWapening?.Onder?.BasisWapening != null)
            {
                toonVerdeelLijnen = false;
                var wapOnder = bordes.PlaatWapening.Onder.BasisWapening;
                var wapOnderVerdeel = bordes.PlaatWapening.Onder.VerdeelWapening;


            

                double staafLengte = 600;

                double xCenter = bordes.Lengte / 2.0;
                double yCenter = -bordes.Breedte / 2.0;
                double x1 = xCenter - staafLengte / 2.0;
                double x2 = xCenter + staafLengte / 2.0;
                double yBasis = yCenter - 100;
                double y1 = yBasis + staafLengte / 2.0;
                double y2 = yBasis - staafLengte / 2.0;


                // Parse wapening tekst (bijv. "r8-150" of "r8-150+r6-500")
                int laagNummerOnder = wapOnder.LaagNummer ?? 1;
                int laagNummerBoven = 0;
                string wapeningTekstOnder = $"{wapOnder}";
                
                string wapeningTekstBoven = $"{wapOnder}";
                bool tweeVerschillendeLijnen = false;

                string wapeningTekstVerdeelBoven = "";

                // Bereken afstand tussen boven- en onderwapening lijnen
                double dekkingOnder = bordes.PlaatDekking.Onder.DekkingToe;
                double dekkingBoven = bordes.PlaatDekking.Boven.DekkingToe;
                double afstandTussenLijnen = bordes.Dikte - (dekkingBoven + dekkingOnder);

                
                if (bordes.PlaatWapening?.Boven?.BasisWapening != null)
                {
                    var wapBoven = bordes.PlaatWapening.Boven.BasisWapening;
                    laagNummerBoven = wapBoven.LaagNummer ?? 1;
                    
                    if (wapBoven.ToString() != wapOnder.ToString())
                    {
                        // Verschillende wapening → twee lijnen
                        tweeVerschillendeLijnen = true;
                        wapeningTekstBoven = $"{wapBoven}";
                    }
                    else
                    {
                        // Zelfde wapening → één lijn
                        //wapeningTekstOnder = $"{wapBoven}";
                    }

                    var wapBovenVerdeel = bordes.PlaatWapening.Boven.VerdeelWapening;
                    wapeningTekstVerdeelBoven = wapBovenVerdeel?.ToString() ?? "?";
                   
                }

                // Hoofdwapening lijn(en) (horizontaal)
                if (tweeVerschillendeLijnen)
                {
                    // ✅ TWEE LIJNEN: boven- en onderwapening verschillend

                    // Onderwapening lijn (onderste)
                    double yOnderLijn = yBasis + afstandTussenLijnen / 2.0;

                    var onderLijn = new SvgLine
                    {
                        X1 = x1,
                        Y1 = yOnderLijn,
                        X2 = x2,
                        Y2 = yOnderLijn,
                        Stroke = kleurOnder,
                        StrokeWidth = 2,
                    };
                    wapeningElements.Add(onderLijn);

                    // Bovenwapening lijn (bovenste, parallel)
                    double yBovenLijn = yBasis - afstandTussenLijnen /2.0;
                    var bovenLijn = new SvgLine
                    {
                        X1 = x1,
                        Y1 = yBovenLijn,
                        X2 = x2,
                        Y2 = yBovenLijn,
                        Stroke = kleurBoven,
                        StrokeWidth = 2,
                    };
                    wapeningElements.Add(bovenLijn);

                    // Tekst onderwapening
                    var wapTekstOnder = new SvgText(
                        $"o:{wapeningTekstOnder}",
                        x: x2,
                        y: yOnderLijn,
                        scale: scale
                    )
                    {
                        Anchor = "left",
                        DominantBaseLine = "middle",
                        Fill = kleurOnder,
                        DY = 0,
                        DX = 20
                    };
                    wapeningElements.Add(wapTekstOnder);

                    // Tekst bovenwapening
                    var wapTekstBoven = new SvgText(
                        $"b:{wapeningTekstBoven}",
                        x: x2,
                        y: yBovenLijn,
                        scale: scale
                    )
                    {
                        Anchor = "left",
                        DominantBaseLine = "middle",
                        Fill = kleurBoven,
                        DY = 0,
                        DX = 20
                    };
                    wapeningElements.Add(wapTekstBoven);

                    // Driehoeken onderwapening
                    double xDriehoekOnder = xCenter + 150;
                    var onderDriehoeken = GenerateWapeningDriehoeken(
                        x: xDriehoekOnder,
                        y: yOnderLijn,
                        aantalOnder: laagNummerOnder,
                        aantalBoven: 0,
                        rotatie: 0,
                        kleurOnder: kleurOnder
                    );
                    wapeningElements.AddRange(onderDriehoeken);

                    // Driehoeken bovenwapening
                    var bovenDriehoeken = GenerateWapeningDriehoeken(
                        x: xDriehoekOnder,
                        y: yBovenLijn,
                        aantalOnder: 0,
                        aantalBoven: laagNummerBoven,
                        rotatie: 0,
                        kleurBoven: kleurBoven
                    );
                    wapeningElements.AddRange(bovenDriehoeken);
                }
                else
                {
                    // ✅ ÉÉN LIJN: boven- en onderwapening identiek
                    var hoofdwapeningLijn = new SvgLine
                    {
                        X1 = x1,
                        Y1 = yBasis,
                        X2 = x2,
                        Y2 = yBasis,
                        Stroke = "black",
                        StrokeWidth = 2,
                    };
                    wapeningElements.Add(hoofdwapeningLijn);

                    // Tekst
                    var wapTekst = new SvgText(
                        wapeningTekstOnder,
                        x: x2,
                        y: yBasis,
                        scale: scale
                    )
                    {
                        Anchor = "left",
                        DominantBaseLine = "middle",
                        Fill = "black",
                        DY = 0,
                        DX = 20
                    };
                    wapeningElements.Add(wapTekst);

                    // Driehoeken
                    double xDriehoekOnder = xCenter + 100;
                    var hoofdwapeningDriehoeken = GenerateWapeningDriehoeken(
                        x: xDriehoekOnder,
                        y: yBasis,
                        aantalOnder: laagNummerOnder,
                        aantalBoven: laagNummerBoven,
                        rotatie: 0
                    );
                    wapeningElements.AddRange(hoofdwapeningDriehoeken);
                }

                // Verdeelwapening lijn(en) (verticaal)
                bool tweeVerschillendeVerdeelLijnen = tweeVerschillendeLijnen; // als onder twee verschillende dan hier ook!
                string verdeelTekstOnder = $"{wapOnderVerdeel}";
                string verdeelTekstBoven = wapeningTekstVerdeelBoven;
                int verdeelLaagOnder = wapOnderVerdeel?.LaagNummer ?? 1;
                int verdeelLaagBoven = 0;

                if (bordes.PlaatWapening?.Boven?.VerdeelWapening != null)
                {
                    var wapBovenVerdeel = bordes.PlaatWapening.Boven.VerdeelWapening;
                    verdeelLaagBoven = wapBovenVerdeel?.LaagNummer ?? 0;
                    
                    if (wapBovenVerdeel?.ToString() != wapOnderVerdeel?.ToString())
                    {
                        // Verschillende verdeelwapening → twee lijnen
                        tweeVerschillendeVerdeelLijnen = true;
                        verdeelTekstBoven = $"{wapBovenVerdeel}";
                    }
                    else
                    {
                        // Zelfde verdeelwapening → één lijn
                        verdeelTekstOnder = $"{wapBovenVerdeel}";
                    }
                }

                if (tweeVerschillendeVerdeelLijnen)
                {
                    // ✅ TWEE VERTICALE LIJNEN: onder- en bovenverdeelwapening verschillend

                    // Onderverdeelwapening lijn (links)
                    double xOnderVerdeelLijn = xCenter + afstandTussenLijnen / 2.0;
                    var onderVerdeelLijn = new SvgLine
                    {
                        X1 = xOnderVerdeelLijn,
                        Y1 = y1,
                        X2 = xOnderVerdeelLijn,
                        Y2 = y2,
                        Stroke = kleurOnder,
                        StrokeWidth = 2,
                    };
                    wapeningElements.Add(onderVerdeelLijn);

                    // Bovenverdeelwapening lijn (rechts, parallel)
                    double xBovenVerdeelLijn = xCenter - afstandTussenLijnen / 2.0;
                    var bovenVerdeelLijn = new SvgLine
                    {
                        X1 = xBovenVerdeelLijn,
                        Y1 = y1,
                        X2 = xBovenVerdeelLijn,
                        Y2 = y2,
                        Stroke = kleurBoven,
                        StrokeWidth = 2,
                    };
                    wapeningElements.Add(bovenVerdeelLijn);

                    // Tekst onderverdeelwapening
                    var verdeelTekstOnderSvg = new SvgText(
                        $"o:{verdeelTekstOnder}",
                        x: xOnderVerdeelLijn,
                        y: y2,
                        angle: -90,
                        scale: scale
                    )
                    {
                        Anchor = "left",
                        DominantBaseLine = "middle",
                        Fill = kleurOnder,
                        DX = 20
                    };
                    wapeningElements.Add(verdeelTekstOnderSvg);

                    // Tekst bovenverdeelwapening
                    var verdeelTekstBovenSvg = new SvgText(
                        $"b:{verdeelTekstBoven}",
                        x: xBovenVerdeelLijn,
                        y: y2,
                        angle: -90,
                        scale: scale
                    )
                    {
                        Anchor = "left",
                        DominantBaseLine = "middle",
                        Fill = kleurBoven,
                        DX = 20
                    };
                    wapeningElements.Add(verdeelTekstBovenSvg);

                    // Driehoeken onderverdeelwapening (verticaal, links)
                    double yDriehoekVerdeel = yBasis - 200;
                    var onderVerdeelDriehoeken = GenerateWapeningDriehoeken(
                        x: xOnderVerdeelLijn,
                        y: yDriehoekVerdeel,
                        aantalOnder: verdeelLaagOnder,
                        aantalBoven: 0,
                        rotatie: -90,
                        kleurOnder: kleurOnder
                    );
                    wapeningElements.AddRange(onderVerdeelDriehoeken);

                    // Driehoeken bovenverdeelwapening (verticaal, rechts)
                    var bovenVerdeelDriehoeken = GenerateWapeningDriehoeken(
                        x: xBovenVerdeelLijn,
                        y: yDriehoekVerdeel,
                        aantalOnder: 0,
                        aantalBoven: verdeelLaagBoven,
                        rotatie: -90,
                        kleurBoven: kleurBoven
                    );
                    wapeningElements.AddRange(bovenVerdeelDriehoeken);
                }
                else
                {
                    // ✅ ÉÉN VERTICALE LIJN: onder- en bovenverdeelwapening identiek
                    var verdeeelwapening = new SvgLine
                    {
                        X1 = xCenter,
                        Y1 = y1,
                        X2 = xCenter,
                        Y2 = y2,
                        Stroke = "black",
                        StrokeWidth = 2,
                    };
                    wapeningElements.Add(verdeeelwapening);

                    // Tekst
                    var verdeelTekst = new SvgText(
                        verdeelTekstOnder,
                        x: xCenter,
                        y: y2,
                        angle: -90
                    )
                    {
                        Anchor = "left",
                        DominantBaseLine = "middle",
                        Fill = "black",
                        DX = 20
                    };
                    wapeningElements.Add(verdeelTekst);

                    // Driehoeken
                    double yDriehoekVerdeel = yBasis - 100;
                    var verdeelwapeningDriehoeken = GenerateWapeningDriehoeken(
                        x: xCenter,
                        y: yDriehoekVerdeel,
                        aantalOnder: verdeelLaagOnder,
                        aantalBoven: verdeelLaagBoven,
                        rotatie: 90
                    );
                    wapeningElements.AddRange(verdeelwapeningDriehoeken);
                }





                // ✅ NIEUW: Verdeellijn (verticale maatlijn) voor wapeningspositie
                if (toonVerdeelLijnen)
                {
                    var verdeelLijn = new SvgDimLine
                    {
                        Mode = DimLineMode.Vertical,
                        X1 = xVerdeel,
                        Y1 = 0, 
                        X2 = xVerdeel,
                        Y2 = -bordes.Breedte,
                        Offset = 0,
                        OffsetLines = 2,
                        Text = $"basis",
                        StrokeWidth = 0.5,
                        StrokeColor = "red"
                    };
                    verdeelLijnen.Add(verdeelLijn);
                }
            }

            // ===============================================
            // 2️⃣ VERSTERKTE STROOK WAPENING 
            // ===============================================
            if (bordes.Trap1.AansluitendElement != null || bordes.Trap1.GebruikEigenOpgave ||
                bordes.Trap2.AansluitendElement != null || bordes.Trap2.GebruikEigenOpgave)
            {
                var wapeningOnder = bordes.BijlegWapeningOnder;
                var wapeningBoven = bordes.BijlegWapeningBoven;
                
                double yVSstart = -bordes.Trap1.Breedte;
                //yVSstart = 500;
                double yVSend = yVSstart - bordes.BreedteVersterkteStrook;
                double yVS = yVSstart - bordes.BreedteVersterkteStrook / 2.0;
                double dekking = bordes.PlaatDekking.Onder.DekkingToe;

                string wapeningTekstOnder = wapeningOnder?.ToString() ?? "NULL";
                string wapeningTekstBoven = "";
                int laagNummerOnder = wapeningOnder?.LaagNummer ?? 2;
                int laagNummerBoven = 0;
                bool tweeVerschillendeBijlegLijnen = false;

                // Bereken afstand tussen boven- en onderwapening
                double dekkingOnder = bordes.PlaatDekking.Onder.DekkingToe;
                double dekkingBoven = bordes.PlaatDekking.Boven.DekkingToe;
                double afstandTussenLijnen = bordes.Dikte - (dekkingBoven + dekkingOnder);

                // Check of bovenwapening bestaat en verschillend is
                if (wapeningBoven != null)
                {
                    laagNummerBoven = wapeningBoven.LaagNummer ?? 1;
                    
                    if (wapeningBoven.ToString() != wapeningOnder?.ToString())
                    {
                        // Verschillende wapening → twee lijnen
                        tweeVerschillendeBijlegLijnen = true;
                        wapeningTekstBoven = wapeningBoven.ToString();
                        wapeningTekstOnder = wapeningOnder?.ToString();
                    }
                    else
                    {
                        // Zelfde wapening → één lijn met beide driehoeken
                        wapeningTekstOnder = wapeningBoven.ToString();
                        laagNummerBoven = wapeningBoven.LaagNummer ?? 1;
                    }
                }

                if (tweeVerschillendeBijlegLijnen)
                {
                    // ✅ TWEE LIJNEN: boven- en onderbijlegwapening verschillend
                    
                    // Onderbijlegwapening lijn (onderste)
                    double yOnderLijn = yVS + afstandTussenLijnen / 2.0;
                    var onderLijn = new SvgLine
                    {
                        X1 = dekking,
                        Y1 = yOnderLijn,
                        X2 = bordes.Lengte - dekking,
                        Y2 = yOnderLijn,
                        Stroke = kleurOnder,
                        StrokeWidth = 2,
                    };
                    wapeningElements.Add(onderLijn);

                    // Bovenbijlegwapening polyline (bovenste, parallel met trap-vormige uiteinden)
                    double yBovenLijn = yVS - afstandTussenLijnen / 2.0;
                    double yTrapVorm = yOnderLijn - 30; // 30mm boven de onderlijn
                    double xTrapInsprong = 500; // 500mm horizontaal insprong
                    
                    List<Punt> bovenWapeningPunten = 
                    [
                        // Start links
                        new(dekking + xTrapInsprong, yTrapVorm),
                        new(dekking, yTrapVorm),
                        new(dekking, yBovenLijn),
                        new(bordes.Lengte - dekking, yBovenLijn),
                        new(bordes.Lengte - dekking, yTrapVorm),
                        new(bordes.Lengte - dekking -500, yTrapVorm)
                    ];
                    
                    var bovenPath = SvgGenerator.MakePath(bovenWapeningPunten, scaleY: 1, close: false);
                    bovenPath.Fill = "none";
                    bovenPath.Stroke = kleurBoven;
                    bovenPath.StrokeWidth = 2;
                    wapeningElements.Add(bovenPath);

                    // Tekst onderbijlegwapening
                    var wapTekstOnder = new SvgText(
                        $"o:{wapeningTekstOnder}",
                        x: bordes.Lengte / 2,
                        y: yOnderLijn,
                        scale: scale
                    )
                    {
                        Anchor = "middle",
                        DominantBaseLine = "base",
                        Fill = kleurOnder,
                        DY = -10,
                    };
                    wapeningElements.Add(wapTekstOnder);

                    // Tekst bovenbijlegwapening
                    var wapTekstBoven = new SvgText(
                        $"b:{wapeningTekstBoven}",
                        x: bordes.Lengte / 2,
                        y: yBovenLijn,
                        scale: scale
                    )
                    {
                        Anchor = "middle",
                        DominantBaseLine = "base",
                        Fill = kleurBoven,
                        DY = -10,
                    };
                    wapeningElements.Add(wapTekstBoven);

                    // Driehoeken onderbijlegwapening
                    double xDriehoekBijleg = bordes.Lengte / 3;
                    var onderBijlegDriehoeken = GenerateWapeningDriehoeken(
                        x: xDriehoekBijleg,
                        y: yOnderLijn,
                        aantalOnder: laagNummerOnder,
                        aantalBoven: 0,
                        kleurOnder: kleurOnder
                    );
                    wapeningElements.AddRange(onderBijlegDriehoeken);

                    // Driehoeken bovenbijlegwapening
                    var bovenBijlegDriehoeken = GenerateWapeningDriehoeken(
                        x: xDriehoekBijleg,
                        y: yBovenLijn,
                        aantalOnder: 0,
                        aantalBoven: laagNummerBoven,
                        kleurBoven: kleurBoven
                    );
                    wapeningElements.AddRange(bovenBijlegDriehoeken);
                }
                else
                {
                    // ✅ ÉÉN LIJN: zelfde wapening of alleen onderwapening
                    var vsWapeningLijn = new SvgLine
                    {
                        X1 = dekking,
                        Y1 = yVS,
                        X2 = bordes.Lengte - dekking,
                        Y2 = yVS,
                        Stroke = "black",
                        StrokeWidth = 2,
                    };
                    wapeningElements.Add(vsWapeningLijn);

                    // Tekst
                    var wapTekst = new SvgText(
                        wapeningTekstOnder,
                        x: bordes.Lengte / 2,
                        y: yVS,
                        scale: scale
                    )
                    {
                        Anchor = "middle",
                        DominantBaseLine = "base",
                        Fill = "darkred",
                        DY = -10,
                    };
                    wapeningElements.Add(wapTekst);

                    // Driehoeken (met beide lagen als ze bestaan)
                    double xDriehoekBijleg = bordes.Lengte / 3;
                    var bijlegDriehoeken = GenerateWapeningDriehoeken(
                        x: xDriehoekBijleg,
                        y: yVS,
                        aantalOnder: laagNummerOnder,
                        aantalBoven: laagNummerBoven
                    );
                    wapeningElements.AddRange(bijlegDriehoeken);
                }



                // ✅ NIEUW: Verdeellijn voor versterkte strook wapening
                if (toonVerdeelLijnen)
                {
                    var verdeelLijn = new SvgDimLine
                    {
                        Mode = DimLineMode.Vertical,
                        X1 = xVerdeel,
                        Y1 = yVSstart,
                        X2 = xVerdeel,
                        Y2 = yVSend,
                        Offset = 0,
                        OffsetLines = 1,
                        Text = $"v.s.",
                        StrokeWidth = 0.5,
                        StrokeColor = "darkred",
                        
                    };
                    verdeelLijnen.Add(verdeelLijn);
                }
            }

            // ===============================================
            // 3️⃣ BIJLEGSTAVEN (HAARSPELDEN)
            // ===============================================
            if (bordes.Trap1?.AansluitendElement != null && bordes.PlaatWapening?.Onder?.BasisWapening != null)
            {
                var c = bordes.Trap1;
                var wap = bordes.Tand.WapeningAlgemeen;

                if (wap != null)
                {
                    // ✅ NIEUW: Verticale wapeningslijn in midden van aansluiting met driehoeken
                    double xMiddenAansluiting = c.Randafstand + (c.Lengte / 2.0);
                    double yWapeningStart = -30;
                    double yWapeningEnd = -530;
                    
                    // Verticale wapeningslijn
                    var wapeningLijn = new SvgLine
                    {
                        X1 = xMiddenAansluiting,
                        Y1 = yWapeningStart,
                        X2 = xMiddenAansluiting,
                        Y2 = yWapeningEnd,
                        Stroke = "black",
                        StrokeWidth = 2
                    };
                    wapeningElements.Add(wapeningLijn);
                    
                    // Driehoeken voor de verticale wapening (1 boven, 1 onder)
                    double yMiddenWapening = (yWapeningStart + yWapeningEnd) / 2.0;
                    var wapeningDriehoeken = GenerateWapeningDriehoeken(
                        x: xMiddenAansluiting,
                        y: yMiddenWapening,
                        aantalOnder: 1,  // 1 driehoek onder (▲)
                        aantalBoven: 1,  // 1 driehoek boven (▼)
                        grootte: 40,
                        rotatie: -90,     // verticaal
                        kleurOnder: "black",
                        kleurBoven: "black"
                    );
                    wapeningElements.AddRange(wapeningDriehoeken);

                    // Wapeningsnotatie tekst (rechts uitgelijnd, links van de verticale lijn)
                    var tandWapeningTekst = new SvgText(
                        wap.ToString(),
                        x: xMiddenAansluiting,
                        y: yWapeningStart,
                        angle: -90,
                        scale: scale
                    )
                    {
                        Anchor = "end",
                        DominantBaseLine = "middle",
                        Fill = "black",
                        DX = -20
                    };
                    wapeningElements.Add(tandWapeningTekst);



                   



                }
            }

            // Herhaal voor Trap2
            if (bordes.Trap2?.AansluitendElement != null && bordes.PlaatWapening?.Onder?.BasisWapening != null)
            {
                var c = bordes.Trap2;
                var wap = bordes.Tand.WapeningAlgemeen;
                
                int aantalBijlegStaven = (int)wap.AantalBijlegStaven;
                double bijlegDiameter = wap.DiameterBijlegStaven;
                
                if (wap != null)
                {
                    // ✅ NIEUW: Verticale wapeningslijn in midden van aansluiting met driehoeken
                    double xMiddenAansluiting = bordes.Lengte - c.Randafstand - (c.Lengte / 2.0);
                    double yWapeningStart = -30;
                    double yWapeningEnd = -530;
                    
                    // Verticale wapeningslijn
                    var wapeningLijn = new SvgLine
                    {
                        X1 = xMiddenAansluiting,
                        Y1 = yWapeningStart,
                        X2 = xMiddenAansluiting,
                        Y2 = yWapeningEnd,
                        Stroke = "black",
                        StrokeWidth = 2
                    };
                    wapeningElements.Add(wapeningLijn);
                    
                    // Driehoeken voor de verticale wapening (1 boven, 1 onder)
                    double yMiddenWapening = (yWapeningStart + yWapeningEnd) / 2.0;
                    var wapeningDriehoeken = GenerateWapeningDriehoeken(
                        x: xMiddenAansluiting,
                        y: yMiddenWapening,
                        aantalOnder: 1,  // 1 driehoek onder (▲)
                        aantalBoven: 1,  // 1 driehoek boven (▼)
                        grootte: 40,
                        rotatie: -90,     // verticaal
                        kleurOnder: "black",
                        kleurBoven: "black"
                    );
                    wapeningElements.AddRange(wapeningDriehoeken);

                    // Wapeningsnotatie tekst (links uitgelijnd, rechts van de verticale lijn)
                    var tandWapeningTekst = new SvgText(
                        wap.ToString(),
                        x: xMiddenAansluiting,
                        y: yWapeningStart,
                        angle: -90,
                        scale: scale
                    )
                    {
                        Anchor = "end",
                        DominantBaseLine = "middle",
                        Fill = "black",
                        DX = -20
                    };
                    wapeningElements.Add(tandWapeningTekst);

                    double bijlegSpacing = c.Lengte / (aantalBijlegStaven + 1);
                    
                    for (int i = 1; i <= aantalBijlegStaven; i++)
                    {
                        double xBijleg = (bordes.Lengte - c.Randafstand - c.Lengte) + i * bijlegSpacing;
                        double yStart = -c.Breedte / 2.0;
                        double yEnd = -(c.Breedte + bordes.BreedteVersterkteStrook / 2.0);
                        
                        var bijlegLijn = new SvgLine
                        {
                            X1 = xBijleg,
                            Y1 = yStart,
                            X2 = xBijleg,
                            Y2 = yEnd,
                            Stroke = "orange",
                            StrokeWidth = bijlegDiameter / 2.0,
                            StrokeDashArray = "5,3"
                        };
                        wapeningElements.Add(bijlegLijn);

                        // ✅ NIEUW: Horizontale verdeellijn voor bijlegstaven (alleen voor eerste staaf)
                        if (toonVerdeelLijnen && i == 1)
                        {
                            double yVerdeelBijleg = (yStart + yEnd) / 2.0;
                            
                            var bijlegVerdeelLijn = new SvgDimLine
                            {
                                Mode = DimLineMode.Horizontal,
                                X1 = bordes.Lengte - c.Randafstand - c.Lengte,
                                Y1 = yVerdeelBijleg,
                                X2 = bordes.Lengte - c.Randafstand,
                                Y2 = yVerdeelBijleg,
                                Offset = 0,
                                OffsetLines = 1,
                                Text = $"{aantalBijlegStaven}Ø{bijlegDiameter:0.#}",
                                StrokeWidth = 0.5,
                                StrokeColor = "orange"
                            };
                            verdeelLijnen.Add(bijlegVerdeelLijn);
                        }
                    }
                }
            }

            return (wapeningElements, verdeelLijnen);
        }

        /// <summary>
        /// Backwards compatible wrapper - gebruikt de nieuwe methode maar retourneert alleen wapening elementen
        /// </summary>
        public static List<BaseSvg> GenerateBordesWapening(BordesEntity bordes)
        {
            var (wapeningElements, _) = GenerateBordesWapeningMetVerdeelLijnen(bordes, toonVerdeelLijnen: false);
            return wapeningElements;
        }


        public static List<SvgDimLine> GenerateKolomDimLines(KolomEntity kolom)
        {
            List<SvgDimLine> dimLines = [];

            dimLines.Add(new SvgDimLine
            {
                Mode = DimLineMode.Vertical,
                X1 = 0,
                Y1 = 0,
                X2 = 0,
                Y2 = -kolom.Hoogte,
                Offset = 0,
                OffsetLines = 3,
                Text = $"{kolom.Hoogte:0}",
                StrokeColor = "var(--neutral-foreground-rest, black)",
            });

            return dimLines;
        }


        public static List<SvgDimLine> GenerateBordesDimLines(BordesEntity bordes, double vsY)
        {
            var t1R = 0.0;
            var t2L = 0.0;
            

            List<SvgDimLine> returnList = [];
            returnList.Add(new SvgDimLine
            {
                Mode = DimLineMode.Vertical,
                X1 = 0,
                Y1 = 0,
                X2 = bordes.Lengte,
                Y2 = -bordes.Breedte,
                Offset = 0,
                OffsetLines = 3,
                //Value = trap.HoogteTotaal,
                Text = $"{bordes.Breedte:0}",
                StrokeWidth = 1,
                StrokeColor = "var(--neutral-foreground-rest, black)"
            });

            if (bordes.Trap1?.AansluitendElement != null || bordes.Trap1!.GebruikEigenOpgave)
            {
                var c = bordes.Trap1;
                t1R = c.Randafstand + c.Lengte;
                returnList.Add(new SvgDimLine
                {
                    Mode = DimLineMode.Horizontal,
                    X1 = c.Randafstand,
                    Y1 = 0,
                    X2 = c.Randafstand + c.Lengte,
                    Y2 = 0,
                    Offset = 0,
                    OffsetLines = -3,
                    //Value = trap.HoogteTotaal,
                    Text = $"{c.Lengte:0}",
                    StrokeWidth = 1,
                    StrokeColor = "var(--neutral-foreground-rest, black)"
                });

                if (c.Randafstand > 0)
                {
                    returnList.Add(new SvgDimLine
                    {
                        Mode = DimLineMode.Horizontal,
                        X1 = 0,
                        X2 = c.Randafstand,
                        Y1 = 0,
                        Y2 = 0,
                        OffsetLines = -3,
                        Text = $"{c.Randafstand:0}"
                    });
                }
               



                returnList.Add(new SvgDimLine
                {
                    Mode = DimLineMode.Vertical,
                    X1 = 0,
                    Y1 = -c.Breedte,
                    X2 = 0,
                    Y2 = -c.Breedte - bordes.BreedteVersterkteStrook,
                    Offset = 0,
                    OffsetLines = 2,
                    //Value = trap.HoogteTotaal,
                    Text = $"{bordes.BreedteVersterkteStrook:0}",
                    StrokeWidth = 1,
                    StrokeColor = "var(--neutral-foreground-rest, black)"
                });

                returnList.Add(new SvgDimLine
                {
                    Mode = DimLineMode.Vertical,
                    X1 = 0,
                    Y1 = 0,
                    X2 = 0,
                    Y2 = -c.Breedte,
                    OffsetLines = 2,
                    Text = $"{c.Breedte:0}"


                });



                var armY1 = c.Breedte / 2.0;
                returnList.Add(new SvgDimLine
                {
                    Mode = DimLineMode.Vertical,
                    X1 = c.Randafstand + c.Lengte / 2.0,
                    Y1 = -armY1,
                    X2 = c.Randafstand + c.Lengte / 2.0,
                    Y2 = vsY,
                    Offset = 0,
                    OffsetLines = 0,
                    StrokeWidth = 1,
                    StrokeColor = "var(--neutral-foreground-rest, black)"
                });



            }


            if (bordes.Trap2?.AansluitendElement != null || bordes.Trap2!.GebruikEigenOpgave)
            {
                var c = bordes.Trap2;
                t2L = bordes.Lengte - c.Randafstand - c.Lengte;
                returnList.Add(new SvgDimLine
                {
                    Mode = DimLineMode.Horizontal,
                    X1 = bordes.Lengte - c.Randafstand,
                    Y1 = 0,
                    X2 = bordes.Lengte - c.Randafstand - c.Lengte,
                    Y2 = 0,
                    Offset = 0,
                    OffsetLines = -3,
                    //Value = trap.HoogteTotaal,
                    Text = $"{c.Lengte:0}",
                    StrokeWidth = 1,
                    StrokeColor = "var(--neutral-foreground-rest, black)"
                });


                if (c.Randafstand > 0)
                {
                    returnList.Add(new SvgDimLine
                    {
                        Mode = DimLineMode.Horizontal,
                        X1 = bordes.Lengte - c.Randafstand,
                        X2 = bordes.Lengte,
                        OffsetLines = -3,
                        Text = $"{c.Randafstand:0}"
                    });
                }

                var armY1 = c.Breedte / 2.0;
                returnList.Add(new SvgDimLine
                {
                    Mode = DimLineMode.Vertical,
                    X1 = bordes.Lengte - c.Randafstand - c.Lengte / 2.0,
                    Y1 = -armY1,
                    X2 = bordes.Lengte - c.Randafstand - c.Lengte / 2.0,
                    Y2 = vsY,
                    Offset = 0,
                    OffsetLines = 0,
                    StrokeWidth = 1,
                    StrokeColor = "var(--neutral-foreground-rest, black)"
                });

            }




            returnList.Add(new SvgDimLine
            {
                Mode = DimLineMode.Horizontal,
                X1 = 0,
                Y1 = 0,
                X2 = bordes.Lengte,
                Y2 = -bordes.Breedte,
                Offset = 0,
                OffsetLines = 3,
                //Value = trap.HoogteTotaal,
                Text = $"{bordes.Lengte:0}",
                StrokeWidth = 1,
                StrokeColor = "var(--neutral-foreground-rest, black)"
            });


            returnList.Add(new SvgDimLine
            {
                Mode = DimLineMode.Horizontal,
                X1 = 0,
                Y1 = 0,
                X2 = bordes.Lengte,
                Y2 = -bordes.Breedte,
                Offset = 0,
                OffsetLines = 3,
                //Value = trap.HoogteTotaal,
                Text = $"{bordes.Lengte:0}",
                StrokeWidth = 1,
                StrokeColor = "var(--neutral-foreground-rest, black)"
            });

            // schalmgat
            if (t1R < t2L)
            {
                returnList.Add(new SvgDimLine { X1 = t1R, X2 = t2L, OffsetLines = 3, Text = $"{(t2L - t1R):0}"});
            }


            return returnList;
        }


        public static List<SvgDimLine> GenerateSimpleSchemaDimLines(SteekTrapEntity trap)
        {
            List<SvgDimLine> returnList = [];
            // totaal (ver.)
            returnList.Add(new SvgDimLine
            {
                Mode = DimLineMode.Vertical,
                X1 = 0,
                Y1 = 0,
                X2 = 0,
                Y2 = -trap.HoogteTotaal,
                Offset = trap.WelMaat,
                OffsetLines = 3,
                //Value = trap.HoogteTotaal,
                StrokeWidth = 1,
                StrokeColor = "var(--neutral-foreground-rest, black)"
            });
            // totaal (hor.)
            returnList.Add(new SvgDimLine
            {
                Mode = DimLineMode.Horizontal,
                X1 = 0,
                Y1 = 0,
                X2 = trap.LtProjZ,
                Y2 = -trap.HoogteTotaal,
                Offset = -trap.HoogteTotaal,
                OffsetLines = -2,
                //Value = trap.AantredeMaat,
                //
                StrokeWidth = 1,
                StrokeColor = "var(--neutral-foreground-rest, black)"
            });


            // totaal (schuin)
            if (trap.HoogteTotaal > trap.LtProjZ * 0.05)
            {
                returnList.Add(new SvgDimLine
                {
                    Mode = DimLineMode.Aligned,
                    X1 = 0,
                    Y1 = 0,
                    X2 = trap.LtProjZ,
                    Y2 = -trap.HoogteTotaal,
                    Offset = 0,
                    OffsetLines = 0,
                    //Value = trap.AantredeMaat,
                    //
                    StrokeWidth = 1,
                    StrokeColor = "var(--neutral-foreground-rest, black)"
                });
            }

           


            return returnList;


        }

        public static List<SvgDimLine> GenerateTrapDimLines(SteekTrapEntity trap, bool isSchema = true)
        {
            List<SvgDimLine> returnList = [];

            // werkelijke rechtse eindpositie van het getekende trappad
            // (verschilt van LengteTotaal als de bovenste aantrede afwijkt van de standaard aantrede)
            double xEindpunt = (!trap.GebruikEigenLengte && trap.TandOpleggingBovenzijde != null)
                ? (trap.OptredeAantal1 - 1) * trap.AantredeMaat
                    + trap.TandOpleggingBovenzijde.HalsDikte
                    + trap.TandOpleggingBovenzijde.TandLengte
                : trap.LtProjZ;

            // totaal (ver.)
            returnList.Add(new SvgDimLine
            {
                Mode = DimLineMode.Vertical,
                X1 = 0,
                Y1 = 0,
                X2 = trap.LtProjZ,
                Y2 = -trap.HoogteTotaal,
                Offset = trap.WelMaat,
                OffsetLines = 3,
                //Value = trap.HoogteTotaal,
                StrokeWidth = 1,
                StrokeColor = "var(--neutral-foreground-rest, black)"
            });





            // totaal (hor.)
            returnList.Add(new SvgDimLine
            {
                Mode = DimLineMode.Horizontal,
                X1 = trap.GebruikEigenLengte? 0 : -trap.WelMaat,
                Y1 = trap.GebruikEigenLengte? 0 : -trap.OptredeMaat,
                X2 = xEindpunt,
                Y2 = -trap.HoogteTotaal,
                Offset = 0,
                OffsetLines = 8,
                //Value = trap.AantredeMaat,
                //
                StrokeWidth = 1,
                StrokeColor = "var(--neutral-foreground-rest, black)"
            });


            // diagonaal
            returnList.Add(new SvgDimLine
            {
                Mode = DimLineMode.Aligned,
                X1 = 0,
                Y1 = 0,
                X2 = trap.LtProjZ,
                Y2 = -trap.HoogteTotaal,
                Offset = trap.SchilDikte,
                OffsetLines = 1,
                Text = $"{trap.LengteSchuin:0}",

            });




            if (!isSchema)
            {

                // schil
                returnList.Add(new SvgDimLine
                {
                    Mode = DimLineMode.Aligned,
                    X1 = trap.AantredeMaat,
                    Y1 = -trap.OptredeMaat,
                    X2 = trap.AantredeMaat + trap.SchilDikte * (trap.OptredeMaat / trap.SchuineMaat),
                    Y2 = -trap.OptredeMaat + trap.SchilDikte * (trap.AantredeMaat / trap.SchuineMaat),
                    Offset = 0,
                    OffsetLines = -3,
                    Text = $"{trap.SchilDikte:0}",

                });


                if (trap.TandOpleggingBovenzijde != null)
                {
                    returnList.Add(new SvgDimLine
                    {
                        Mode = DimLineMode.Horizontal,
                        X1 = xEindpunt - trap.TandOpleggingBovenzijde.TandLengte,
                        Y1 = -trap.HoogteTotaal + trap.TandOpleggingBovenzijde.TandHoogte,
                        X2 = xEindpunt,
                        Y2 = -trap.HoogteTotaal,
                        Offset = -trap.TandOpleggingBovenzijde.TandHoogte,
                        OffsetLines = -2,
                    });

                    returnList.Add(new SvgDimLine
                    {
                        Mode = DimLineMode.Horizontal,
                        X1 = xEindpunt - trap.TandOpleggingBovenzijde.TandLengte - trap.TandOpleggingBovenzijde.HalsDikte,
                        Y1 = -trap.HoogteTotaal + trap.TandOpleggingBovenzijde.TandHoogte,
                        X2 = xEindpunt - trap.TandOpleggingBovenzijde.TandLengte,
                        Y2 = -trap.HoogteTotaal,
                        Offset = -trap.TandOpleggingBovenzijde.TandHoogte,
                        OffsetLines = -2,
                    });

                    returnList.Add(new SvgDimLine
                    {
                        Mode = DimLineMode.Vertical,
                        X1 = xEindpunt - trap.TandOpleggingBovenzijde.TandLengte,
                        Y1 = -trap.HoogteTotaal + trap.TandOpleggingBovenzijde.TandHoogte,
                        X2 = xEindpunt,
                        Y2 = -trap.HoogteTotaal,
                        Offset = -trap.TandOpleggingBovenzijde.TandLengte,
                        OffsetLines = -2,
                    });

                }

            }


            return returnList;
        }


        public static SvgPath GenerateTrapboom(SteekTrapEntity trap)
        {
            var pathData = GenerateTrapboomPath(trap);
            return new SvgPath
            {
                D = pathData,
                Stroke = "var(--neutral-foreground-rest, black)",
                StrokeWidth = 1,
                Fill = "none",
                Opacity = 1.0,
                StrokeDashArray = "",
                StrokeLineJoin = "miter",
                StrokeLineCap = "butt",
                FillRule = "nonzero"
            };
        }

        public static SvgPath GenerateTrapSvgPath(SteekTrapEntity trap, BoundingBox bb)
        {
            var pathData = GenerateTrapPathOptimized(trap, bb);
            return new SvgPath
            {
                D = pathData,
                Stroke = "var(--neutral-foreground-rest, black)",
                StrokeWidth = 2,
                Fill = "lightgray",
                Opacity = 1.0,
                StrokeDashArray = "",
                StrokeLineJoin = "miter",
                StrokeLineCap = "butt",
                FillRule = "nonzero"
            };
        }

       

        /// <summary>
        /// Hulplijn langs de voorkant van de treden (looplijn):
        /// van (-WelMaat, -OptredeMaat) tot de voorkant van de laatste trede.
        /// </summary>
        public static SvgLine? GenerateLooplijn(SteekTrapEntity trap)
        {
            if (trap.GebruikEigenLengte) return null;

            return new SvgLine
            {
                X1 = -trap.WelMaat,
                Y1 = -trap.OptredeMaat,
                X2 = (trap.OptredeAantal1 - 1) * trap.AantredeMaat - trap.WelMaat,
                Y2 = -trap.OptredeAantal1 * trap.OptredeMaat,
                Stroke = "gray",
                StrokeWidth = 0.5,
                StrokeDashArray = "8,4",
            };
        }

        public static string GenerateTrapboomPath(SteekTrapEntity trap)
        {
            var sb = new StringBuilder();
            // Startpunt links-onder
            double x = 0, y = 0;

            sb.Append($"M {x} {y} ");


            // x = DsnLeft
            x = trap.DsnLeft;
            sb.Append($"l {x.ToSvg()} {y.ToSvg()} ");

            // y 
            // bereken snijpunt 
            x = 0;
            y = -trap.OptredeMaat;
            sb.Append($"l {x.ToSvg()} {y.ToSvg()} ");

            x = trap.DsnRight - trap.AantredeMaat;
            y = trap.DsnTop;
            sb.Append($"L {x.ToSvg()} {y.ToSvg()} ");


            x = trap.DsnRight;
            y = trap.DsnTop;
            sb.Append($"L {x.ToSvg()} {y.ToSvg()} ");

            x = trap.DsnRight;
            y = -trap.HoogteTotaal;
            sb.Append($"L {x.ToSvg()} {y.ToSvg()} ");




            return sb.ToString();
        }

        public static string GenerateTrapPath(SteekTrapEntity trap, BoundingBox bb, bool zonderAfrondingen = true)
        {
            var sb = new StringBuilder();

            if (trap.GebruikEigenLengte)
            {
                // eigen opgave lengte => simpel pad, geen trap tekenen
                sb.Append($"M {0} {0} ");
                sb.Append($"l {trap.LtProjZ.ToSvg()} {(-trap.HoogteTotaal).ToSvg()} ");
                sb.Append('Z');
                return sb.ToString();
            }


            var topRadius = zonderAfrondingen ? 0 : trap.TopRadius;
            var bottomRadius = zonderAfrondingen ? 0 : trap.BottomRadius;
            double wel = trap.WelMaat;
            double welV = trap.WelMaatVertikaal;
            double tandHoogte = trap.TandOpleggingBovenzijde?.TandHoogte ?? 0;
            double tandLengte = trap.TandOpleggingBovenzijde?.TandLengte ?? 0;
            double halsDikte = trap.TandOpleggingBovenzijde?.HalsDikte ?? trap.AantredeMaat;


            // Startpunt links-onder
            double x = 0, y = 0;
            sb.Append($"M {x} {y} ");
            bb.Add(x, y);

            // bovencontour
            for (int i = 0; i < trap.OptredeAantal1; i++)
            {
                // omhoog (optrede - TopRadius)
                // x = -wel, y = optrede 
                double optredeNetto = trap.OptredeMaat - topRadius - welV;
                if (optredeNetto > 0)
                {
                    x = -wel;
                    y = -optredeNetto;
                    sb.Append($"l {x.ToSvg()} {y.ToSvg()} ");
                    bb.Add(x, y);
                }


                if (welV > 0)
                {
                    x = 0;
                    y = -welV;
                    sb.Append($"l {x.ToSvg()} {y.ToSvg()} ");
                    //bb.Add(x, y);
                }

                // boogje bovenzijde
                //if (trap.TopRadius > 0)
                //    sb.Append($"a {topRadius},{topRadius} 0 0 1 {topRadius},{-topRadius} ");

                // rechts (aantrede - TopRadius - BottomRadius)
                // bovenste trede: gebruik halsDikte + tandLengte als totale breedte
                double aantredeBreedte = (trap.TandOpleggingBovenzijde != null && i == trap.OptredeAantal1 - 1)
                    ? halsDikte + tandLengte
                    : trap.AantredeMaat;
                double aantredeNetto = aantredeBreedte - topRadius - bottomRadius + wel;
                if (aantredeNetto > 0)
                {
                    x = aantredeNetto;
                    y = 0;
                    sb.Append($"l {x.ToSvg()} {y.ToSvg()} ");

                }

                // boogje onderzijde (afronding trede)
                //if (bottomRadius > 0)
                //    sb.Append($"a {bottomRadius},{bottomRadius} 0 0 1 {bottomRadius},{bottomRadius} ");

                //// omhoog (TopRadius + rest optrede)
                //if (topRadius > 0)
                //    sb.Append($"l 0,{-topRadius} ");
            }

            // eindpunt looplijn (bovenste voorzijde van trap)
            double x2 = trap.TandOpleggingBovenzijde != null
                ? (trap.OptredeAantal1 - 1) * trap.AantredeMaat + halsDikte
                : trap.AantredeMaat * trap.OptredeAantal1 - tandLengte;
            double y2 = -trap.OptredeMaat * trap.OptredeAantal1 - tandHoogte;

            // 100mm naar beneden, dan 100mm links
            sb.Append($"l 0 {tandHoogte.ToSvg()} l {(-tandLengte).ToSvg()} 0 ");

            // offsetlijn berekenen
            var (q1x, q1y, q2x, q2y) = OffsetLoopLijn((0, 0), (trap.AantredeMaat, -trap.OptredeMaat), trap.SchilDikte);

            // rechts snijpunt (verticale lijn door eindpunt)
            var rightIntersect = IntersectLines(
                (q1x, q1y), (q2x, q2y),
                (x2, y2), (x2, y2 + 1000)
            );

            // links snijpunt (horizontale lijn door beginpunt)
            var leftIntersect = IntersectLines(
                (q1x, q1y), (q2x, q2y),
                (0, 0), (1000, 0)
            );

            //CultureInfo culture = CultureInfo.InvariantCulture;

            if (rightIntersect is { } r)
            {
                sb.Append($"L {r.X.ToSvg()} {r.Y.ToSvg()} ");
                bb.Add(r.X, r.Y);

            }
            if (leftIntersect is { } l)
            {
                sb.Append($"L {l.X.ToSvg()} {l.Y.ToSvg()} ");
                bb.Add(l.X, l.Y);
            }

            // sluiten
            sb.Append('Z');

            return sb.ToString();
        }

        public static string GenerateTrapPathOptimized(SteekTrapEntity trap, BoundingBox bb)
        {
            var polygon = trap.DoorsnedePolygoon;
            if (polygon == null || polygon.Count < 3)
                return GenerateTrapPath(trap, bb);

            var sb = new StringBuilder();
            sb.Append($"M {polygon[0].X.ToSvg()} {polygon[0].Y.ToSvg()} ");
            bb.Add(polygon[0].X, polygon[0].Y);

            for (int i = 1; i < polygon.Count; i++)
            {
                sb.Append($"L {polygon[i].X.ToSvg()} {polygon[i].Y.ToSvg()} ");
                bb.Add(polygon[i].X, polygon[i].Y);
            }

            sb.Append('Z');
            return sb.ToString();
        }

        public static SvgPath? GenerateTrapOnderWapeningPath(SteekTrapEntity trap)
        {
            double dekkingOnder = trap.PlaatDekking.Onder.DekkingToe;

            double tandHoogte = trap.TandOpleggingBovenzijde?.TandHoogte ?? 0;
            double tandLengte = trap.TandOpleggingBovenzijde?.TandLengte ?? 0;
            double halsDikte  = trap.TandOpleggingBovenzijde?.HalsDikte ?? trap.AantredeMaat;

            double x2 = trap.TandOpleggingBovenzijde != null
                ? (trap.OptredeAantal1 - 1) * trap.AantredeMaat + halsDikte
                : trap.AantredeMaat * trap.OptredeAantal1 - tandLengte;
            double y2 = -trap.OptredeMaat * trap.OptredeAantal1 - tandHoogte;

            var (q1x, q1y, q2x, q2y) = OffsetLoopLijn((0, 0), (trap.AantredeMaat, -trap.OptredeMaat), trap.SchilDikte);

            var rightIntersect = IntersectLines(
                (q1x, q1y), (q2x, q2y),
                (x2, y2), (x2, y2 + 1000));

            var leftIntersect = IntersectLines(
                (q1x, q1y), (q2x, q2y),
                (0, 0), (1000, 0));

            if (rightIntersect == null || leftIntersect == null) return null;

            var (X, Y) = rightIntersect.Value;
            var l = leftIntersect.Value;

            double len = trap.SchuineMaat;

            // Inward normal from schil bottom face (toward interior of concrete)
            double nx = -trap.OptredeMaat / len;
            double ny = -trap.AantredeMaat / len;

            // Offset start point: (0,0) offset upward along y=0 face
            var p0 = (X: 0.0, Y: -dekkingOnder);

            // Offset end point: rightIntersect offset inward along schil normal
            var rOff = (X: X + nx * dekkingOnder, Y: Y + ny * dekkingOnder);

            // Miter at leftIntersect: intersection of offset horizontal line and offset schil line
            var lOff = (X: l.X + nx * dekkingOnder, Y: l.Y + ny * dekkingOnder);
            var miter = IntersectLines(
                p0, (1000.0, p0.Y),
                lOff, (lOff.X + trap.AantredeMaat, lOff.Y - trap.OptredeMaat));

            if (miter == null) return null;

            var sb = new StringBuilder();
            sb.Append($"M {p0.X.ToSvg()} {p0.Y.ToSvg()} ");
            sb.Append($"L {miter.Value.X.ToSvg()} {miter.Value.Y.ToSvg()} ");
            sb.Append($"L {rOff.X.ToSvg()} {rOff.Y.ToSvg()} ");

            return new SvgPath
            {
                D = sb.ToString(),
                Stroke = "blue",
                StrokeWidth = 2,
                Fill = "none",
                StrokeLineCap = "round",
                StrokeLineJoin = "round",
                VectorEffect = "non-scaling-stroke"
            };
        }

        private static (double, double, double, double) OffsetLoopLijn(
            (double X, double Y) p1,
            (double X, double Y) p2,
            double offset)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);

            // +offset → visueel omlaag in SVG
            double ox = -dy / len * offset;
            double oy = dx / len * offset;

            return (p1.X + ox, p1.Y + oy, p2.X + ox, p2.Y + oy);
        }

        private static (double X, double Y)? IntersectLines(
            (double X, double Y) p1, (double X, double Y) p2,
            (double X, double Y) q1, (double X, double Y) q2)
        {
            double dx1 = p2.X - p1.X;
            double dy1 = p2.Y - p1.Y;
            double dx2 = q2.X - q1.X;
            double dy2 = q2.Y - q1.Y;

            double det = dx1 * dy2 - dy1 * dx2;
            if (Math.Abs(det) < 1e-9) return null; // evenwijdig

            double t = ((q1.X - p1.X) * dy2 - (q1.Y - p1.Y) * dx2) / det;
            return (p1.X + t * dx1, p1.Y + t * dy1);
        }
    }

}
