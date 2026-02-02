using Construct.Application.GraphModels;
using Construct.Domain.Entities;
using Microsoft.Graph.Models;

namespace Construct.Application.Interfaces
{
    public interface IGraphDriveService
    {
        Task<List<DriveItemNode>> GetRootNodesAsyc(string selectedSource);
        Task<List<DriveItemNode>> GetChildrenAsync(string parentId, string? extensionFilter = null, int maxDepth = 8);

        Task<List<DriveItem>> LoadDriveItemsAsync(string selectedSource, string? extensionFilter = null, int maxDepth = 8);

        Task<List<DriveItemNode>> LoadDriveItemTreeAsync(string selectedSource, string? extensionFilter = null, int maxDepth = 8);

        Task<ProjectFileInfo> GetProjectFileInfoByDriveItem(DriveItem driveItem);
        Task<List<ProjectFileInfo>> GetProjectFileInfosByParentFolder(DriveItem folder);

        Task AutoSave(ProjectFileInfo projectFileInfo, byte[] bytes);
    }

}
