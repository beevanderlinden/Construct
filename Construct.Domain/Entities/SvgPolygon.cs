namespace Construct.Domain.Entities
{
    public class SvgPolygon : BaseSvg
    {
        public SvgPolygon() { }

        /// <summary>
        /// Maakt een <polygon></polygon>
        /// </summary>
        /// <param name="points">Comma-separated lijst van x,y coördinaten</param>
        public SvgPolygon(string points)
        {
            Points = points;
        }

        protected override string TagName => "polygon";

        public override BoundingBox GetBoundingBox()
        {
            // Simpele bounding box implementatie (kan later verfijnd worden)
            return new BoundingBox() { MinX = 0, MinY = 0, MaxXValue = 0, MaxYValue = 0 };
        }

        protected override void ApplyAttributes()
        {
            Set("points", Points);
            Set("id", Id);
            Set("class", CssClass);
            Set("transform", Transform);
            Set("opacity", Opacity);
            Set("stroke", Stroke);
            Set("stroke-width", StrokeWidth);
            Set("fill", Fill);
            Set("style", Style);
            Set("vector-effect", VectorEffect);
        }

        /// <summary>points attribuut: lijst van x,y coördinaten gescheiden door spaties of komma's</summary>
        public string Points { get; set; } = string.Empty;

        /// <summary>vulling (fill). null of leeg betekent geen fill-attribuut.</summary>
        public string? Fill { get; set; }

        /// <summary>lijnkleur (stroke). null of leeg betekent geen stroke-attribuut.</summary>
        public string? Stroke { get; set; }

        /// <summary>stroke-width attribuut. null betekent geen attribuut.</summary>
        public double? StrokeWidth { get; set; }

        /// <summary>opacity voor de gehele polygon (0..1). null betekent geen attribuut.</summary>
        public double? Opacity { get; set; }

        /// <summary>extra class-naam(en) om toe te voegen aan class attribuut</summary>
        public string? CssClass { get; set; }

        /// <summary>style string (gaat vóór inline style attribuut als aanwezig)</summary>
        public string? Style { get; set; }

        /// <summary>id attribuut</summary>
        public new string? Id { get; set; }

        /// <summary>transform attribuut (bv. "translate(10,20) rotate(30)")</summary>
        public string? Transform { get; set; }

        public string? VectorEffect { get; set; } = "non-scaling-stroke";
    }
}
