using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommonLibrary.Models;

namespace Construct.Domain.Serialization
{
    /// <summary>
    /// Custom JSON converter voor Dictionary<Guid, BaseMateriaal>
    /// Handelt zowel array-format (Key/Value pairs) als object-format af
    /// </summary>
    public class MaterialenDictionaryConverter : JsonConverter<Dictionary<Guid, BaseMateriaal>>
    {
        public override Dictionary<Guid, BaseMateriaal>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dictionary = new Dictionary<Guid, BaseMateriaal>();

            // ? Handle ARRAY format: [{ "Key": "...", "Value": {...} }, ...]
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                // Deserialize as List<KeyValuePair<string, BaseMateriaal>>
                var kvpList = JsonSerializer.Deserialize<List<KeyValuePair<string, BaseMateriaal>>>(ref reader, options);
                
                if (kvpList != null)
                {
                    foreach (var kvp in kvpList)
                    {
                        if (Guid.TryParse(kvp.Key, out var guidKey) && kvp.Value != null)
                        {
                            dictionary[guidKey] = kvp.Value;
                        }
                    }
                }
                return dictionary;
            }
            
            // ? Handle OBJECT format: { "guid1": {...}, "guid2": {...} }
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        string propertyName = reader.GetString() ?? "";

                        if (Guid.TryParse(propertyName, out var key))
                        {
                            reader.Read();
                            var value = JsonSerializer.Deserialize<BaseMateriaal>(ref reader, options);

                            if (value != null)
                            {
                                dictionary[key] = value;
                            }
                        }
                    }
                }
            }

            return dictionary.Count > 0 ? dictionary : null;
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<Guid, BaseMateriaal> value, JsonSerializerOptions options)
        {
            if (value == null || value.Count == 0)
            {
                writer.WriteNullValue();
                return;
            }

            // ? Schrijf als ARRAY format voor compatibility: [{ "Key": "...", "Value": {...} }, ...]
            writer.WriteStartArray();

            foreach (var kvp in value)
            {
                writer.WriteStartObject();
                writer.WriteString("Key", kvp.Key.ToString());
                writer.WritePropertyName("Value");
                JsonSerializer.Serialize(writer, kvp.Value, options);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }
    }
}

