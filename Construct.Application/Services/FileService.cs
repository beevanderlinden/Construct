using Construct.Application.Interfaces;
using Construct.Domain;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Construct.Application.Services
{
    public class FileService : IFileService
    {
        //private readonly IAppSettingService _appSettingService;
        private readonly JsonSerializerOptions _jsonOptions;



        public string SaveDirectory { get; set; } = AppContext.BaseDirectory; // default

        public FileService()
        {
            //_appSettingService = appSettingService;
            _jsonOptions = ProjectJsonOptions.Default;


            // ONDERSTAAND IS VERVANGEN DOOR bovenstaand, 
            // verwijderen if tested
            // -------
            //_jsonOptions = new JsonSerializerOptions
            //{
            //    // Standaard opties voor serialisatie, je kunt hier ook formatteren aanpassen
            //    WriteIndented = true,
            //    ReferenceHandler = ReferenceHandler.IgnoreCycles,
            //    NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            //    PropertyNameCaseInsensitive = true,
            //    IgnoreReadOnlyFields = true,
            //    IgnoreReadOnlyProperties = true,
            //    TypeInfoResolver = new DefaultJsonTypeInfoResolver
            //    {
            //        Modifiers =
            //        {
            //            ti =>
            //            {
            //                if (ti.Type == typeof(SectionElement))
            //                {
            //                    ti.PolymorphismOptions = new JsonPolymorphismOptions
            //                    {
            //                        TypeDiscriminatorPropertyName = "$type",
            //                        DerivedTypes =
            //                        {
            //                            //new JsonDerivedType(typeof(SectionContent), "SectionContent"),
            //                            new JsonDerivedType(typeof(ParagraphContent), "ParagraphContent"),
            //                            //new JsonDerivedType(typeof(TableCellContent), "TableCellContent"),
            //                            new JsonDerivedType(typeof(TableContent), "TableContent"),
            //                            new JsonDerivedType(typeof(HeadingContent), "HeadingContent"),
            //                            new JsonDerivedType(typeof(MigraDocElement), "MigraDocElement"),
            //                            new JsonDerivedType(typeof(MigraDocTable), "MigraDocTable")



            //                        }
            //                    };
            //                }
            //            }
            //        }
            //    }
            //};

            // -----
        }

        //public async Task SaveAsync<T>(T data, string? filename = null)
        //{
        //    var path = await _appSettingService.GetDefaultSavePathAsync();
        //    if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("Geen standaardpad ingesteld.");

        //    var filePath = Path.Combine(path, filename ?? "project.cproj");
        //    var json = JsonSerializer.Serialize(data);
        //    await File.WriteAllTextAsync(filePath, json);
        //}









        // Save - serialize object en comprimeer naar bestand
        public void Save(string path, object data)
        {
            // Serialiseren naar JSON
            string jsonData = JsonSerializer.Serialize(data, _jsonOptions);

            // Compressie naar bestand
            using FileStream fs = new(path, FileMode.Create);
            using GZipStream compressionStream = new(fs, CompressionLevel.Optimal);
            using StreamWriter writer = new(compressionStream);
            writer.Write(jsonData);
        }

        // Load - decompressie van bestand en deserialiseren naar object
        public T Load<T>(string path)
        {
            // Decompressie van bestand
            using FileStream fs = new(path, FileMode.Open);
            using GZipStream compressionStream = new(fs, CompressionMode.Decompress);
            using StreamReader reader = new(compressionStream);
            string jsonData = reader.ReadToEnd();

            var result = JsonSerializer.Deserialize<T>(jsonData, _jsonOptions);

            return result is null
                ? throw new InvalidOperationException($"Deserialisatie van bestand '{path}' naar {typeof(T).Name} is mislukt.")
                : result;
        }


        public string? GetBase64<T>(T obj)
        {
            try
            {
                string jsonData = JsonSerializer.Serialize(obj, _jsonOptions);
                byte[] compressed;
                string output;

                using (var memoryStream = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal))
                    {
                        gzipStream.Write(Encoding.UTF8.GetBytes(jsonData), 0, Encoding.UTF8.GetByteCount(jsonData));
                    }
                    compressed = memoryStream.ToArray();


                }

                // 'compressed' is nu een byte array van de gecomprimeerde data
                // streams zijn gesloten, dus we kunnen de byte array gebruiken

                using (var ms = new MemoryStream(compressed))
                {
                    // Hier kun je de byte array converteren naar Base64
                    output = Convert.ToBase64String(ms.ToArray());

                }


                return output;





                //using var memoryStream = new MemoryStream();
                //memoryStream.Write(Encoding.UTF8.GetBytes(jsonData), 0, Encoding.UTF8.GetByteCount(jsonData));



                //using var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal);
                //using var streamReader = new StreamReader(gzipStream);

                //var watBenIk = streamReader.ReadToEnd();




                //return watBenIk;

                //using var writer = new StreamWriter(gzipStream);



                //using var ms2 = new MemoryStream();
                //ms2.Position = 0;

                //gzipStream.CopyTo(ms2);

                //var bytes = streamReader.BaseStream.Read(ms2,);

                //writer.Flush();
                //gzipStream.Flush();

                //return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fout bij opslaan naar base64: {ex.Message}");
                return null;
            }
        }

        public T? LoadFromBytes<T>(byte[] bytes)
        {
            try
            {
                using var inputStream = new MemoryStream(bytes);
                using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
                using var reader = new StreamReader(gzipStream);
                var jsonData = reader.ReadToEnd();
                return JsonSerializer.Deserialize<T>(jsonData, _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fout bij laden uit bytes: {ex.Message}");
                return default;
            }
        }

        public T? LoadFromBase64<T>(string base64)
        {
            try
            {
                var bytes = Convert.FromBase64String(base64);

                using var inputStream = new MemoryStream(bytes);
                using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
                using var reader = new StreamReader(gzipStream);

                var jsonData = reader.ReadToEnd();
                return JsonSerializer.Deserialize<T>(jsonData, _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fout bij laden uit base64: {ex.Message}");
                return default;
            }
        }

        // 


        // Create - maakt een leeg bestand aan (optioneel)
        public void Create(string path)
        {
            // Zorg ervoor dat het bestand wordt gemaakt, zelfs als het leeg is
            using FileStream fs = new(path, FileMode.Create);
        }


        public async Task SaveAsync<T>(string path, T obj)
        {
            using var fileStream = File.Create(path);
            using var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal);
            await JsonSerializer.SerializeAsync(gzipStream, obj, _jsonOptions);
        }

        public async Task<T?> LoadAsync<T>(string path)
        {
            if (!File.Exists(path))
                return default;

            using var fileStream = File.OpenRead(path);
            using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            return await JsonSerializer.DeserializeAsync<T>(gzipStream, _jsonOptions);
        }




        public async Task CreateEmptyAsync<T>(string path) where T : new()
        {
            T newObj = new();
            await SaveAsync(path, newObj);
        }

        // ✅ Json string (synchroon, voor simpele json serialisatie gewoon sneller)
        public string GetJsonString<T>(T obj)
        {
            return JsonSerializer.Serialize(obj, _jsonOptions);
        }



        // ✅ Basis helper voor schrijven
        private async Task WriteJsonAsync<T>(Stream targetStream, T obj)
        {
            await JsonSerializer.SerializeAsync(targetStream, obj, _jsonOptions);
            await targetStream.FlushAsync();
        }

        // ✅ Byte[] json
        public async Task<byte[]> GetJsonBytesAsync<T>(T obj)
        {
            await using var ms = new MemoryStream();
            await WriteJsonAsync(ms, obj);
            return ms.ToArray();
        }

        // ✅ Byte[] zipped json
        public async Task<byte[]> GetGzippedJsonBytesAsync<T>(T obj)
        {
            await using var ms = new MemoryStream();
            await using var gzipStream = new GZipStream(ms, CompressionLevel.Optimal, leaveOpen: true);
            await WriteJsonAsync(gzipStream, obj);
            gzipStream.Close();
            return ms.ToArray();
        }

        // ✅ String json
        public async Task<string> GetJsonBase64Async<T>(T obj)
        {
            var bytes = await GetJsonBytesAsync(obj);
            return Convert.ToBase64String(bytes);
        }

        // ✅ String zipped json
        public async Task<string> GetGzippedJsonBase64Async<T>(T obj)
        {
            var bytes = await GetGzippedJsonBytesAsync(obj);
            return Convert.ToBase64String(bytes);
        }



    }
}
