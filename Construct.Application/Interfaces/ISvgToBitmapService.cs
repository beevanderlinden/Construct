namespace Construct.Application.Interfaces
{
    public interface ISvgToBitmapService
    {
        Task<string?> ConvertToBase64Async(string svgXml, double widthPx, double heightPx, string format);
    }
}
