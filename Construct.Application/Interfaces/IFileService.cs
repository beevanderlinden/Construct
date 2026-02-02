namespace Construct.Application.Interfaces
{
    public interface IFileService
    {
        string SaveDirectory { get; set; }

        void Save(string path, object data);
        T Load<T>(string path);


        T? LoadFromBase64<T>(string base64);

        T? LoadFromBytes<T>(byte[] bytes);

        string? GetBase64<T>(T obj);

        void Create(string path);

        Task SaveAsync<T>(string path, T obj);
        Task<T?> LoadAsync<T>(string path);
        Task CreateEmptyAsync<T>(string path) where T : new();


        string GetJsonString<T>(T obj);


        Task<byte[]> GetJsonBytesAsync<T>(T obj);
        Task<byte[]> GetGzippedJsonBytesAsync<T>(T obj);

        Task<string> GetJsonBase64Async<T>(T obj);
        Task<string> GetGzippedJsonBase64Async<T>(T obj);


    }

}
