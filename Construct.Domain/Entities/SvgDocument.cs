using Construct.Domain.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Construct.Domain.Entities
{
    public class SvgDocument
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public object? Tag { get; set; } = null;
        public string Name { get; set; } = "";
        public double Width { get; set; }
        public double Height { get; set; }

        public double ViewBoxMinX { get; set; } = 0;
        public double ViewBoxMinY { get; set; } = 0;
        public double ViewBoxWidth { get; set; }
        public double ViewBoxHeight { get; set; }

        public SvgViewBox ViewBox { get; set; } = new SvgViewBox();


        public string? Style { get; set; } = null;
        public string? Background { get; set; } = null; // optioneel

        // Jouw paths, texts, lines gaan hier in
        public List<BaseSvg> Children { get; } = [];
        // 🔥 NIEUWE FEATURE: los HTML/SVG fragmenten
        public List<string> RawFragments { get; } = [];

        public SvgDocument(double width, double height)
        {
            Width = width;
            Height = height;
            ViewBoxWidth = width;
            ViewBoxHeight = height;
        }

        public string Render()
        {
            var sb = new StringBuilder();
            sb.Append("<svg ");
            sb.Append($"id='{Id}' ");
            //sb.Append($"<svg width=\"{Width.ToSvg()}\" height=\"{Height.ToSvg()}\" ");
            sb.Append($"viewBox=\"{ViewBoxMinX.ToSvg()} {ViewBoxMinY.ToSvg()} {ViewBoxWidth.ToSvg()} {ViewBoxHeight.ToSvg()}\" ");

            if (!string.IsNullOrWhiteSpace(Style))
                sb.Append($"style=\"{Style}\" ");

            sb.Append("xmlns=\"http://www.w3.org/2000/svg\">");

            if (!string.IsNullOrWhiteSpace(Background))
            {
                sb.Append($"<rect width=\"100%\" height=\"100%\" fill=\"{Background}\" />");
            }

            // Render children
            foreach (var child in Children)
                sb.Append(child.Render());
            // 🔥 Dan alle losse raw SVG strings toevoegen
            foreach (var raw in RawFragments)
                sb.Append(raw);

            sb.Append("</svg>");

            return sb.ToString();
        }
    }

}
