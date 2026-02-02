namespace Construct.Domain.Extensions
{
    using System.Globalization;

    public static class SvgExtensions
    {
        /// <summary>
        /// Format double voor SVG: altijd punt als decimaal, optioneel format.
        /// </summary>
        public static string ToSvg(this double value, string format = "0.###") =>
            value.ToString(format, CultureInfo.InvariantCulture);
    }

}
