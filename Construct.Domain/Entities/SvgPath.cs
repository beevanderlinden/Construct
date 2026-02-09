namespace Construct.Domain.Entities
{
    using System.Security;
    using System.Xml.Linq;


    public class SvgPath : BaseSvg
    {
        // Path data (verplicht)
        public string D { get; set; } = string.Empty;

        // Kleuren en opmaak
        public string Stroke { get; set; } = "var(--neutral-foreground-rest, black)";
        public double StrokeWidth { get; set; } = 1;
        public string Fill { get; set; } = "none";

        // Extra styling
        public double? Opacity { get; set; } = 1.0;
        public double? FillOpacity { get; set; } = 0.4;
        public string? StrokeDashArray { get; set; } 
        public string? StrokeLineJoin { get; set; } = "round";
        public string? StrokeLineCap { get; set; } = "round";
        public string? FillRule { get; set; } = "nonzero";
        public string VectorEffect { get; set; } = "non-scaling-stroke";

        // Markers
        public string? MarkerStart { get; set; } = null;
        public string? MarkerEnd { get; set; } = null;

        protected override string TagName => "path";

        public override BoundingBox GetBoundingBox()
        {
            return SvgPathBoundingBoxCalculator.GetBoundingBox(D);
        }

        protected override void ApplyAttributes()
        {
            Set("d", D);
            Set("stroke", Stroke);
            Set("stroke-width", StrokeWidth);
            Set("fill", Fill);
            Set("opacity", Opacity);
            Set("fill-opacity", FillOpacity);
            Set("stroke-dasharray", StrokeDashArray);
            Set("stroke-linejoin", StrokeLineJoin);
            Set("stroke-linecap", StrokeLineCap);
            Set("fill-rule", FillRule);
            Set("vector-effect", VectorEffect);

            // Marker URLs
            Set("marker-start", "url(#{0})", MarkerStart);
            Set("marker-end", "url(#{0})", MarkerEnd);
        }
    }



   


}
