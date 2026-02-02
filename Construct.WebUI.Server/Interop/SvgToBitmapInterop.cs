using Construct.Application.Interfaces;
using Microsoft.JSInterop;

namespace Construct.WebUI.Server.Interop
{
    /// <summary>
    /// Implementatie van ISvgToBitmapService met JSInterop
    /// </summary>
    public class SvgToBitmapJsService : ISvgToBitmapService
    {
        private readonly IJSRuntime _js;

        public SvgToBitmapJsService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task<string?> ConvertToBase64Async(string svgXml, double widthPx, double heightPx, string format = "png")
        {
            if (format.Equals("png", StringComparison.CurrentCultureIgnoreCase))
            {
                try
                {
                    return await _js.InvokeAsync<string?>(
                        "svgHelpers.svgToBase64Png",
                        svgXml, widthPx
                        );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SVG→Bitmap conversie mislukt: {ex.Message}");
                    return null;
                }
            }
            if (format.Equals("wmf", StringComparison.CurrentCultureIgnoreCase))
            {
                try
                {
                    return await _js.InvokeAsync<string?>(
                        "svgHelpers.svgToBase64Jpeg",
                        svgXml, widthPx
                        );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SVG→Bitmap conversie mislukt: {ex.Message}");
                    return null;
                }
            }
            else return null;

        }


    }
}
