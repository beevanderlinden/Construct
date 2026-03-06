using System.Text;

namespace Construct.Domain.Entities
{
    public class SvgDefs : BaseSvg
    {
        public List<BaseSvg> Children { get; } = [];

        protected override string TagName => "defs";

        public SvgDefs() { }

        public SvgDefs(IEnumerable<BaseSvg> children)
        {
            Children.AddRange(children);
        }

        public void Add(BaseSvg child)
        {
            Children.Add(child);
        }

        public void AddRange(IEnumerable<BaseSvg> children)
        {
            Children.AddRange(children);
        }

        protected override void ApplyAttributes()
        {
            // <defs> heeft normaal gesproken geen attributes
            // maar we laten dit staan zodat je eventueel toch attributes kunt toevoegen
        }

        public override string Render()
        {
            ApplyAttributes();

            var sb = new StringBuilder();
            sb.Append($"<{TagName}");

            // Attributes uit BaseSvg
            foreach (var kvp in Attributes)
                sb.Append($" {kvp.Key}=\"{kvp.Value}\"");

            sb.Append(">");

            // Render alle definities
            foreach (var child in Children)
                sb.Append(child.Render());

            sb.Append($"</{TagName}>");

            return sb.ToString();
        }

        public override BoundingBox GetBoundingBox()
        {
            throw new NotImplementedException();
        }
    }



}
