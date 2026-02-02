using Microsoft.JSInterop;
using MigraDoc.DocumentObjectModel;

public class SvgToMigraDocHelper : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;

    public SvgToMigraDocHelper(IJSRuntime js)
    {
        _js = js;
    }

    public async Task InitializeAsync()
    {
        _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "/js/svgExport.js");
    }

    /// <summary>
    /// Zet SVG in PNG om en voegt toe aan MigraDoc Section via een tijdelijk bestand.
    /// </summary>
    public async Task AddSvgToContainerAsync(DocumentObject container, string svgXml, string svgElementId, double widthPt, double heightPt)
    {
        if (_module == null)
            await InitializeAsync();

        // svgElementId moet een <div> in je Blazor page zijn met de SVG string
        var pngBlob = await _module!.InvokeAsync<IJSObjectReference>("svgExport.svgToPng", svgElementId);
        if (pngBlob == null)
            throw new Exception("PNG export from SVG failed.");

        // Blob -> byte[]
        var arrayBuffer = await pngBlob.InvokeAsync<byte[]>("arrayBuffer");

        // Tijdelijk bestand maken
        string tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
        await File.WriteAllBytesAsync(tempFile, arrayBuffer);



        // Toevoegen aan MigraDoc

        if (container is Section sec)
        {
            var img = sec.AddImage(tempFile);
            img.Width = $"{widthPt}pt";
            img.Height = $"{heightPt}pt";
        }
        if (container is Paragraph par)
        {
            var img = par.AddImage(tempFile);
            img.Width = $"{widthPt}pt";
            img.Height = $"{heightPt}pt";
        }




        // Cleanup: verwijder bestand na gebruik (bijv na RTF/PDF render)
        // File.Delete(tempFile); <- niet hier direct!
    }

    public ValueTask DisposeAsync()
    {
        return _module?.DisposeAsync() ?? ValueTask.CompletedTask;
    }
}
