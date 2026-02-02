using System.Text;

namespace Construct.Application.Interfaces
{

    public interface IFileDownloadService
    {
        Task DownloadAsync(string fileName, string contentType, byte[] data);
        Task DownloadStringAsync(string fileName, string contentType, string content, Encoding? encoding = null);
    }



}
