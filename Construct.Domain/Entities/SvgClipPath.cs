using System.Text;

namespace Construct.Domain.Entities
{
    public class SvgClipPath : BaseSvg
    {
        protected override string TagName => "clipPath";

        public new string? Id { get; set; }

        public List<BaseSvg> Children { get; } = [];

        protected override void ApplyAttributes()
        {
            Set("id", Id);
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
