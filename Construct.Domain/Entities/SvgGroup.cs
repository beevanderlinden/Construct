using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Construct.Domain.Entities
{


    public class SvgGroup : BaseSvg
    {
        // Kind-objecten (andere Svg elementen)
        //public List<BaseSvg> Children { get; } = [];

        // Groeps-eigenschappen
        public string? Transform { get; set; } = null;
        public double? Opacity { get; set; } = null;
        public string? Stroke { get; set; } = null;
        public double? StrokeWidth { get; set; } = null;
        public string? Fill { get; set; } = null;

        protected override string TagName => "g";

        public SvgGroup() { }

        public SvgGroup(IEnumerable<BaseSvg> children)
        {
            Content.Children.AddRange(children);
        }

        public void Add(BaseSvg child)
        {
            Content.Children.Add(child);
        }

        public void AddRange(IEnumerable<BaseSvg> children)
        {
            Content.Children.AddRange(children);
        }

        protected override void ApplyAttributes()
        {
            // Groep-attributes worden automatisch naar de XML gezet
            Set("transform", Transform);
            Set("opacity", Opacity);
            Set("stroke", Stroke);
            Set("stroke-width", StrokeWidth);
            Set("fill", Fill);
        }

        //public override string Render()
        //{
        //    ApplyAttributes();

        //    var sb = new StringBuilder();

        //    sb.Append($"<{TagName}");

        //    // Attributes uit BaseSvg
        //    foreach (var kvp in Attributes)
        //        sb.Append($" {kvp.Key}=\"{kvp.Value}\"");

        //    sb.Append(">");

        //    // Render alle children
        //    foreach (var child in Children)
        //        sb.Append(child.Render());

        //    sb.Append($"</{TagName}>");

        //    return sb.ToString();
        //}

        public override BoundingBox GetBoundingBox()
        {
            return new BoundingBox() { };
            throw new NotImplementedException();
        }
    }

}
