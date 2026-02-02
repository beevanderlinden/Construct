using Construct.Domain.Entities;
using Microsoft.JSInterop;

namespace Construct.Application.Interfaces
{
    public interface IProjectFileService
    {
        Task<List<ProjectFileInfo>> GetAvailableProjectFilesWithInfoAsync();

        Task<List<ProjectFileInfo>> GetProjectFilesFromUserDirectoryAsync(IJSRuntime jsRuntime);

        //Task<List<ProjectFileInfo>> GetProjectFileInfosFromDriveItem(DriveItem driveItem);



        Task<ProjectFileInfo> GetProjectFileInfo(string? webUrl);
        //Task<ProjectFileInfo> GetProjectFileInfoByDriveItem(DriveItem driveItem);
        Task<ProjectInfoEntity?> LeesProjectInfoUitBytesAsync(byte[] data);

        Task AutoSave(IJSRuntime jsRuntime, string fileName, string base64);
        Task SaveAs(IJSRuntime jsRuntime, string fileName, string base64);
        Task AutoSave(IJSRuntime jsRuntime, ProjectFileInfo projectFileInfo);
    }


}