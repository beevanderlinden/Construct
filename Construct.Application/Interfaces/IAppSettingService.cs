namespace Construct.Application.Interfaces
{
    public interface IAppSettingService
    {
        Task<string?> GetDefaultSavePathAsync();
        Task SetDefaultSavePathAsync(string path);
        Task<List<string>> GetAvailableProjectFilesAsync();

    }

}
