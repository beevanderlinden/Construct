using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using CommonLibrary.Models;

namespace Construct.Application.Serialization
{
    /// <summary>
    /// JsonConverter voor Dictionary<Guid, BaseMateriaal>
    /// - WRITE: Serialiseert als [{ Key, Value }, ...] 
    /// - READ: Deserialiseert via MateriaalFactory (polymorfie!)
    /// </summary>
    public class MaterialenDictionaryConverter : JsonConverter<Dictionary<Guid, BaseMateriaal>>
    {
        public override Dictionary<Guid, BaseMateriaal> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = new Dictionary<Guid, BaseMateriaal>();
            using (var jsonDoc = JsonDocument.ParseValue(ref reader))
            {
                var root = jsonDoc.RootElement;
                
                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in root.EnumerateArray())
                    {
                        if (item.TryGetProperty("Key", out var keyEl) && 
                            item.TryGetProperty("Value", out var valEl))
                        {
                            var keyStr = keyEl.GetString();
                            if (Guid.TryParse(keyStr, out var key))
                            {
                                try
                                {
                                    // ? Gebruik MateriaalFactory voor polymorfie!
                                    var node = JsonNode.Parse(valEl.GetRawText());
                                    if (node != null)
                                    {
                                        var material = Factories.MateriaalFactory.Create(node, options);
                                        if (material != null)
                                        {
                                            result[key] = material;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"? Materiaal deserialisatie error: {ex.Message}");
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<Guid, BaseMateriaal> value, JsonSerializerOptions options)
        {
            // ? Schrijf als array van { Key, Value }
            writer.WriteStartArray();
            foreach (var kvp in value)
            {
                writer.WriteStartObject();
                writer.WriteString("Key", kvp.Key.ToString());
                writer.WritePropertyName("Value");
                
                // ? HANDMATIG: Schrijf MateriaalType + Material properties FLAT
                writer.WriteStartObject();
                
                // Bepaal type van concrete klasse
                string materiaalType = kvp.Value.GetType().Name switch
                {
                    "BetonContext" => "Beton",
                    "StaalContext" => "Staal",
                    "HoutContext" => "Hout",
                    _ => kvp.Value.GetType().Name
                };
                
                // Schrijf MateriaalType EERST
                writer.WriteString("MateriaalType", materiaalType);
                
                // Serialiseer Material zelf, dan kopieer properties
                using (var doc = JsonDocument.Parse(JsonSerializer.Serialize(kvp.Value, kvp.Value.GetType(), options)))
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (prop.Name != "MateriaalType")  // Skip duplicates
                        {
                            writer.WritePropertyName(prop.Name);
                            prop.Value.WriteTo(writer);
                        }
                    }
                }
                
                writer.WriteEndObject();
                
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
    }

    /// <summary>
    /// VERWIJDERD: MaterialWithTypeWrapper is niet meer nodig
    /// We schrijven nu handmatig in Write()
    /// </summary>
}
