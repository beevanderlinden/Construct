namespace Construct.Domain.Extensions

{
    //using Svg;
    using System.Text;
    using static Construct.Domain.Entities.SvgHelper;

    public static class SvgBuilderExtensions
    {
        public static void AppendSvgHeader(
        this StringBuilder sb,
        SvgDocumentInfo? info,
        SvgViewBox viewBox,
        double? width = null,
        double? height = null,
        string style = "",
        string status = "")
        {
            // Begin <svg ...> tag dynamisch opbouwen
            sb.Append($@"<svg xmlns=""http://www.w3.org/2000/svg"" 
                xmlns:inkscape=""http://www.inkscape.org/namespaces/inkscape"" 
                id=""{info?.Id}""
                preserveAspectRatio=""xMidYMid meet"" 
                viewBox=""{viewBox}""");

            if (!string.IsNullOrWhiteSpace(style))
                sb.Append($@" style=""{style}""");

            if (!string.IsNullOrWhiteSpace(status))
                sb.Append($@" class=""{status}""");

            if (width.HasValue)
                sb.Append($@" width=""{width.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}mm""");

            if (height.HasValue)
                sb.Append($@" height=""{height.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}mm""");

            sb.AppendLine(">");

            // Titel, beschrijving, metadata
            if (info != null)
            {
                if (!string.IsNullOrWhiteSpace(info.Title))
                    sb.AppendLine($"  <title>{System.Security.SecurityElement.Escape(info.Title)}</title>");

                if (!string.IsNullOrWhiteSpace(info.Description))
                    sb.AppendLine($"  <desc>{System.Security.SecurityElement.Escape(info.Description)}</desc>");

                var metaJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    info.Author,
                    info.Version,
                    info.Generated
                });
                sb.AppendLine($"  <metadata>{metaJson}</metadata>");
            }
        }


        public static void AppendSvgFooter(this StringBuilder sb)
        {
            sb.AppendLine("</svg>");
        }
    }

}
