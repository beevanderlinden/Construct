namespace Construct.Domain.Entities
{
    public class SvgLine : BaseSvg
    {
        // Geometrie
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }


       
       

        // Opmaak
        public string Stroke { get; set; } = "var(--neutral-foreground-rest)";
        public double StrokeWidth { get; set; } = 1.0;
        public string StrokeDashArray { get; set; } = "";
        public string StrokeLineCap { get; set; } = "round"; // butt, round, square
        public string VectorEffect { get; set; } = "non-scaling-stroke";

        // markers
        public string? MarkerStart { get; set; }
        public string? MarkerEnd { get; set; }

        protected override string TagName => "line";

        public override BoundingBox GetBoundingBox()
        {
            return new BoundingBox() { MinX = Math.Min(X1, X2), MaxXValue = Math.Max(X1, X2), MinY = Math.Min(Y1, Y2), MaxYValue = Math.Max(Y1,Y2) };
        }

        protected override void ApplyAttributes()
        {
            base.ApplyAttributes(); // id, class, cursor en data

            // Puntcoördinaten
            Set("x1", X1);
            Set("y1", Y1);
            Set("x2", X2);
            Set("y2", Y2);

            // Opmaak
            Set("stroke", Stroke);
            Set("stroke-width", StrokeWidth);
            Set("stroke-dasharray", StrokeDashArray);
            Set("stroke-linecap", StrokeLineCap);
            Set("vector-effect", VectorEffect);

            // Markers (done clean!)
            Set("marker-start", "url(#{0})", MarkerStart);
            Set("marker-end", "url(#{0})", MarkerEnd);
        }


       

    }


   


}
