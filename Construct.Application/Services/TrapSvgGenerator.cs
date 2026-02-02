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




    public static class PuntExtensions
    {
        public static List<VerticalExtent> ToVerticalExtents(
            this IEnumerable<Punt> punten)
        {
            return punten
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
                })
                .ToList();
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
            SvgLine hor = new SvgLine() { X1 = x1, X2 = x2, Y1 = 0, Y2 = 0 };
            SvgLine vert = new SvgLine() { X1 = 0, X2 = 0, Y1 = y1, Y2 = y2 };
            SvgText txtX1 = new SvgText() { X = x1, Y = 0 , Scale = scale, Text = titleX};
            SvgText txtX2 = new SvgText() { X = x2, Y = 0, Scale = scale, Text = titleX };
            SvgText txtY1 = new SvgText() { X = 0, Y = y1, Scale = scale, Text = titleY };
            SvgText txtY2 = new SvgText() { X = 0, Y = y2, Scale = scale, Text = titleY };


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


        public static SvgPath MakePath(IEnumerable<Punt> punten, double scaleY = 1, bool close = true, string fill = "none", string stroke = "var(--neutral-foreground-rest, black)")
        {
            SvgPath path = new SvgPath();
            path.Fill = fill;
            path.Stroke = stroke;
            var sb = new StringBuilder();

            var array = punten.ToArray();
            var p0 = array.First();

            p0.Y *= scaleY;
            sb.Append($"M {p0.X.ToString("F4", CultureInfo.InvariantCulture)} {p0.Y.ToString("F4", CultureInfo.InvariantCulture)} ");


            for (int i = 1; i<array.Count(); i++)
            {
                var p = array[i];
                p.Y *= scaleY;
                sb.Append($"L {p.X.ToString("F4", CultureInfo.InvariantCulture)} {p.Y.ToString("F4", CultureInfo.InvariantCulture)} ");
            }

            if (close)
                sb.Append("Z");

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

                var circleBasis = new SvgCircle
                {
                    Cx = pl.Position,
                    Cy = yBasis,
                    R = 0.05,
                    Fill = "black"
                };
                //group.Add(circleBasis);


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
                        $"{Math.Abs(pl.Magnitude):0.##}",
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
                    ? $"{Math.Abs(Math.Max(dl.StartMagnitude, dl.EndMagnitude)):0.##}"
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




        public static List<BaseSvg> ToSvgBAK(this BEAM.ILoad? load, double scale, double yBasis, double heightPx = 20, bool showValue = false)
        {
            List<BaseSvg> returnList = [];

            if (load == null) return returnList;

            SvgText svgText = new("?", 0, 0, 1, 0, scale: scale);
            //svgText.Scale = scale;
            List<Punt> punten = [];


            // reken uit hoe hoog de figuur moet worden.
            var hFig = heightPx / scale;
            yBasis /= scale;

            bool isPuntlast = false;
            bool isLijnlast = false;

            if (load is BEAM.PointLoad pl)
            {
                isPuntlast = true;
                punten.Add(new(pl.Position, yBasis)); // puntje aan de start
                punten.Add(new(pl.Position, yBasis - (pl.Magnitude / Math.Abs(pl.Magnitude) * hFig))); // achterzijde van de pijl
                if (pl.Magnitude > 0)
                {
                    //isPositief = true;
                    //punten.Reverse(); // opwaardse pijl!
                }
                svgText = new($"{Math.Abs(pl.Magnitude):0.##}", x: pl.Position, y: yBasis, angle: -90, scale: scale);
                returnList.Add(svgText);
            }


            if (load is BEAM.DistributedLoad dl)
            {
                isLijnlast = true;
                if (dl.StartMagnitude < 0 && dl.EndMagnitude > 0)
                {
                    throw new NotSupportedException("Driehoekslasten mogen niet door nul heen lopen (StartMagnitude en EindMagnitude moeten beide positief of beide negatief zijn!)");
                }


                if (dl.StartMagnitude > 0 | dl.EndMagnitude > 0)
                {
                    //isPositief = true;
                    //punten.Reverse();
                    //yBasis -= hFig;
                }

                var hMax = Math.Max(Math.Abs(dl.StartMagnitude), Math.Abs(dl.EndMagnitude));


                punten.Add(new(dl.EndPosition, yBasis)); // rechtonder
                punten.Add(new(dl.EndPosition, yBasis + ((dl.EndMagnitude) / hMax) * hFig)); // rechtsboven
                punten.Add(new(dl.StartPosition, yBasis + ((dl.StartMagnitude) / hMax) * hFig)); // linksboven
                punten.Add(new(dl.StartPosition, yBasis)); // linksonder

                double yText = yBasis - hFig / 2.0;

                string labelTekst = dl.Name;
                

                if (showValue) 
                    labelTekst = $"{Math.Abs(Math.Max(dl.StartMagnitude, dl.EndMagnitude)):0.##}";


                svgText = new(labelTekst, x: dl.LabelPosition.X, y: yText, scale: scale);
                svgText.Anchor = dl.LabelPosition.Anchor;
                returnList.Add(svgText);




            }

            var path = MakePath(punten, close: false);

            if (isLijnlast)
            {
                path.MarkerStart = "chevStart";
                path.MarkerEnd = "chevEnd";
                path.Fill = "lightgrey";

                SvgPath p2 = new SvgPath();
                SvgLine line = new SvgLine();
                line.X1 = punten.First().X;
                line.Y1 = punten.First().Y;
                line.X2 = punten.Last().X;
                line.Y2 = punten.Last().Y;

                returnList.Add(line);

            }
            if (isPuntlast)
            {
                path.MarkerEnd = "chevEnd";
            }

            returnList.Add(path);
            //returnList.Add(svgText);

            return returnList;

        }


        public static SvgPath ToPathOpt(this BEAM.ILoad? load, out SvgText svgText, double scale, double yBasis, double heigthPx = 20)
        {
            List<Punt> punten = [];
            svgText = new("?", 0, 0, 1, 0);

            // reken uit hoe hoog de figuur moet worden.
            var hFig = heigthPx / scale;
            yBasis /= scale;

            bool isPuntlast = false;
            bool isLijnlast = false;

            if (load is BEAM.PointLoad pl)
            {
                isPuntlast = true;
                punten.Add(new(pl.Position, yBasis)); // puntje aan de start
                punten.Add(new(pl.Position, yBasis - (pl.Magnitude / Math.Abs(pl.Magnitude) * hFig))); // achterzijde van de pijl
                if (pl.Magnitude > 0)
                {
                    //isPositief = true;
                    //punten.Reverse(); // opwaardse pijl!
                }
                svgText = new($"{Math.Abs(pl.Magnitude):0.##}", pl.Position, yBasis, -90);
            }
            

            if (load is BEAM.DistributedLoad dl)
            {
                isLijnlast = true;
                if (dl.StartMagnitude < 0 && dl.EndMagnitude > 0)
                {
                    throw new NotSupportedException("Driehoekslasten mogen niet door nul heen lopen (StartMagnitude en EindMagnitude moeten beide positief of beide negatief zijn!)");
                }


                if (dl.StartMagnitude > 0 | dl.EndMagnitude > 0)
                {
                    //isPositief = true;
                    //punten.Reverse();
                    yBasis -= hFig;
                }

                var hMax = Math.Max(Math.Abs(dl.StartMagnitude), Math.Abs(dl.EndMagnitude));

                
                punten.Add(new(dl.EndPosition, yBasis)); // rechtonder
                punten.Add(new(dl.EndPosition, yBasis + ((dl.EndMagnitude) / hMax) * hFig)); // rechtsboven
                punten.Add(new(dl.StartPosition, yBasis + ((dl.StartMagnitude) / hMax) * hFig)); // linksboven
                punten.Add(new(dl.StartPosition, yBasis)); // linksonder

                double yText = yBasis - hFig / 2.0;


                svgText = new($"{Math.Abs(Math.Max(dl.StartMagnitude, dl.EndMagnitude)):0.##}", dl.LabelPosition.X, yText);
                svgText.Anchor = dl.LabelPosition.Anchor;

                


            }

            var path = MakePath(punten, close: false);
            
            if (isLijnlast)
            {
                path.MarkerStart = "chevStart";
                path.MarkerEnd = "chevEnd";
                path.Fill = "lightgrey";
            }
            if (isPuntlast)
            {
                path.MarkerEnd = "chevEnd";
            }
            return path;
        }

        public static SvgPath ToPath(this BEAM.ILoad? load, out SvgText svgText, double scaleY = 0.01)
        {
            List<Punt> punten = [];
            svgText = new("?", 0, 0, 1, 0);

            if (load is BEAM.DistributedLoad dl)
            {
                punten.Add(new(dl.StartPosition, 0));
                punten.Add(new(dl.EndPosition, 0));
                punten.Add(new(dl.EndPosition, dl.EndMagnitude * scaleY));
                punten.Add(new(dl.StartPosition, dl.StartMagnitude * scaleY));

                svgText = new($"{Math.Max(dl.StartMagnitude, dl.EndMagnitude):0.##}", dl.StartPosition, 0, dl.EndPosition, 0);





            }

            return MakePath(punten);
        }


        public static void AddToSection(this SvgDocument svgDoc, SectionContent section, SBLigger beam)
        {
            //ParagraphContent par = new();

            var tag = svgDoc.Tag;


            switch (svgDoc.Name.Trim().ToLower())
            {
                case "figuur":
                    
                    

                    if (tag is Eurocode.Belastingen.BelastingGeval geval)
                    {
                        section.Elements.Add(new ParagraphContent($"Belastinggeval ({geval.Naam} | {geval.Type})", "Kop 3"));
                        section.Elements.Add(new ParagraphContent(svgDoc.Render()));

                        var loads = beam.Loads.Where(l => l.LoadCase == geval);

                        TableContent tableContent = new()
                        {
                            HideHeaders = false,
                            Title = ""
                        };

                        tableContent.Headers = [
                            new(){CellContent = new("naam")},
                                            new(){CellContent = new("omschrijving")},
                                            new(){CellContent = new("startpos.")},
                                            new(){CellContent = new("eindpos.")},
                                            new(){CellContent = new("startwaarde")},
                                            new(){CellContent = new("eindwaarde")},
                                            new(){CellContent = new("eenheid")},
                            ];


                        foreach (var l in loads)
                        {
                            tableContent.Rows.Add(
                            [
                              new TableCellContent(l.Name, "20mm"),
                                              new TableCellContent(l.Description ?? "..." , "100mm"),
                                              new TableCellContent(l.UserFriendlyStartPos, "12mm"),
                                              new TableCellContent(l.UserFriendlyEndPos, "12mm"),
                                              new TableCellContent(l.UserFriendlyStartValue, "12mm"),
                                              new TableCellContent(l.UserFriendlyEndValue, "12mm"),
                                              new TableCellContent(l.Unit, "12mm")
                            ]);


                        }

                        section.Elements.Add(tableContent.Clone());
                    }
                    break;

                case "vz":
                    //section.Elements.Add(new ParagraphContent($"Dwarskrachten ({tag})", "Kop 3"));
                    section.Elements.Add(new ParagraphContent(svgDoc.Render()));
                    break;

                case "my":
                    //section.Elements.Add(new ParagraphContent($"Momenten ({tag})", "Kop 3"));
                    section.Elements.Add(new ParagraphContent(svgDoc.Render()));
                    break;
            }

        }

        public static List<SvgDocument> ToSvgDocumentsOpt(
            this SBLigger beam, int w, int h, string style, 
            List<BelastingGeval> belastingGevallen,
            List<BelastingCombinatieTypeEnum> belastingCombinatieTypes,
            List<BelastingCombinatie> belastingCombinaties)
        {
            List<SvgDocument> collection = [];
            
            foreach (var bg in belastingGevallen ?? new List<BelastingGeval>())
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


        public static List<SvgDocument> ToSvgDocuments(this StrookEntity strook, string svgStyle, double widthPx = 800, double heightPx = 9999)
        {
            List<SvgDocument> collection = [];
            BoundingBox boundingBox = new();



            string xmlStrook = SvgGenerator.GenerateStrookSvgXml(strook, boundingBox, widthPx, heightPx, svgStyle, 
                toonMomentenlijn: !true, toonDwarskrachtenlijn: false, toonFiguren: true, strook.Beam.LoadContext?.BelastingGevallen.FirstOrDefault());
            var svgDoc = new SvgDocument(300, 300)
            {
                Name = "Figuur",
                Style = svgStyle,
                ViewBoxWidth = boundingBox.Width,
                ViewBoxHeight = boundingBox.Height,
                ViewBoxMinX = boundingBox.MinX,
                ViewBoxMinY = boundingBox.MinY
            };

           
            svgDoc.RawFragments.Clear();
            svgDoc.RawFragments.Add(xmlStrook);
            collection.Add(svgDoc);

            //par = new(svgDoc.Render());
            //section.Elements.Add(par);

            boundingBox.Reset();
            svgDoc = new(300, 300) { Style = svgStyle, Name="Vz" };
            xmlStrook = SvgGenerator.GenerateStrookSvgXml(strook, boundingBox, widthPx, heightPx, svgStyle, toonFiguren: false, toonDwarskrachtenlijn: true);
            svgDoc.ViewBoxWidth = boundingBox.Width;
            svgDoc.ViewBoxHeight = boundingBox.Height;
            svgDoc.ViewBoxMinY = boundingBox.MinY;
            svgDoc.ViewBoxMinX = boundingBox.MinX;



            svgDoc.RawFragments.Clear();
            svgDoc.RawFragments.Add(xmlStrook);
            collection.Add(svgDoc);

            //par = new(svgDoc.Render());
            //section.Elements.Add(par);


            boundingBox.Reset();
            svgDoc = new(300, 300) { Style = svgStyle, Name="My" };
            xmlStrook = SvgGenerator.GenerateStrookSvgXml(strook, boundingBox, widthPx, heightPx, svgStyle, toonFiguren: false, toonMomentenlijn: true);
            svgDoc.ViewBoxWidth = boundingBox.Width;
            svgDoc.ViewBoxHeight = boundingBox.Height;
            svgDoc.ViewBoxMinY = boundingBox.MinY;
            svgDoc.ViewBoxMinX = boundingBox.MinX;
            
            svgDoc.RawFragments.Clear();
            svgDoc.RawFragments.Add(xmlStrook);
            collection.Add(svgDoc);

            //par = new(svgDoc.Render());
            //section.Elements.Add(par);


            return collection;
        }


        public static List<BaseSvg> GenerateBeamShearDiagram(double scale, Mechanica.SimpleBeam.SBLigger beam, List<BeamResult> results, string fill, string stroke)
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

            //foreach (var e in verticalExtents)
            //{
            //    svgTags.Add(new SvgLine()
            //    {
            //        X1 = e.X,
            //        Y1 = e.MinY * -scaleY,
            //        X2 = e.X,
            //        Y2 = e.MaxY * -scaleY,
            //        Stroke = stroke,
            //        StrokeWidth = 0.5,
            //    });



            //    //svgTags.Add(new SvgText($"{e.MinY:0.#}", e.X, e.MinY * -scaleY, angle: -90, scale: scale));
            //}

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




            foreach (var pts in ptsCollection)
            {
                svgTags.Add(MakePath(pts, -scaleY, close: true, fill: fill, stroke: stroke));
            }


            return svgTags;

        }

        public static List<BaseSvg> GenerateBeamDeflectionDiagram(double scale, SBLigger beam, List<BeamResult> results, string stroke)
        {
            List<BaseSvg> svgTags = [];
            if (beam == null) return svgTags;

            SvgLine line = new SvgLine()
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
                SvgText txtW = new((minDataPoint.Y*1000).ToString("0.#"), x: minDataPoint.X, y: minDataPoint.Y * -scaleY, scale: scale);
                txtW.DY = 3.0 / scale;
                if (minDataPoint.Y < 0)
                    txtW.DominantBaseLine = "hanging";
                else txtW.DominantBaseLine = "base";
                svgTags.Add(txtW);
            }

            if (maxDataPoint.Y > 0.00)
            {
                SvgText txtW = new((maxDataPoint.Y * 1000).ToString("0.#"), x: maxDataPoint.X, y: maxDataPoint.Y * -scaleY, scale: scale);
                txtW.DY = 3.0 / scale;
                txtW.DominantBaseLine = "base";
                svgTags.Add(txtW);
            }

            


            return svgTags;
        }

        public static List<BaseSvg> GenerateBeamMomentDiagram(double scale, Mechanica.SimpleBeam.SBLigger beam, List<BeamResult> results, string fill, string stroke)
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

                // ORIGINEEL: Bouw polygonen van beide diagrammen (INDIVIDUEEL TEKENEN)
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

                var scaleY = grafiekAmplitude /
                    momentData
                    .Select(p => Math.Abs(p.Y))
                    .DefaultIfEmpty(1)
                    .Max();

                // ORIGINEEL: Teken alle lijnen individueel (meerdere door elkaar)
                foreach (var list in momentPointsCollection)
                {
                    var p = MakePath(list, -scaleY, true);
                    p.Fill = fill;
                    p.Stroke = stroke;
                    svgTags.Add(p);
                }

                // NIEUW: Teken ook met Clipper merged versie (TEST)
                // Uncomment dit als je de merged versie wilt testen
                /*
                List<(double x, double m)> normalPolygon = [];
                List<(double x, double m)> accidentalPolygon = [];

                foreach (var r in results)
                {
                    if (r != null)
                    {
                        normalPolygon.AddRange(r.MomentDiagram);
                        accidentalPolygon.AddRange(r.MomentDiagramAccidentalFixity);
                    }
                }

                if (normalPolygon.Count > 0)
                {
                    var mergedPolygon = MergePolygonsWithClipper(
                        BuildMomentPolygon(normalPolygon, beam.Length),
                        BuildMomentPolygon(accidentalPolygon, beam.Length));

                    if (mergedPolygon.Count > 0)
                    {
                        var svgPunten = mergedPolygon.Select(p => new Punt(p.x, p.m)).ToList();
                        var p = MakePath(svgPunten, -scaleY, true);
                        p.Fill = "yellow"; // Ander kleur zodat je het verschil ziet
                        p.Stroke = "red";
                        svgTags.Add(p);
                    }
                }
                */

                // 6. Toon min/max waarden
                var minDataPoint = momentData.OrderBy(p => p.Y).FirstOrDefault();
                var maxDataPoint = momentData.OrderBy(p => p.Y).LastOrDefault();

                if (Math.Abs(minDataPoint.Y) > 0.001)
                {
                    SvgText txtM = new(minDataPoint.Y.ToString("0.#"), x: minDataPoint.X, y: minDataPoint.Y * -scaleY, scale: scale);
                    txtM.DY = 3.0 / scale;
                    txtM.DominantBaseLine = minDataPoint.Y < 0 ? "hanging" : "base";
                    svgTags.Add(txtM);
                }

                if (Math.Abs(maxDataPoint.Y) > 0.001)
                {
                    SvgText txtM = new(maxDataPoint.Y.ToString("0.#"), x: maxDataPoint.X, y: maxDataPoint.Y * -scaleY, scale: scale);
                    txtM.DY = -3.0 / scale;
                    txtM.DominantBaseLine = maxDataPoint.Y < 0 ? "hanging" : "base";
                    svgTags.Add(txtM);
                }
            }
            return svgTags;
        }

        /// <summary>
        /// Bouwt een gesloten polygon voor het momentdiagram
        /// </summary>
        private static List<(double x, double m)> BuildMomentPolygon(List<(double x, double m)> points, double beamLength)
        {
            if (points.Count == 0) return [];

            var polygon = new List<(double x, double m)> { (0, 0) };
            var sorted = points.OrderBy(p => p.x).ToList();
            
            foreach (var pt in sorted)
                polygon.Add(pt);
            
            polygon.Add((beamLength, 0));
            
            foreach (var pt in sorted.AsEnumerable().Reverse())
                polygon.Add(pt);

            return polygon;
        }

        /// <summary>
        /// Merged twee polygonen met Clipper2 library (UNION operation)
        /// </summary>
        private static List<(double x, double m)> MergePolygonsWithClipper(
            List<(double x, double m)> polygon1,
            List<(double x, double m)> polygon2)
        {
            try
            {
                // Converteer naar Clipper2 PathD (dubbele precisie)
                var path1 = new Clipper2Lib.PathD(
                    polygon1.Select(p => new Clipper2Lib.PointD(p.x, p.m)).ToList());
                
                var path2 = new Clipper2Lib.PathD(
                    polygon2.Select(p => new Clipper2Lib.PointD(p.x, p.m)).ToList());

                var clipper = new Clipper2Lib.ClipperD(); 
                clipper.AddSubject(new Clipper2Lib.PathsD { path1 });
                clipper.AddClip(new Clipper2Lib.PathsD { path2 });

                // Execute vult solution parameter in en retourneert bool
                var solution = new Clipper2Lib.PathsD();
                clipper.Execute(Clipper2Lib.ClipType.Union, Clipper2Lib.FillRule.EvenOdd, solution);

                if (solution.Count == 0) return polygon1;

                var largestPath = solution.OrderByDescending(p => p.Count).First();
                return largestPath.Select(pt => (pt.x, pt.y)).ToList();
            }
            catch
            {
                return polygon1;
            }
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

            double maxWidthBereikt = layout.ViewBox.ScaleX(wPx) / scale;
            
            if (maxWidthBereikt > 1)
            {

                string debug = "wat kan ik hiermee";
            }

            var margin = layout.Margin;
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
            SvgDocumentInfo? info = null;

            if (strook == null) 
                return "";

            // maak svgObjecten
            List<BaseSvg> svgTags = [];
            string fill = "lightblue";
            string stroke = "darkblue";

            // 💡maak de paden en maatlijnen
            List<Punt> punten = [new(), new(strook.Beam.Length, 0)];

            //List<SvgPath> svgPaths = [MakePath(punten)];
            //List<SvgDimLine> dimLines = [];
            //List<SvgText> teksten = [];
            string status = "";

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
            SvgDimLine maat1 = new() { Text = $"{strook.Beam.Length:0.000 m}", X2 = strook.Beam.Length, Offset = posY };
                maat1.Scale = scale;
                maat1.MarkerStart = "chevStart";
                maat1.MarkerEnd = "chevEnd";

                //

                svgTags.Add(maat1);
                //

               

            foreach (double pos in positions)
            {
                SvgText absolute = new($"{pos:0.000}", pos, 1.2 * posY, angle: -90, scale: scale);
                absolute.DominantBaseLine = "middle";
                absolute.Anchor = "end";
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

                SvgText txtV1 = new((shear1.Y).ToString("0.00"), x: shear1.X, y: shear1.Y * -scaleY, scale: scale);
                txtV1.DY = 3.0 / scale;
                if (v1 < 0)
                    txtV1.DominantBaseLine = "hanging";
                else txtV1.DominantBaseLine = "base";
                svgTags.Add(txtV1);

                var shear2 = shearOrder.Last();
                var v2 = shear2.Y;
                SvgText txtV2 = new(shear2.Y.ToString("0.00"), x: shear2.X, y: shear2.Y * -scaleY, scale: scale);
                txtV2.DY = -3.0 / scale;
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
                    SvgText txtM = new(minDataPoint.Y.ToString("0.00"), x: minDataPoint.X, y: minDataPoint.Y * -scaleY, scale: scale);
                    txtM.DY = 3.0 / scale;
                    if (minDataPoint.Y < 0)
                        txtM.DominantBaseLine = "hanging";
                    else txtM.DominantBaseLine = "base";
                    svgTags.Add(txtM);
                }

                if (Math.Abs(maxDataPoint.Y) > 0.001)
                {
                    SvgText txtM = new(maxDataPoint.Y.ToString("0.00"), x: maxDataPoint.X, y: maxDataPoint.Y * -scaleY, scale: scale);
                    txtM.DY = -3.0 / scale;
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
                Id = kolom.Guid.ToString(),
                Title = kolom.Merk ?? "KOLOM",
                Description = "kolom",
                Label = kolom.Merk ?? "LABEL",
            };

            // 💡maak de paden en maatlijnen
            List<Punt> punten = [new(), new(kolom.Breedte, 0), new(kolom.Breedte, -kolom.Hoogte), new(0, -kolom.Hoogte)];
            List<SvgPath> svgPaths = [MakePath(punten)];
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


            return svg2;

        }


        public static string GenerateBordesSvgXml(BordesEntity bordes, BoundingBox bb, double actualWidthPx, double actualHeightPx, string style = "width:auto; height:auto;", bool toonMaatlijnen = true)
        {
            SvgHelper svgHelper = new();
            SvgDocumentInfo? info = new()
            {
                Id = bordes.Guid.ToString(),
                Title = bordes.Merk ?? "TRAP",
                Description = "steektrap",
                Label = bordes.Merk ?? "LABEL",

            };

            // 💡maak de paden en maatlijnen
            List<Punt> punten = [new(),new(bordes.Lengte, 0), new(bordes.Lengte, -bordes.Breedte), new(0, -bordes.Breedte) ];
            List<SvgPath> svgPaths = [MakePath(punten)];

            if(bordes.Trap1?.AansluitendElement != null)
            {
                var c = bordes.Trap1;
                double left = c.Pos.Start;
                double bot = 0;
                double right = c.Pos.End;
                double top = -c.Breedte;

                List<Punt> trap1 = [new(left, bot),new(right,bot), new(right,top), new(left,top)];

                var path1 = MakePath(trap1);
                path1.Fill = "yellow";

                svgPaths.Add(path1);
            }
            if (bordes.Trap2?.AansluitendElement != null)
            {
                var c = bordes.Trap2;
                double left = c.Pos.Start;
                double bot = 0;
                double right = c.Pos.End;
                double top = -c.Breedte;

                List<Punt> trap2 = [new(left, bot), new(right, bot), new(right, top), new(left, top)];

                var path2 = MakePath(trap2);
                path2.Fill = "yellow";
                svgPaths.Add(path2);
            }


            var vsY1 = -100.0;
            if (bordes.Trap1 != null) vsY1 = -bordes.Trap1.Breedte;
            var vsY2 = vsY1 - bordes.BreedteVersterkteStrook;


            List<Punt> vs = [new(0, vsY1), new(bordes.Lengte, vsY1), new(bordes.Lengte, vsY2), new (0, vsY2)];
            var vsPath = MakePath(vs);
            vsPath.Fill = "darkblue";
            vsPath.FillOpacity = 0.1;
            vsPath.Stroke = "red";
            vsPath.StrokeDashArray = "60,20,10,20";
            svgPaths.Add(vsPath);


            var hartY = (vsY1 + vsY2) / 2.0;
            List<Punt> hart = [new(-50, hartY), new(bordes.Lengte + 50, hartY)];
            var hartPath = MakePath(hart);
            hartPath.StrokeWidth = 0.5;
            hartPath.StrokeDashArray = "2,10";
            svgPaths.Add(hartPath);

            var yStart = bordes.Trap1?.Breedte ??  100;
            yStart += 40;
            var breedteOplegging = 120; 

            List<Punt> opLinks = [new(0, -yStart), new(breedteOplegging, -yStart), new(breedteOplegging, -bordes.Breedte), new (0, -bordes.Breedte)];
            var opLinksPath = MakePath(opLinks);
            opLinksPath.Stroke = "red";
            opLinksPath.StrokeDashArray = "5,5";
            opLinksPath.Fill = "none";
            svgPaths.Insert(0, opLinksPath);

            List<Punt> opRechts = [new(bordes.Lengte, -yStart), new(bordes.Lengte -breedteOplegging, -yStart), new(bordes.Lengte - breedteOplegging, -bordes.Breedte), new(bordes.Lengte, -bordes.Breedte)];
            var opRechtsPath = MakePath(opRechts);
            opRechtsPath.Stroke = "red";
            opRechtsPath.StrokeDashArray = "5,5";
            opRechtsPath.Fill = "none";
            svgPaths.Insert(0, opRechtsPath);



            // No FEM analysis voor bordes
            //SBL.SBLigger sbl = new SBL.SBLigger(bordes.Lengte * 0.001);

            //var l1 = SBL.Examples.GetTest1();

            //SBL.Examples.RunTest(l1);


            // BEAM
            BEAM.Examples.Run(withP: true, withRect: false, withTri: false);



            // prutsen voor dwarskrachten en momenten

            double baseY = -bordes.Breedte / 2.0;
            List<Punt> testShear = [new(0,baseY)];
            List<Punt> testMoment = [new(0,baseY)];

            Construct.Application.Interfaces.Beam.SimpleBeam _simpleBeam = new();
            double length = bordes.Lengte * 0.001;
            _simpleBeam = new() { Length = length };
            double position = length * 0.5;
            double magnitude = -3.00;
            _simpleBeam.Loads.Add(new Construct.Application.Interfaces.Beam.PointLoad(position, magnitude));
            //_simpleBeam.Loads.Add(new DistributedLoad(0, length, -5, -0));
            _simpleBeam.SolveReactions();
            
            Console.WriteLine($"R_A = {_simpleBeam.StartReaction:0.0} kN, R_B = {_simpleBeam.EndReaction:0.0} kN");

            List<double> positions = new();
            for (double pos = 0; Math.Round(pos, 4) <= length; pos += 0.1)
            {
                positions.Add(Math.Round(pos, 4));
            }
            

            foreach (var pos in positions)
            {
                double shear = _simpleBeam.ShearAt(pos);
                double moment = _simpleBeam.MomentAt(pos);
                Console.WriteLine($"x = {pos:0.0} m: V = {shear:0.00} kN, M = {moment:0.00} kNm");
               
                testShear.Add(new Punt((pos * 1000), (baseY - shear * 50)));
                testMoment.Add(new Punt((pos * 1000),(baseY - moment * 100)));
            }
            testShear.Add(new(bordes.Lengte, baseY));
            testMoment.Add(new(bordes.Lengte, baseY));

            var shearPath = MakePath(testShear);
            shearPath.Stroke = "blue";
            shearPath.Fill = "none";

            var momentPath = MakePath(testMoment);
            momentPath.Stroke = "red";
            momentPath.Fill = "none";

            //svgPaths.Add(shearPath);
            //svgPaths.Add(momentPath);

            // todo, naar generieke svg-generator methode
            List<SvgDimLine> dimLines = TrapSvgGenerator.GenerateBordesDimLines(bordes);






            List<SvgText> teksten = TrapSvgGenerator.GenerateBordesSvgText(bordes);

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

            var svg2 = svgHelper.GetSvgStringOptimal(info, vbWithMargins, svgPaths, dimLines, teksten, actualWidthPx, actualHeightPx, style, status);


            return svg2;
        }


    }


    public static class TrapSvgGenerator
    {

        public static SvgPath GenerateLijnlast(double x1, double y1, double x2, double y2, double h, string tekst)
        {
            SvgPath path = new SvgPath();
            var sb = new StringBuilder();


            // eigen opgave lengte => simpel pad, geen trap tekenen
            sb.Append($"M {x1} {y1} ");
            //sb.Append($"l {trap.LengteTotaal.ToSvg()} {(-trap.HoogteTotaal).ToSvg()} ");
            //sb.Append("Z");
            //return sb.ToString();


            var pathData = sb.ToString();

            return path;


        }

        public static List<SvgPath> GenerateTrapPaths(SteekTrapEntity trap, BoundingBox bb, bool zonderAfrondingen = true)
        {
            List<SvgPath> returnList = [];

            // referentie onderkant
            //returnList.Add(GenerateBottomRef(trap));
            //returnList.Add(GenerateTopRef(trap));



            // doorsnede
            returnList.Add(GenerateTrapSvgPath(trap, bb, zonderAfrondingen));





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

        public static string? GenerateBase64PngFromSvgXml(string svgXml)
        {
            string? base64Png = null;
            return base64Png;
        }




        public static string GenerateTrapSvgXml(SteekTrapEntity trap, BoundingBox bb, double actualWidthPx, double actualHeightPx, string style = "width:auto; height:auto;", bool toonMaatlijnen = true)
        {
            SvgHelper svgHelper = new();
            SvgDocumentInfo? info = new()
            {
                Id = trap.Guid.ToString(),
                Title = trap.Merk ?? "TRAP",
                Description = "steektrap",
                Label = trap.Merk ?? "LABEL",

            };

            // 💡maak de paden en maatlijnen
            var svgPaths = GenerateTrapPaths(trap, bb);

            var lijnlast = GenerateLijnlast(0, 0, trap.LengteTotaal, 0, 100, $"q={trap.Krachten.Gk:0.##}({trap.Krachten.Lijnlast_qk:0.##})");

            List<SvgDimLine> dimLines = [];

            if (toonMaatlijnen)
                dimLines = GenerateTrapDimLines(trap);

            // Bepaal de viewBox
            var x = Math.Min(bb.MinX, 0);
            var y = Math.Min(bb.MinY, -trap.HoogteTotaal);
            var w = Math.Max(bb.Width, trap.LengteTotaal + trap.WelMaat); // tijdelijke oplossing, gebruik later een boundingBox voor in assemblage-entiteit.
            var h = Math.Max(bb.Height, trap.HoogteTotaal);
            SvgHelper.SvgViewBox viewBox = new(x, y, w, h);

            // Maak een nieuwe viewbox aan met het aantal regelafstanden in rondom de tekening.
            var vbWithMargins = viewBox.WithMarginsByText(
                leftLines: 5,
                rightLines: 5,
                topLines: 4,
                bottomLines: 1,
                fontSizePx: 12,
                lineHeight: 1.5,
                actualWidthPx: actualWidthPx,
                actualHeightPx: actualHeightPx
            );



            var teksten = GenerateTrapTeksten(trap, bb, vbWithMargins.GetScale(actualWidthPx, actualHeightPx));

            var status = trap.Akkoord ? "" : "has-warning";

            var svg2 = svgHelper.GetSvgStringOptimal(info, vbWithMargins, svgPaths, dimLines, teksten, actualWidthPx, actualHeightPx, style, status);


            return svg2;
        }


        public static List<SvgText> GenerateTrapTeksten(SteekTrapEntity trap, BoundingBox bb, double scale)
        {
            List<SvgText> returnList = [];


            var x1 = trap.LengteTotaal / 2;
            var x2 = x1 + trap.AantredeMaat;
            var y1 = -trap.HoogteTotaal / 2;
            var y2 = y1 - trap.OptredeMaat;
            var txt = new SvgText($"\r\n{trap.WapeningSchil}", x1, y1, x2, y2);
            {

            }
            ;
            txt.AddToBoundingBox(bb, scale);

            //returnList.Add(txt);

            return returnList;

        }



        public static List<SvgText> GenerateBordesSvgText(BordesEntity bordes)
        {
            List<SvgText> list = [];

            if (bordes.Trap1 != null)
            {
                var c = bordes.Trap1;
                
                if (c.AansluitendElement != null)
                    list.Add(new SvgText(c.AansluitendElement?.Merk ?? "TRAP" , c.Randafstand, 100, c.Randafstand + c.Lengte, 100));
                
                double yVS = c.Breedte + bordes.BreedteVersterkteStrook / 2.0;
                list.Add(new SvgText("versterkte strook",x: 0, y: -yVS, x2:bordes.Lengte, y2:-yVS, scale:1));
            }

            if (bordes.Trap2?.AansluitendElement != null)
            {
                var c = bordes.Trap2;

                if (c.AansluitendElement != null)
                    list.Add(new SvgText(c.AansluitendElement?.Merk ?? "TRAP", bordes.Lengte - c.Randafstand - c.Lengte, 100, bordes.Lengte -  c.Randafstand, 100));
            }

            
            list.Add(new SvgText("basis strook",x: 0, y: -bordes.Breedte + 500, x2:bordes.Lengte, y2:-bordes.Breedte +500, scale:1));



            return list;
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
                Text = $"{kolom.Hoogte.ToString("0")}",
                StrokeColor = "var(--neutral-foreground-rest, black)",
            });

            return dimLines;
        }


        public static List<SvgDimLine> GenerateBordesDimLines(BordesEntity bordes)
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
                Text = $"{bordes.Breedte.ToString("0")}",
                StrokeWidth = 1,
                StrokeColor = "var(--neutral-foreground-rest, black)"
            });

            if (bordes.Trap1?.AansluitendElement != null)
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
                    Text = $"{c.Lengte.ToString("0")}",
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
                    Text = $"{bordes.BreedteVersterkteStrook.ToString("0")}",
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
                var armY2 = c.Breedte + bordes.BreedteVersterkteStrook / 2.0;
                returnList.Add(new SvgDimLine
                {
                    Mode = DimLineMode.Vertical,
                    X1 = c.Randafstand + c.Lengte / 2.0,
                    Y1 = -armY1,
                    X2 = c.Randafstand + c.Lengte / 2.0,
                    Y2 = -armY2,
                    Offset = 0,
                    OffsetLines = 0,
                    //Value = trap.HoogteTotaal,
                    Text = $"{(armY2 - armY1).ToString("0")}",
                    StrokeWidth = 1,
                    StrokeColor = "var(--neutral-foreground-rest, black)"
                });



            }


            if (bordes.Trap2?.AansluitendElement != null)
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
                    Text = $"{c.Lengte.ToString("0")}",
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
                var armY2 = c.Breedte + bordes.BreedteVersterkteStrook / 2.0;
                returnList.Add(new SvgDimLine
                {
                    Mode = DimLineMode.Vertical,
                    X1 = bordes.Lengte - c.Randafstand - c.Lengte / 2.0,
                    Y1 = -armY1,
                    X2 = bordes.Lengte - c.Randafstand - c.Lengte / 2.0,
                    Y2 = -armY2,
                    Offset = 0,
                    OffsetLines = 0,
                    //Value = trap.HoogteTotaal,
                    Text = $"{(armY2 - armY1).ToString("0")}",
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
                Text = $"{bordes.Lengte.ToString("0")}",
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
                Text = $"{bordes.Lengte.ToString("0")}",
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


        public static List<SvgDimLine> GenerateTrapDimLines(SteekTrapEntity trap)
        {
            List<SvgDimLine> returnList = [];



            // totaal (ver.)
            returnList.Add(new SvgDimLine
            {
                Mode = DimLineMode.Vertical,
                X1 = 0,
                Y1 = 0,
                X2 = trap.LengteTotaal,
                Y2 = -trap.HoogteTotaal,
                Offset = trap.WelMaat,
                OffsetLines = 3,
                //Value = trap.HoogteTotaal,
                Text = $"{trap.OptredeAantal1}×{trap.OptredeMaat:0.#} = {trap.HoogteTotaal:0}",
                StrokeWidth = 1,
                StrokeColor = "var(--neutral-foreground-rest, black)"
            });





            // totaal (hor.)
            returnList.Add(new SvgDimLine
            {
                Mode = DimLineMode.Horizontal,
                X1 = 0,
                Y1 = 0,
                X2 = trap.LengteTotaal,
                Y2 = -trap.HoogteTotaal,
                Offset = 0,
                OffsetLines = 2,
                //Value = trap.AantredeMaat,
                Text = $"{trap.LengteTotaal:0}",
                StrokeWidth = 1,
                StrokeColor = "var(--neutral-foreground-rest, black)"
            });


            // diagonaal
            returnList.Add(new SvgDimLine
            {
                Mode = DimLineMode.Aligned,
                X1 = 0,
                Y1 = 0,
                X2 = trap.LengteTotaal,
                Y2 = -trap.HoogteTotaal,
                Offset = 0,
                OffsetLines = -3,
                Text = $"{trap.LengteSchuin:0}",

            });




            if (!trap.GebruikEigenLengte)
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
                        X1 = trap.LengteTotaal - trap.TandOpleggingBovenzijde.TandLengte,
                        Y1 = -trap.HoogteTotaal + trap.TandOpleggingBovenzijde.TandHoogte,
                        X2 = trap.LengteTotaal,
                        Y2 = -trap.HoogteTotaal,
                        Offset = -trap.TandOpleggingBovenzijde.TandHoogte,
                        OffsetLines = -2,
                    });

                    returnList.Add(new SvgDimLine
                    {
                        Mode = DimLineMode.Vertical,
                        X1 = trap.LengteTotaal - trap.TandOpleggingBovenzijde.TandLengte,
                        Y1 = -trap.HoogteTotaal + trap.TandOpleggingBovenzijde.TandHoogte,
                        X2 = trap.LengteTotaal,
                        Y2 = -trap.HoogteTotaal,
                        Offset = -trap.TandOpleggingBovenzijde.TandLengte,
                        OffsetLines = -2,
                    });

                }

            }


            return returnList;
        }

        public static SvgPath GenerateTrapSvgPath(SteekTrapEntity trap, BoundingBox bb, bool zonderAfrondingen = true)
        {
            var pathData = GenerateTrapPath(trap, bb, zonderAfrondingen);
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

        public static SvgPath GenerateBottomRef(SteekTrapEntity trap)
        {
            return new SvgPath
            {
                D = "M -1000 0 l 9000 0",
                Stroke = "black",
                StrokeWidth = 1,
                Fill = "lightgray",
                Opacity = 1.0,
                StrokeDashArray = "",
                StrokeLineJoin = "miter",
                StrokeLineCap = "butt",
                FillRule = "nonzero"
            };
        }


        public static SvgPath GenerateTopRef(SteekTrapEntity trap)
        {
            int top = (int)-trap.HoogteTotaal;

            return new SvgPath
            {
                D = $"M -1000 {top} l 9000 0",
                Stroke = "black",
                StrokeWidth = 1,
                Fill = "lightgray",
                Opacity = 1.0,
                StrokeDashArray = "",
                StrokeLineJoin = "miter",
                StrokeLineCap = "butt",
                FillRule = "nonzero"
            };
        }



        public static string GenerateTrapPath(SteekTrapEntity trap, BoundingBox bb, bool zonderAfrondingen = true)
        {
            var sb = new StringBuilder();

            if (trap.GebruikEigenLengte)
            {
                // eigen opgave lengte => simpel pad, geen trap tekenen
                sb.Append($"M {0} {0} ");
                sb.Append($"l {trap.LengteTotaal.ToSvg()} {(-trap.HoogteTotaal).ToSvg()} ");
                sb.Append("Z");
                return sb.ToString();
            }


            var topRadius = zonderAfrondingen ? 0 : trap.TopRadius;
            var bottomRadius = zonderAfrondingen ? 0 : trap.BottomRadius;
            double wel = trap.WelMaat;
            double welV = trap.WelMaatVertikaal;
            double tandHoogte = trap.TandOpleggingBovenzijde?.TandHoogte ?? 0;
            double tandLengte = trap.TandOpleggingBovenzijde?.TandLengte ?? 0;


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
                // x = wel + aantrede
                double aantredeNetto = trap.AantredeMaat - topRadius - bottomRadius + wel;
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
            double x2 = trap.AantredeMaat * trap.OptredeAantal1 - tandLengte;
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
            sb.Append("Z");

            return sb.ToString();
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
