namespace Construct.Domain.Entities
{
    public class SvgRect : BaseSvg
    {
        public SvgRect() { }

        public SvgRect(double x, double y, double width, double height)
        {
            X = x; Y = y; Width = width; Height = height;
        }

        protected override string TagName => "rect";

        public override BoundingBox GetBoundingBox()
        {
            return new BoundingBox() { MinX = X, MinY = Y, MaxXValue = X + Width, MaxYValue = Y + Height };
        }

        protected override void ApplyAttributes()
        {
            Set("x", X);
            Set("y", Y);
            Set("width", Width);
            Set("height", Height);
            Set("fill", Fill);
            Set("stroke", Stroke);
            Set("stroke-width", StrokeWidth);
            Set("opacity", Opacity);
            Set("transform", Transform);
            Set("class", CssClass);
            Set("style", Style);
            Set("vector-effect", VectorEffect);
        }

        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public string? Fill { get; set; }
        public string? Stroke { get; set; }
        public double? StrokeWidth { get; set; }
        public double? Opacity { get; set; }
        public string? CssClass { get; set; }
        public string? Style { get; set; }
        public string? Transform { get; set; }
        public string? VectorEffect { get; set; } = "non-scaling-stroke";
    }
}
