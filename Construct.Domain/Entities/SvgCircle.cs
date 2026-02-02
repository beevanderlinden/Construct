namespace Construct.Domain.Entities
{
    public class SvgCircle : BaseSvg
    {
        public SvgCircle() { }

        /// <summary>
        /// Maakt een <circle></circle>
        /// </summary>
        /// <param name="cx">middelpunt x</param>
        /// <param name="cy">middelpunt y</param>
        /// <param name="r">straal</param>
        public SvgCircle(double cx, double cy, double r)
        {
            Cx = cx;
            Cy = cy;
            R = r;
        }


        protected override string TagName => "circle";

        public override BoundingBox GetBoundingBox()
        {
            return new BoundingBox() { MinX = Cx - R, MinY = Cy - R, MaxXValue = Cx + R, MaxYValue = Cy + R };
        }

        protected override void ApplyAttributes()
        {
            Set("cx", Cx);
            Set("cy", Cy);
            Set("r", R);
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

        /// <summary>cx attribuut (x-coordinaat middelpunt)</summary>
        public double Cx { get; set; }


        /// <summary>cy attribuut (y-coordinaat middelpunt)</summary>
        public double Cy { get; set; }


        /// <summary>radius</summary>
        public double R { get; set; }


        /// <summary>vulling (fill). null of leeg betekent geen fill-attribuut.</summary>
        public string? Fill { get; set; }


        /// <summary>lijnkleur (stroke). null of leeg betekent geen stroke-attribuut.</summary>
        public string? Stroke { get; set; }


        /// <summary>stroke-width attribuut. null betekent geen attribuut.</summary>
        public double? StrokeWidth { get; set; }


        /// <summary>opacity voor de gehele cirkel (0..1). null betekent geen attribuut.</summary>
        public double? Opacity { get; set; }


        /// <summary>extra class-naam(en) om toe te voegen aan class attribuut</summary>
        public string? CssClass { get; set; }


        /// <summary>style string (gaat vóór inline style attribuut als aanwezig)</summary>
        public string? Style { get; set; }


        /// <summary>id attribuut</summary>
        public string? Id { get; set; }


        /// <summary>transform attribuut (bv. "translate(10,20) rotate(30)")</summary>
        public string? Transform { get; set; }

        public string? VectorEffect { get; set; } = "non-scaling-stroke";




        // RENDER via BASE


    }

}
