using Construct.Application.Interfaces;
using Microsoft.JSInterop;
using System.Text;

namespace Construct.Application.Services
{
    public class FileDownloadService(IJSRuntime js) : IFileDownloadService
    {
        private readonly IJSRuntime _js = js;

        public async Task DownloadAsync(string fileName, string contentType, byte[] data)
        {
            var base64 = Convert.ToBase64String(data);
            await _js.InvokeVoidAsync("downloadFile", fileName, contentType, base64);
        }

        public async Task DownloadStringAsync(string fileName, string contentType, string content, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;
            var bytes = encoding.GetBytes(content);
            await DownloadAsync(fileName, contentType, bytes);
        }
    }
}
