using Construct.Application.GraphModels;
using Construct.Application.Interfaces;
using Construct.Domain.Entities;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;

namespace Construct.Application.Services
{
    public class GraphDriveService : IGraphDriveService
    {
        private readonly GraphServiceClient _graphClient;
        private readonly IProjectFileService _projectFileService;

        public GraphDriveService(GraphServiceClient graphClient, IProjectFileService projectFileService)
        {
            _graphClient = graphClient;
            _projectFileService = projectFileService;
        }

        public async Task<List<DriveItem>> LoadDriveItemsAsync(string selectedSource, string? extensionFilter = null, int maxDepth = 8)
        {
            var items = new List<DriveItem>();

            try
            {
                // Startpad bepalen
                var basePath = selectedSource switch
                {
                    "mydrive" => "/me/drive/root",
                    "appfolder" => "/me/drive/special/approot",
                    "shared" => "/me/drive/sharedWithMe", // wordt niet recursief
                    _ => throw new InvalidOperationException("Ongeldige bron")
                };




                if (selectedSource == "shared")
                {
                    var sharedRequestInfo = new RequestInformation
                    {
                        HttpMethod = Method.GET,
                        UrlTemplate = "{+baseurl}" + basePath,
                        PathParameters = new Dictionary<string, object> { { "baseurl", "https://graph.microsoft.com/v1.0" } }
                    };

                    var response = await _graphClient
                        .RequestAdapter
                        .SendAsync<DriveItemCollectionResponse>(
                            sharedRequestInfo,
                            DriveItemCollectionResponse.CreateFromDiscriminatorValue,
                            null,
                            CancellationToken.None
                        );

                    if (response?.Value != null)
                    {
                        items.AddRange(FilterByExtension(response.Value, extensionFilter));
                    }

                    return items;
                }

                await LoadRecursiveAsync(basePath, items, extensionFilter, maxDepth, 0);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Fout bij ophalen items: " + ex.Message);
            }

            return items;
        }

        private async Task LoadRecursiveAsync(string path, List<DriveItem> items, string? extensionFilter, int maxDepth, int currentDepth)
        {
            if (currentDepth > maxDepth) return;

            var requestInfo = new RequestInformation
            {
                HttpMethod = Method.GET,
                UrlTemplate = "{+baseurl}" + path + "/children",
                PathParameters = new Dictionary<string, object> { { "baseurl", "https://graph.microsoft.com/v1.0" } }
            };

            var response = await _graphClient
                .RequestAdapter
                .SendAsync<DriveItemCollectionResponse>(
                    requestInfo,
                    DriveItemCollectionResponse.CreateFromDiscriminatorValue,
                    null,
                    CancellationToken.None
                );

            if (response?.Value == null) return;

            foreach (var item in response.Value)
            {
                if (item.File != null)
                {
                    if (string.IsNullOrEmpty(extensionFilter) || item.Name.EndsWith(extensionFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        items.Add(item);
                    }
                }
                else if (item.Folder != null)
                {
                    // ✅ Universeel pad voor submap: werkt voor root, approot, gewone mappen
                    var subfolderPath = $"/me/drive/items/{item.Id}";

                    await LoadRecursiveAsync(subfolderPath, items, extensionFilter, maxDepth, currentDepth + 1);
                }
            }
        }

        private static IEnumerable<DriveItem> FilterByExtension(IEnumerable<DriveItem> items, string? extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return items;
            return items.Where(i => i.File != null && i.Name.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
        }

        private async Task TraverseFolderAsync(string folderPath, List<DriveItem> collected, string extensionFilter, int maxDepth, int currentDepth)
        {
            if (currentDepth > maxDepth)
                return;

            var requestInfo = new RequestInformation
            {
                HttpMethod = Method.GET,
                UrlTemplate = "{+baseurl}" + folderPath + "/children",
                PathParameters = new Dictionary<string, object>
            {
                { "baseurl", "https://graph.microsoft.com/v1.0" }
            }
            };

            var response = await _graphClient.RequestAdapter.SendAsync<DriveItemCollectionResponse>(
                requestInfo,
                DriveItemCollectionResponse.CreateFromDiscriminatorValue,
                null,
                CancellationToken.None
            );

            if (response?.Value == null) return;

            foreach (var item in response.Value)
            {
                // Bestand met juiste extensie toevoegen
                if (item.File != null)
                {
                    if (extensionFilter == null || item.Name.EndsWith(extensionFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        collected.Add(item);
                    }
                }
                // Map: recursief verkennen
                else if (item.Folder != null && item.Name != null)
                {
                    var subPath = $"{folderPath}/items/{item.Id}";
                    await TraverseFolderAsync(subPath, collected, extensionFilter, maxDepth, currentDepth + 1);
                }
            }
        }

        private async Task LoadRecursiveTreeAsync(string path, List<DriveItemNode> parentList, string? extensionFilter, int maxDepth, int currentDepth, string currentFolderPath)
        {
            if (currentDepth > maxDepth) return;

            var requestInfo = new RequestInformation
            {
                HttpMethod = Method.GET,
                UrlTemplate = "{+baseurl}" + path + "/children",
                PathParameters = new Dictionary<string, object>
        {
            { "baseurl", "https://graph.microsoft.com/v1.0" }
        }
            };

            var response = await _graphClient.RequestAdapter.SendAsync<DriveItemCollectionResponse>(
                requestInfo,
                DriveItemCollectionResponse.CreateFromDiscriminatorValue,
                null,
                CancellationToken.None
            );

            if (response?.Value == null) return;

            foreach (var item in response.Value)
            {
                var fullPath = string.IsNullOrEmpty(currentFolderPath)
                    ? item.Name
                    : currentFolderPath + "/" + item.Name;

                if (item.File != null)
                {
                    if (string.IsNullOrEmpty(extensionFilter) || item.Name.EndsWith(extensionFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        parentList.Add(new DriveItemNode
                        {
                            Item = item,
                            Path = fullPath,
                            WebUrl = item.WebUrl,
                        });
                    }
                }
                else if (item.Folder != null)
                {
                    var folderNode = new DriveItemNode
                    {
                        Item = item,
                        Path = fullPath,
                        WebUrl = item.WebUrl,
                        Children = new()
                    };

                    parentList.Add(folderNode);

                    var subPath = $"/me/drive/items/{item.Id}";
                    await LoadRecursiveTreeAsync(subPath, folderNode.Children, extensionFilter, maxDepth, currentDepth + 1, fullPath);
                }
            }
        }



        public async Task<List<DriveItemNode>> LoadDriveItemTreeAsync(string selectedSource, string? extensionFilter = null, int maxDepth = 8)
        {
            var result = new List<DriveItemNode>();

            var basePath = selectedSource switch
            {
                "mydrive" => "/me/drive/root",
                "appfolder" => "/me/drive/special/approot",
                "shared" => "/me/drive/sharedWithMe", // alleen root mogelijk
                _ => throw new InvalidOperationException("Ongeldige bron")
            };

            if (selectedSource == "shared")
            {
                // enkel flat
                var requestInfo = new RequestInformation
                {
                    HttpMethod = Method.GET,
                    UrlTemplate = "{+baseurl}" + basePath,
                    PathParameters = new Dictionary<string, object>
            {
                { "baseurl", "https://graph.microsoft.com/v1.0" }
            }
                };

                var response = await _graphClient.RequestAdapter.SendAsync<DriveItemCollectionResponse>(
                    requestInfo,
                    DriveItemCollectionResponse.CreateFromDiscriminatorValue,
                    null,
                    CancellationToken.None
                );

                if (response?.Value != null)
                {
                    foreach (var item in response.Value)
                    {
                        if (item.File != null &&
                            (string.IsNullOrEmpty(extensionFilter) || item.Name.EndsWith(extensionFilter, StringComparison.OrdinalIgnoreCase)))
                        {
                            result.Add(new DriveItemNode
                            {
                                Item = item,
                                Path = item.Name,
                                WebUrl = item.WebUrl,
                            });
                        }
                    }
                }

                return result;
            }

            // root map ophalen
            await LoadRecursiveTreeAsync(basePath, result, extensionFilter, maxDepth, 0, "");
            return result;
        }

        public Task<List<DriveItemNode>> GetRootNodesAsyc(string selectedSource)
        {
            return LoadDriveItemTreeAsync(selectedSource, maxDepth: 1);
        }

        public async Task<List<DriveItemNode>> GetChildrenAsync(string parentId, string? extensionFilter = null, int maxDepth = 8)
        {
            var result = new List<DriveItemNode>();
            var path = $"/me/drive/items/{parentId}";
            await LoadRecursiveTreeAsync(path, result, extensionFilter, maxDepth, 0, "");
            return result;
        }

        public async Task<ProjectFileInfo> GetProjectFileInfoByDriveItem(DriveItem driveItem)
        {
            using var stream = await _graphClient
            .Drives[driveItem.ParentReference.DriveId]
            .Items[driveItem.Id]
            .Content
            .GetAsync();

            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            // 👇 Probeer de inhoud te lezen als ProjectEntity

            var info = await _projectFileService.LeesProjectInfoUitBytesAsync(bytes);

            return info == null
                ? throw new InvalidOperationException("Bestand kon niet worden gelezen als geldig project")
                : new ProjectFileInfo
                {
                    FilePath = driveItem.WebUrl ?? string.Empty,
                    Info = info,
                    Base64 = Convert.ToBase64String(bytes),
                    LastModified = driveItem.LastModifiedDateTime?.ToLocalTime().DateTime ?? DateTime.MinValue,
                    DriveId = driveItem.ParentReference?.DriveId ?? string.Empty,
                    ItemId = driveItem.Id ?? string.Empty,
                    Size = driveItem.Size,
                    SizeInKB = (double)(driveItem.Size ?? 0) / 1000
                };
        }

        public async Task<List<ProjectFileInfo>> GetProjectFileInfosByParentFolder(DriveItem folder)
        {
            var result = new List<ProjectFileInfo>();
            var children = await _graphClient
                .Drives[folder.ParentReference.DriveId]
                .Items[folder.Id]
                .Children.GetAsync();

            if (children?.Value is null || children.Value.Count == 0)
            {
                return result;
            }

            // 2. Filter op .cprj bestanden
            var cprjFiles = children.Value
                .Where(item => item.File != null && item.Name != null && item.Name.EndsWith(".cprj", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var driveItem in cprjFiles)
            {
                try
                {
                    var projectInfo = await GetProjectFileInfoByDriveItem(driveItem);
                    result.Add(projectInfo);
                }
                catch (Exception ex)
                {
                    // Je kunt hier loggen of negeren als je wilt dat één fout het geheel niet breekt
                    Console.WriteLine($"Fout bij verwerken van {driveItem.Name}: {ex.Message}");
                }
            }

            return result;


        }

        public async Task AutoSave(ProjectFileInfo projectFileInfo, byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);

            await _graphClient
                .Drives[projectFileInfo.DriveId]
                .Items[projectFileInfo.ItemId]
                .Content
                .PutAsync(stream); // overschrijft 
        }


    }
}
