using Construct.Application.Interfaces;
using Construct.Domain.Entities;

namespace Construct.Application.Services
{
    public class ExportService
    {
        private readonly ISvgToBitmapService _svgToBitmapService;

        public ExportService(ISvgToBitmapService svgToBitmapService)
        {
            _svgToBitmapService = svgToBitmapService;
        }

        public async Task<string?> GenereerTrapBitmapAsync(SteekTrapEntity trap, string format = "png")
        {
            string svgXml = TrapSvgGenerator.GenerateTrapSvgXml(trap, new BoundingBox(), 600, 400,
                "position:absolute; right:15mm; top:15mm;");

            return await _svgToBitmapService.ConvertToBase64Async(svgXml, 600, 400, format);
        }
    }
}
