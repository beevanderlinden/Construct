using System.Text;

namespace Construct.Domain.Entities
{
    public class SvgContent
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? MetadataJson { get; set; }

        public List<BaseSvg> Children { get; } = [];

        /// <summary>
        /// True wanneer dit element inner content moet renderen.
        /// </summary>
        public bool HasContent =>
            !string.IsNullOrWhiteSpace(Title) ||
            !string.IsNullOrWhiteSpace(Description) ||
            !string.IsNullOrWhiteSpace(MetadataJson) ||
            Children.Count > 0;

        /// <summary>
        /// Rendert alle inner content: title, description, metadata en children.
        /// </summary>
        public string Render()
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(Title))
                sb.Append("<title>").Append(Escape(Title!)).Append("</title>");

            if (!string.IsNullOrWhiteSpace(Description))
                sb.Append("<desc>").Append(Escape(Description!)).Append("</desc>");

            if (!string.IsNullOrWhiteSpace(MetadataJson))
                sb.Append("<metadata>").Append(Escape(MetadataJson!)).Append("</metadata>");

            foreach (var child in Children)
                sb.Append(child.Render());

            return sb.ToString();
        }

        private static string Escape(string value) =>
            value
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
    }

}
