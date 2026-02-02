using System.Text;

namespace Construct.Domain.Entities
{
    public class SvgPattern : BaseSvg
    {
        protected override string TagName => "pattern";

        public string? Id { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string PatternUnits { get; set; } = "userSpaceOnUse";

        public List<BaseSvg> Children { get; } = new();

        protected override void ApplyAttributes()
        {
            Set("id", Id);
            Set("width", Width);
            Set("height", Height);
            Set("patternUnits", PatternUnits);
        }

        public void Add(BaseSvg child) => Children.Add(child);

        public override string Render()
        {
            ApplyAttributes();

            var sb = new StringBuilder();
            sb.Append($"<{TagName}");

            foreach (var kv in Attributes)
                sb.Append($" {kv.Key}=\"{kv.Value}\"");

            sb.Append(">");

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
