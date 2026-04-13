using CommonLibrary.Models;
using Eurocode.BetonConstructies;
using Eurocode.HoutConstructies;
using Eurocode.StaalConstructies;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Construct.Domain.Serialization
{
    public class BaseMateriaalJsonConverter : JsonConverter<BaseMateriaal>
    {
        public override BaseMateriaal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            if (!doc.RootElement.TryGetProperty("MateriaalType", out var typeProp))
                throw new JsonException("MateriaalType ontbreekt");

            var type = (MateriaalType)Enum.Parse(typeof(MateriaalType), typeProp.GetString()!);

            return type switch
            {
                MateriaalType.Beton => JsonSerializer.Deserialize<BetonContext>(doc.RootElement.GetRawText(), options),
                MateriaalType.Staal => JsonSerializer.Deserialize<StaalContext>(doc.RootElement.GetRawText(), options),
                MateriaalType.Hout => JsonSerializer.Deserialize<HoutContext>(doc.RootElement.GetRawText(), options),
                _ => throw new NotSupportedException($"Materiaal {type} niet ondersteund")
            };
        }

        public override void Write(Utf8JsonWriter writer, BaseMateriaal value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("MateriaalType", value.Type.ToString());

            using var doc = JsonSerializer.SerializeToDocument(value, value.GetType(), options);
            foreach (var prop in doc.RootElement.EnumerateObject())
                prop.WriteTo(writer);

            writer.WriteEndObject();
        }
    }

}
