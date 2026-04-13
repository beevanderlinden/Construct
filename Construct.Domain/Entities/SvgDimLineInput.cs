using System.Globalization;
using System.Text;

namespace Construct.Domain.Entities
{
    /// <summary>
    /// Maatlijn die in plaats van een statische <c>&lt;text&gt;</c> een HTML-input rendert
    /// via SVG <c>&lt;foreignObject&gt;</c>. De waarde-wijziging wordt doorgegeven via
    /// <c>window.svgHelpers.notifyDimInput(input)</c>, die de DotNet-callback aanroept
    /// die via <c>svgHelpers.registerDimRef(svgId, dotNetRef)</c> is geregistreerd.
    /// </summary>
    public class SvgDimLineInput : SvgDimLine
    {
        /// <summary>
        /// Sleutel waarmee de callback in de parent-component wordt geïdentificeerd
        /// (bijv. <c>"Breedte"</c> of <c>"Hoogte"</c>).
        /// </summary>
        public string InputKey { get; set; } = "";

        /// <summary>Breedte van de foreignObject in SVG-eenheden.</summary>
        public double FoWidth { get; set; } = 72;

        /// <summary>Hoogte van de foreignObject in SVG-eenheden.</summary>
        public double FoHeight { get; set; } = 18;

        /// <summary>Minimale waarde voor het input-element (<c>min</c> attribuut). Null = geen minimum.</summary>
        public double? Min { get; set; }

        /// <summary>Maximale waarde voor het input-element (<c>max</c> attribuut). Null = geen maximum.</summary>
        public double? Max { get; set; }

        /// <summary>Stapgrootte voor het input-element (<c>step</c> attribuut). Null = standaard (1).</summary>
        public double? Step { get; set; }

        /// <summary>
        /// Rendert de maatlijn (hoofdlijn + hulplijnen) met een
        /// <c>&lt;foreignObject&gt;</c>-input in plaats van een <c>&lt;text&gt;</c>.
        /// </summary>
        public override string Render()
        {
            var sb = new StringBuilder();
            sb.Append("<g>");

            const double fontPx = 12;
            const double lineHeight = 1.5;
            double scale = Scale;

            var (x1o, y1o, x2o, y2o, _) = GetOffsetPoints(fontPx, lineHeight, scale);

            bool isReversed = (Mode == DimLineMode.Horizontal && x2o < x1o)
                           || (Mode == DimLineMode.Vertical   && y2o < y1o);

            string msId = isReversed ? (MarkerEnd ?? "") : (MarkerStart ?? "");
            string meId = isReversed ? (MarkerStart ?? "") : (MarkerEnd ?? "");

            // 1. Hoofdmaatlijn (identiek aan SvgDimLine)
            sb.Append($"<line x1=\"{F(x1o)}\" y1=\"{F(y1o)}\" x2=\"{F(x2o)}\" y2=\"{F(y2o)}\"" +
                      $" stroke=\"{StrokeColor}\" stroke-width=\"{StrokeWidth}\"" +
                      $" vector-effect=\"non-scaling-stroke\"");
            if (!string.IsNullOrEmpty(msId)) sb.Append($" marker-start=\"url(#{msId})\"");
            if (!string.IsNullOrEmpty(meId)) sb.Append($" marker-end=\"url(#{meId})\"");
            sb.Append(" />");

            // 2. foreignObject met <input> op de positie van de originele tekst
            var (mx, my) = GetMidPoint(fontPx, lineHeight, scale);

            double foX, foY;
            if (Mode == DimLineMode.Vertical)
            {
                // Verticale lijn: input horizontaal gecentreerd op de dimline-x, verticaal gecentreerd
                foX = x1o - FoWidth / 2;
                foY = my   - FoHeight / 2;
            }
            else
            {
                // Horizontale (of aligned): zelfde positie als de originele tekst
                double dy = -fontPx / scale * 0.67;
                foX = mx - FoWidth / 2;
                foY = my + dy - FoHeight / 2;
            }

            // Inline CSS: consistent met de CSS-variabelen die ook in BetonBalkDoorsnedeEditor gebruikt worden
            const string inputCss =
                "width:100%;height:100%;font-size:12px;font-weight:600;text-align:center;" +
                "border:1px solid var(--accent-stroke-rest,#0078d4);border-radius:3px;" +
                "background:var(--neutral-layer-2,#f5f5f5);color:var(--neutral-foreground-rest,#111);" +
                "padding:0 2px;box-sizing:border-box;outline:none;";

            sb.Append($"<foreignObject x=\"{F(foX)}\" y=\"{F(foY)}\"" +
                      $" width=\"{F(FoWidth)}\" height=\"{F(FoHeight)}\">");
            sb.Append("<div xmlns=\"http://www.w3.org/1999/xhtml\"" +
                      " style=\"width:100%;height:100%;display:flex;align-items:center;\">");
            sb.Append($"<input type=\"number\"" +
                      $" value=\"{DisplayValue}\"" +
                      $" data-dim-key=\"{InputKey}\"" +
                      (Min  is not null ? $" min=\"{F(Min.Value)}\"" : "") +
                      (Max  is not null ? $" max=\"{F(Max.Value)}\"" : "") +
                      (Step is not null ? $" step=\"{F(Step.Value)}\"" : "") +
                      $" style=\"{inputCss}\"" +
                      $" onchange=\"window.svgHelpers?.notifyDimInput(this)\" />");
            sb.Append("</div>");
            sb.Append("</foreignObject>");

            // 3. Hulplijnen (identiek aan SvgDimLine)
            if (ShowExtensionLines)
            {
                sb.Append($"<line x1=\"{F(X1)}\" y1=\"{F(Y1)}\" x2=\"{F(x1o)}\" y2=\"{F(y1o)}\"" +
                          $" stroke=\"{StrokeColor}\" stroke-width=\"0.5\"" +
                          $" stroke-dasharray=\"2,2\" vector-effect=\"non-scaling-stroke\" />");
                sb.Append($"<line x1=\"{F(X2)}\" y1=\"{F(Y2)}\" x2=\"{F(x2o)}\" y2=\"{F(y2o)}\"" +
                          $" stroke=\"{StrokeColor}\" stroke-width=\"0.5\"" +
                          $" stroke-dasharray=\"2,2\" vector-effect=\"non-scaling-stroke\" />");
            }

            sb.Append("</g>");
            return sb.ToString();
        }

        private static string F(double v) =>
            v.ToString("F2", CultureInfo.InvariantCulture);
    }
}
