using Construct.Application.Interfaces;
using Construct.Domain.Entities;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.JSInterop;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Construct.Application.Services
{
    public class ProjectFileService : IProjectFileService
    {
        private readonly IAppSettingService _appSettingService;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            Converters = { new JsonStringEnumConverter() },
            WriteIndented = true
        };

        public ProjectFileService(IAppSettingService appSettingService)
        {
            _appSettingService = appSettingService;
        }




        public async Task AutoSave(IJSRuntime jsRuntime, ProjectFileInfo projectFileInfo)
        {
            await jsRuntime.InvokeVoidAsync("dirInterop.autoSaveCprjFile", projectFileInfo.FilePath, projectFileInfo.Base64);
        }

        public async Task AutoSave(IJSRuntime jsRuntime, string fileName, string base64)
        {
            await jsRuntime.InvokeVoidAsync("dirInterop.autoSaveCprjFile", fileName, base64);

        }

        public async Task SaveAs(IJSRuntime jsRuntime, string fileName, string base64)
        {
            await jsRuntime.InvokeVoidAsync("dirInterop.saveCprjFile", fileName, base64);
        }


        public async Task<List<ProjectFileInfo>> GetProjectFilesFromUserDirectoryAsync(IJSRuntime jsRuntime)
        {
            var lijst = new List<ProjectFileInfo>();

            var bestanden = await jsRuntime.InvokeAsync<List<CprjRaw>>("dirInterop.openDirectoryAndReadCprjFiles");

            foreach (var bestand in bestanden)
            {
                try
                {
                    var bytes = Convert.FromBase64String(bestand.Base64);
                    var info = await LeesProjectInfoUitBytesAsync(bytes);
                    if (info != null)
                    {
                        lijst.Add(new ProjectFileInfo
                        {
                            FilePath = bestand.Name,
                            Info = info,
                            Base64 = bestand.Base64,
                            LastModified = bestand.LastModifiedDateTime,
                            SizeInKB = bestand.SizeInKB,


                        });
                    }
                    else
                    {
                        Console.WriteLine($"Geen ProjectFileInfo gevonden in bestand {bestand.Name}, / corrupt bestand overgeslagen.");
                    }
                }
                catch
                {
                    // Optioneel: log corrupt bestand
                }
            }

            return lijst;
        }


        public async Task<ProjectFileInfo> GetProjectFileInfoByDriveItem(GraphServiceClient graphClient, DriveItem driveItem)
        {
            using var stream = await graphClient
            .Drives[driveItem.ParentReference!.DriveId]
            .Items[driveItem.Id]
            .Content
            .GetAsync();

            using var memoryStream = new MemoryStream();
            await (stream ?? throw new InvalidOperationException("Stream kon niet worden geopend.")).CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            // 👇 Probeer de inhoud te lezen als ProjectEntity
            var info = await LeesProjectInfoUitBytesAsync(bytes);

            return info == null
                ? throw new InvalidOperationException("Bestand kon niet worden gelezen als geldig project")
                : new ProjectFileInfo
                {
                    FilePath = driveItem.WebUrl ?? string.Empty,
                    Info = info,
                    Base64 = Convert.ToBase64String(bytes),
                    LastModified = DateTime.UtcNow, // Als je geen exacte hebt
                    SizeInKB = bytes.Length / 1024
                };
        }


        public async Task<ProjectFileInfo> GetProjectFileInfo(string? fileWebUrl)
        {
            using var httpClient = new HttpClient();

            // 👇 Haal bestand binnen als byte-array
            var bytes = await httpClient.GetByteArrayAsync(fileWebUrl);

            // 👇 Probeer de inhoud te lezen als ProjectEntity
            var info = await LeesProjectInfoUitBytesAsync(bytes);

            return info == null
                ? throw new InvalidOperationException("Bestand kon niet worden gelezen als geldig project")
                : new ProjectFileInfo
                {
                    FilePath = fileWebUrl ?? string.Empty,
                    Info = info,
                    Base64 = Convert.ToBase64String(bytes),
                    LastModified = DateTime.UtcNow, // Als je geen exacte hebt
                    SizeInKB = bytes.Length / 1024
                };
        }



        public async Task<List<ProjectFileInfo>> GetAvailableProjectFilesWithInfoAsync()
        {
            var lijst = new List<ProjectFileInfo>();

            var projectDirectory = await _appSettingService.GetDefaultSavePathAsync();


            if (!Directory.Exists(projectDirectory))
                return lijst;

            foreach (var pad in Directory.EnumerateFiles(projectDirectory, "*.cprj"))
            {
                var info = await LeesProjectInfoAsync(pad);
                if (info != null)
                {
                    lijst.Add(new ProjectFileInfo
                    {
                        FilePath = pad,
                        Info = info
                    });
                }
            }

            return lijst;
        }

        private async Task<ProjectInfoEntity?> LeesProjectInfoAsync(string pad)
        {
            try
            {
                var bytes = await File.ReadAllBytesAsync(pad);
                return await LeesProjectInfoUitBytesAsync(bytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fout bij lezen van {pad}: {ex.Message}");
                return null;
            }
        }

        public async Task<ProjectInfoEntity?> LeesProjectInfoUitBytesAsync(byte[] data)
        {
            try
            {
                using var inputStream = new MemoryStream(data);
                using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
                var wrapper = await JsonSerializer.DeserializeAsync<ProjectFileRoot>(gzipStream, _jsonOptions);
                return wrapper?.ProjectInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fout bij lezen van cprj-bytes: {ex.Message}");
                return null;
            }
        }

        public Task<List<ProjectFileInfo>> GetProjectFilesFromUserDirectoryAsync()
        {
            throw new NotImplementedException();
        }
    }

}
