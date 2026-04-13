using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text;

namespace Construct.Domain.Entities
{
    public enum DimLineMode
    {
        Aligned,     // maatlijn evenwijdig aan meetlijn
        Horizontal,  // maatlijn altijd horizontaal, meetpunten projecteren op laagste Y
        Vertical     // maatlijn altijd verticaal, meetpunten projecteren op laagste X
    }

  
    public class SvgDimLine : BaseSvg
    {
        private readonly SvgGroup _group = new();
        public double Scale { get; set; } = 1.0;
        public string? MarkerStart { get; set; } = "circle-cross";
        public string? MarkerEnd { get; set; } = "circle-cross";



        // Originele punten
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }

        // Offset in viewBox-units
        public double Offset { get; set; } = 0.0;

        // Extra offset in aantal tekstregels
        public int OffsetLines { get; set; } = 0;

        public DimLineMode Mode { get; set; } = DimLineMode.Aligned;

        public string Text { get; set; } = string.Empty;
        public string StrokeColor { get; set; } = "var(--neutral-foreground-rest)";
        public double StrokeWidth { get; set; } = 1;
        public bool ShowExtensionLines { get; set; } = true;
        public string StringFormat { get; set; } = "0";

        private double _length;

        public string DisplayValue
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Text))
                    return Text;

                _length = Mode switch
                {
                    DimLineMode.Aligned => Math.Sqrt((X2 - X1) * (X2 - X1) + (Y2 - Y1) * (Y2 - Y1)),
                    DimLineMode.Horizontal => Math.Abs(X2 - X1),
                    DimLineMode.Vertical => Math.Abs(Y2 - Y1),
                    _ => 0
                };

                return _length.ToString(StringFormat);
            }
        }


        private double dx => X2 - X1;
        private double dy => Y2 - Y1;
        private double len => Math.Sqrt(dx * dx + dy * dy);
        private double nx => len > 0 ? -dy / len : 0;  // normale vector
        private double ny => len > 0 ? dx / len : 0;


        public double Angle
        {
            get
            {
                return Mode switch
                {
                    DimLineMode.Aligned => Math.Atan2(Y2 - Y1, X2 - X1) * 180.0 / Math.PI,
                    DimLineMode.Horizontal => 0.0,
                    DimLineMode.Vertical => -90.0,
                    _ => 0.0
                };
            }
        }

        protected override string TagName => "g"; // DimLines zijn een group

        /// <summary>
        /// Berekent de offsetpunten gegeven de fontSize, lineHeight en viewBox schaal.
        /// Houdt rekening met Mode (Aligned, Horizontal, Vertical)
        /// </summary>
        public (double X1o, double Y1o, double X2o, double Y2o, double Angle) GetOffsetPoints(
            double fontSizePx, double lineHeight, double scale)
        {
            double textLineSpaceUnits = (fontSizePx / scale) * lineHeight;
            double offsetTotal = Offset + OffsetLines * textLineSpaceUnits;



            double x1o, y1o, x2o, y2o, angle;

            switch (Mode)
            {
                case DimLineMode.Aligned:
                    x1o = X1 + nx * offsetTotal;
                    y1o = Y1 + ny * offsetTotal;
                    x2o = X2 + nx * offsetTotal;
                    y2o = Y2 + ny * offsetTotal;
                    angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
                    break;

                case DimLineMode.Horizontal:
                    double baseY = Math.Min(Y1, Y2) - offsetTotal;
                    x1o = X1;
                    x2o = X2;
                    y1o = baseY;
                    y2o = baseY;
                    angle = 0;
                    break;

                case DimLineMode.Vertical:
                    double baseX = Math.Min(X1, X2) - offsetTotal;
                    x1o = baseX;
                    x2o = baseX;
                    y1o = Y1;
                    y2o = Y2;
                    angle = -90;
                    break;

                default:
                    x1o = X1; y1o = Y1; x2o = X2; y2o = Y2; angle = 0;
                    break;
            }

            return (x1o, y1o, x2o, y2o, angle);
        }

        /// <summary>
        /// Middelpunt voor tekst
        /// </summary>
        public (double MidX, double MidY) GetMidPoint(double fontSizePx, double lineHeight, double scale)
        {
            var (x1o, y1o, x2o, y2o, _) = GetOffsetPoints(fontSizePx, lineHeight, scale);
            return ((x1o + x2o) / 2, (y1o + y2o) / 2);
        }

        protected override void ApplyAttributes()
        {
            // SvgDimLine heeft geen eigen attributen, want het is een container
            // maar je kunt wel defaults zetten als je wilt:
            Set("stroke", StrokeColor);
            Set("stroke-width", StrokeWidth);
        }

        private void Build()
        {
            _group.Content.Children.Clear();
            double fontPx = 12;
            double lineHeight = 1.5;
            double scale = Scale;

            var (x1o, y1o, x2o, y2o, angle) = GetOffsetPoints(fontPx, lineHeight, scale);

            // Bepaal de richting voor marker orientation
            bool isReversed = false;
            if (Mode == DimLineMode.Horizontal && x2o < x1o)
                isReversed = true;
            else if (Mode == DimLineMode.Vertical && y2o < y1o)
                isReversed = true;

            // --- 1. Hoofdmaatlijn ---
            var mainLine = new SvgLine
            {
                X1 = x1o,
                Y1 = y1o,
                X2 = x2o,
                Y2 = y2o,
                Stroke = StrokeColor,
                StrokeWidth = StrokeWidth,
                MarkerStart = isReversed ? MarkerEnd : MarkerStart,
                MarkerEnd = isReversed ? MarkerStart : MarkerEnd,
                VectorEffect = "non-scaling-stroke",
            };

            _group.Content.Children.Add(mainLine);

            // --- 2. Tekst ---
            var (mx, my) = GetMidPoint(fontPx, lineHeight, scale);
            
            double dx = 0;
            double dy = 0;
            
            if (Mode == DimLineMode.Horizontal)
            {
                dy = -fontPx / scale * 0.67;
            }
            else if (Mode == DimLineMode.Vertical)
            {
                // Voor verticale tekst: offset naar rechts (in geroteerde ruimte)
                // Met -90° rotatie wordt dit "links" van de lijn
                // Dus we willen positieve dx voor offset naar rechts vóór rotatie
                dx = fontPx / scale * 0.67;
            }
            else // Aligned
            {
                dy = -fontPx / scale * 0.67;
            }

            var text = new SvgText(DisplayValue, x: mx + dx, y: my + dy)
            {
                Anchor = "middle",
                Pts = fontPx,
                Scale = Scale,
                Angle = angle,
            };

            _group.Content.Children.Add(text);

            // --- 3. Extension lines ---
            if (ShowExtensionLines)
            {
                var ext1 = new SvgLine
                {
                    X1 = X1,
                    Y1 = Y1,
                    X2 = x1o,
                    Y2 = y1o,
                    Stroke = StrokeColor,
                    StrokeWidth = 0.5,
                    StrokeDashArray = "2,2",
                    VectorEffect = "non-scaling-stroke"
                };

                var ext2 = new SvgLine
                {
                    X1 = X2,
                    Y1 = Y2,
                    X2 = x2o,
                    Y2 = y2o,
                    Stroke = StrokeColor,
                    StrokeWidth = 0.5,
                    StrokeDashArray = "2,2",
                    VectorEffect = "non-scaling-stroke"
                };

                _group.Content.Children.Add(ext1);
                _group.Content.Children.Add(ext2);
            }
        }

        /// <summary>
        /// Rendert maatlijn met dynamische schaling (tekst blijft uniform ongeacht zoom).
        /// Voor gebruik in SvgHelper waar tekst altijd 12px moet zijn.
        /// </summary>
        public string Render(double scale)
        {
            
            

            var sb = new StringBuilder();
            sb.AppendLine("<g>");
    
            double fontPx = 12;
            double lineHeight = 1.5;

            var (x1o, y1o, x2o, y2o, angle) = GetOffsetPoints(fontPx, lineHeight, scale);

            // Bepaal de richting voor marker orientation
            bool isReversed = false;
            if (Mode == DimLineMode.Horizontal && x2o < x1o)
                isReversed = true;
            else if (Mode == DimLineMode.Vertical && y2o < y1o)
                isReversed = true;

            // --- 1. Hoofdmaatlijn ---
            var mainLine = new SvgLine
            {
                X1 = x1o,
                Y1 = y1o,
                X2 = x2o,
                Y2 = y2o,
                Stroke = StrokeColor,
                StrokeWidth = StrokeWidth,
                MarkerStart = isReversed ? MarkerEnd : MarkerStart,
                MarkerEnd = isReversed ? MarkerStart : MarkerEnd,
                VectorEffect = "non-scaling-stroke",
            };
            sb.AppendLine(mainLine.Render());

            // --- 2. Tekst (met scale!) ---
            var (mx, my) = GetMidPoint(fontPx, lineHeight, scale);
            
            double dx = 0;
            double dy = 0;
            
            if (Mode == DimLineMode.Horizontal)
            {
                dy = -fontPx / scale * 0.67;
            }
            else if (Mode == DimLineMode.Vertical)
            {
                dx = -fontPx / scale * 0.67;
            }
            else
            {
                dy = -fontPx / scale * 0.67;
            }

            var text = new SvgText(DisplayValue, x: mx + dx, y: my + dy)
            {
                Anchor = "middle",
                Pts = fontPx,
                Angle = angle,
            };
            // ✅ Gebruik Render(scale) voor de tekst!
            sb.AppendLine(text.Render(scale));

            // --- 3. Extension lines ---
            if (ShowExtensionLines)
            {
                var ext1 = new SvgLine
                {
                    X1 = X1, Y1 = Y1, X2 = x1o, Y2 = y1o,
                    Stroke = StrokeColor, StrokeWidth = 0.5,
                    StrokeDashArray = "2,2", VectorEffect = "non-scaling-stroke"
                };
                var ext2 = new SvgLine
                {
                    X1 = X2, Y1 = Y2, X2 = x2o, Y2 = y2o,
                    Stroke = StrokeColor, StrokeWidth = 0.5,
                    StrokeDashArray = "2,2", VectorEffect = "non-scaling-stroke"
                };
                sb.AppendLine(ext1.Render());
                sb.AppendLine(ext2.Render());
            }

            sb.AppendLine("</g>");

            if (_length == 0) return "";

            return sb.ToString();
        }

        /// <summary>
        /// Rendert maatlijn zonder dynamische schaling (tekst schaalt mee met SVG).
        /// </summary>
        public override string Render()
        {
            _group.Content.Children.Clear();
            double fontPx = 12;
            double lineHeight = 1.5;
            double scale = Scale;

            var (x1o, y1o, x2o, y2o, angle) = GetOffsetPoints(fontPx, lineHeight, scale);

            bool isReversed = false;
            if (Mode == DimLineMode.Horizontal && x2o < x1o)
                isReversed = true;
            else if (Mode == DimLineMode.Vertical && y2o < y1o)
                isReversed = true;

            var mainLine = new SvgLine
            {
                X1 = x1o,
                Y1 = y1o,
                X2 = x2o,
                Y2 = y2o,
                Stroke = StrokeColor,
                StrokeWidth = StrokeWidth,
                MarkerStart = isReversed ? MarkerEnd : MarkerStart,
                MarkerEnd = isReversed ? MarkerStart : MarkerEnd,
                VectorEffect = "non-scaling-stroke",
            };
            _group.Content.Children.Add(mainLine);

            var (mx, my) = GetMidPoint(fontPx, lineHeight, scale);
            
            double dx = 0;
            double dy = 0;
            
            if (Mode == DimLineMode.Horizontal)
            {
                dy = -fontPx / scale * 0.67;
            }
            else if (Mode == DimLineMode.Vertical)
            {
                dx = -fontPx / scale * 0.67; // ✅ Negatief voor links/boven in geroteerde ruimte
            }
            else
            {
                dy = -fontPx / scale * 0.67;
            }

            var text = new SvgText(DisplayValue, x: mx + dx, y: my + dy)
            {
                Anchor = "middle",
                Pts = fontPx,
                Scale = Scale,
                Angle = angle,
            };
            _group.Content.Children.Add(text);

            if (ShowExtensionLines)
            {
                var ext1 = new SvgLine
                {
                    X1 = X1, Y1 = Y1, X2 = x1o, Y2 = y1o,
                    Stroke = StrokeColor, StrokeWidth = 0.5,
                    StrokeDashArray = "2,2", VectorEffect = "non-scaling-stroke"
                };
                var ext2 = new SvgLine
                {
                    X1 = X2, Y1 = Y2, X2 = x2o, Y2 = y2o,
                    Stroke = StrokeColor, StrokeWidth = 0.5,
                    StrokeDashArray = "2,2", VectorEffect = "non-scaling-stroke"
                };
                _group.Content.Children.Add(ext1);
                _group.Content.Children.Add(ext2);
            }

            return _group.Render();
        }

        public override BoundingBox GetBoundingBox()
        {
            var bb = new BoundingBox();

            // Originele meetpunten (aanhechtingspunten van de hulplijnen)
            bb.Add(X1, Y1);
            bb.Add(X2, Y2);

            // Offsetpunten (waar de eigenlijke maatlijn ligt)
            var (x1o, y1o, x2o, y2o, _) = GetOffsetPoints(12, 1.5, Scale);
            bb.Add(x1o, y1o);
            bb.Add(x2o, y2o);

            return bb;
        }
    }


   
}


