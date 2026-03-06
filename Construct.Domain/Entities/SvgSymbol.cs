using System.Text;

namespace Construct.Domain.Entities
{
    public class SvgSymbol : BaseSvg
    {
        protected override string TagName => "symbol";

        public string? Id { get; set; }
        public double? ViewBoxX { get; set; }
        public double? ViewBoxY { get; set; }
        public double? ViewBoxWidth { get; set; }
        public double? ViewBoxHeight { get; set; }

        public List<BaseSvg> Children { get; } = [];

        protected override void ApplyAttributes()
        {
            Set("id", Id);

            if (ViewBoxWidth is not null && ViewBoxHeight is not null)
            {
                string viewBox = $"{ViewBoxX ?? 0} {ViewBoxY ?? 0} {ViewBoxWidth} {ViewBoxHeight}";
                Set("viewBox", viewBox);
            }
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
