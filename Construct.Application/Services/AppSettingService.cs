namespace Construct.Application.Services
{
    using Construct.Application.Interfaces;
    using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

    public class AppSettingService : IAppSettingService
    {
        private readonly ProtectedLocalStorage _localStorage;
        private const string DefaultSavePathKey = "DefaultSavePath";

        public AppSettingService(ProtectedLocalStorage localStorage)
        {
            _localStorage = localStorage;
        }



        public async Task<string?> GetDefaultSavePathAsync()
        {
            // debug
            //return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            var result = await _localStorage.GetAsync<string>(DefaultSavePathKey);

            return result.Success ? result.Value : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        public async Task SetDefaultSavePathAsync(string path)
        {
            string fallbackPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MijnApp");

            // Controleer of de opgegeven map bestaat, en probeer schrijfrechten te testen
            try
            {
                if (!Directory.Exists(path))
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Console.WriteLine(ex.Message);
                        path = fallbackPath;
                    }
                }

                // Test schrijfrechten door een tijdelijk bestand te maken
                string testFilePath = Path.Combine(path, "write_test.tmp");
                await File.WriteAllTextAsync(testFilePath, "test");
                File.Delete(testFilePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Log de fout en gebruik de fallback-map
                path = fallbackPath;
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                // Andere fout bij aanmaken of schrijven, gebruik fallback
                path = fallbackPath;
                Console.WriteLine(ex.Message);
            }

            // Zorg dat de fallback directory bestaat
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            await _localStorage.SetAsync(DefaultSavePathKey, path);
        }

        public async Task<List<string>> GetAvailableProjectFilesAsync()
        {
            var path = await GetDefaultSavePathAsync();
            if (string.IsNullOrWhiteSpace(path)) return [];

            return Directory.GetFiles(path, "*.cprj").ToList();
        }

    }


}
