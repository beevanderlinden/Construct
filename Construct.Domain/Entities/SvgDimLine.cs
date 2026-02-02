using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Razor.TagHelpers;

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

        public string DisplayValue
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Text))
                    return Text;

                double length = Mode switch
                {
                    DimLineMode.Aligned => Math.Sqrt((X2 - X1) * (X2 - X1) + (Y2 - Y1) * (Y2 - Y1)),
                    DimLineMode.Horizontal => Math.Abs(X2 - X1),
                    DimLineMode.Vertical => Math.Abs(Y2 - Y1),
                    _ => 0
                };

                return length.ToString(StringFormat);
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
            double scale = Scale; // neem schaal van buiten
            //double textSize = fontPx / scale;

            var (x1o, y1o, x2o, y2o, angle) = GetOffsetPoints(fontPx, lineHeight, scale);

            // --- 1. Hoofdmaatlijn ---
            var mainLine = new SvgLine
            {
                X1 = x1o,
                Y1 = y1o,
                X2 = x2o,
                Y2 = y2o,
                Stroke = StrokeColor,
                StrokeWidth = StrokeWidth,
                MarkerStart = MarkerStart,
                MarkerEnd = MarkerEnd,
                VectorEffect = "non-scaling-stroke",
                
            };

            _group.Content.Children.Add(mainLine);


            // --- 2. Tekst ---
            var (mx, my) = GetMidPoint(fontPx, lineHeight, scale);
            var dy = -fontPx / scale * 0.67;

            var text = new SvgText(DisplayValue, x: mx, y: my + dy)
            {
                Anchor = "middle",
                Pts = fontPx,
                Scale = Scale,
                //FontFamily = "Arial",
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

        public override string Render()
        {
            Build();
            return _group.Render();
        }

        public override BoundingBox GetBoundingBox()
        {
            return new BoundingBox() { };
            throw new NotImplementedException();
        }
    }


    public class SvgDimLineBAK
    {
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }

        private double _value;
        public double Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    ValueChanged.InvokeAsync(value);
                }
            }
        }

        public EventCallback<double> ValueChanged { get; set; }


        // Optionele tekst
        public string? Text { get; set; }

        public double StrokeWidth { get; set; } = 1;
        public string StrokeColor { get; set; } = "black";
        public double Offset = 0;                // afstand van maatlijn tot objectlijn



        // Tekststijl info voor omzetting naar units
        public double FontSizePx { get; set; } = 12;
        public double LineHeight { get; set; } = 1.5;

        public int OffsetLines { get; set; } = 0;
        public double DimScale { get; set; } = 1.0;




        public bool ShowExtensionLines = true;    // hulplijnen aan/uit
        public DimLineMode Mode = DimLineMode.Aligned;


        // Berekende properties voor het component

        private double TextLineSpaceUnits =>
       (FontSizePx / DimScale) * LineHeight;

        private double OffsetTotal =>
            Offset + OffsetLines * TextLineSpaceUnits;



        public double MidX => (X1o + X2o) / 2;
        public double MidY => (Y1o + Y2o) / 2;
        public double Angle
        {
            get
            {
                return Mode switch
                {
                    DimLineMode.Aligned =>
                        Math.Atan2(Y2 - Y1, X2 - X1) * 180.0 / Math.PI,
                    DimLineMode.Horizontal => 0.0,
                    DimLineMode.Vertical => -90.0,
                    _ => 0.0
                };
            }
        }

        private double dx => X2 - X1;
        private double dy => Y2 - Y1;
        private double len => Math.Sqrt(dx * dx + dy * dy);
        private double nx => -dy / len;  // normaal vector (unit)
        private double ny => dx / len;



        //public double X1o => X1 + nx * Offset;
        //public double Y1o => Y1 + ny * Offset;
        //public double X2o => X2 + nx * Offset;
        //public double Y2o => Y2 + ny * Offset;


        // Offset-punten (hangen af van Mode)
        public double X1o
        {
            get
            {
                return Mode switch
                {
                    DimLineMode.Aligned => X1 + nx * OffsetTotal,
                    DimLineMode.Horizontal => X1,
                    DimLineMode.Vertical => Math.Min(X1, X2) - OffsetTotal,
                    _ => X1
                };
            }
        }

        public double Y1o
        {
            get
            {
                return Mode switch
                {
                    DimLineMode.Aligned => Y1 + ny * OffsetTotal,
                    DimLineMode.Horizontal => Math.Min(Y1, Y2) - OffsetTotal,
                    DimLineMode.Vertical => Y1,
                    _ => Y1
                };
            }
        }

        public double X2o
        {
            get
            {
                return Mode switch
                {
                    DimLineMode.Aligned => X2 + nx * OffsetTotal,
                    DimLineMode.Horizontal => X2,
                    DimLineMode.Vertical => Math.Min(X1, X2) - OffsetTotal,
                    _ => X2
                };
            }
        }

        public double Y2o
        {
            get
            {
                return Mode switch
                {
                    DimLineMode.Aligned => Y2 + ny * OffsetTotal,
                    DimLineMode.Horizontal => Math.Min(Y1, Y2) - OffsetTotal,
                    DimLineMode.Vertical => Y2,
                    _ => Y2
                };
            }
        }

        public string DisplayValue
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Text))
                    return Text;

                double length = Mode switch
                {
                    DimLineMode.Aligned => Math.Sqrt((X2 - X1) * (X2 - X1) + (Y2 - Y1) * (Y2 - Y1)),
                    DimLineMode.Horizontal => Math.Abs(X2 - X1),
                    DimLineMode.Vertical => Math.Abs(Y2 - Y1),
                    _ => 0
                };

                return length.ToString("0.##"); // 2 decimalen
            }
        }



    }
}


