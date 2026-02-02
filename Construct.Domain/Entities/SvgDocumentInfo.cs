namespace Construct.Domain.Extensions
{
    public class SvgDocumentInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "Mijn SVG-tekening";
        public string Description { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Author { get; set; } = "Construct.WebUI.Server";
        public string Version { get; set; } = "1.0.0";
        public DateTime Generated { get; set; } = DateTime.Now;
    }

}
