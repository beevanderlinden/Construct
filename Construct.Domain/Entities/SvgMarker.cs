using System.Text;

namespace Construct.Domain.Entities
{
    public class SvgMarker : BaseSvg
    {
        protected override string TagName => "marker";

        public double MarkerWidth { get; set; } = 10;
        public double MarkerHeight { get; set; } = 10;
        public double RefX { get; set; } = 0;
        public double RefY { get; set; } = 0;
        public string Orient { get; set; } = "auto";

        public List<BaseSvg> Children { get; } = [];

        protected override void ApplyAttributes()
        {
            Set("id", Id);
            Set("markerWidth", MarkerWidth);
            Set("markerHeight", MarkerHeight);
            Set("refX", RefX);
            Set("refY", RefY);
            Set("orient", Orient);
        }

        public void Add(BaseSvg child) => Children.Add(child);

        public override string Render()
        {
            ApplyAttributes();

            var sb = new StringBuilder();
            sb.Append($"<{TagName}");

            foreach (var kv in Attributes)
                sb.Append($" {kv.Key}=\"{kv.Value}\"");

            sb.Append('>');

            foreach (var c in Children)
                sb.Append(c.Render());

            sb.Append($"</{TagName}>");
            return sb.ToString();
        }

        public override BoundingBox GetBoundingBox()
        {
            throw new NotImplementedException();
        }
    }


}
