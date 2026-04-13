using System.Text.Json;
using System.Text.Json.Serialization;
using Eurocode.Belastingen;

namespace Construct.Domain.Serialization
{
    /// <summary>
    /// Custom JSON converter voor BelastingCombinatieItem om circular references te voorkomen.
    /// Serialiseert ALLEEN Geval en PermanentIsGunstig (niet Context en Combinatie).
    /// Bij deserialisatie worden Context en Combinatie hersteld via RestoreReferencesAfterDeserialization().
    /// </summary>
    public class BelastingCombinatieItemConverter : JsonConverter<BelastingCombinatieItem>
    {
        public override BelastingCombinatieItem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token");
            }

            BelastingGeval? geval = null;
            bool permanentIsGunstig = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString()!;
                    reader.Read(); // Move to value

                    switch (propertyName)
                    {
                        case "Geval":
                            geval = JsonSerializer.Deserialize<BelastingGeval>(ref reader, options);
                            break;
                        case "PermanentIsGunstig":
                            permanentIsGunstig = reader.GetBoolean();
                            break;
                        // ?? IGNORE: Combinatie, Context (worden later hersteld)
                        case "Combinatie":
                        case "Context":
                            reader.Skip();
                            break;
                    }
                }
            }

            if (geval == null)
            {
                throw new JsonException("Geval is required for BelastingCombinatieItem");
            }

            // ? Create item WITHOUT Context/Combinatie (deze worden later gezet in RestoreReferencesAfterDeserialization)
            return new BelastingCombinatieItem
            {
                Context = null!, // Wordt hersteld in AssemblageEntity.RestoreReferencesAfterDeserialization
                Combinatie = null!, // Wordt hersteld in AssemblageEntity.RestoreReferencesAfterDeserialization
                Geval = geval,
                PermanentIsGunstig = permanentIsGunstig
            };
        }

        public override void Write(Utf8JsonWriter writer, BelastingCombinatieItem value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            // ? Serialiseer ALLEEN Geval en PermanentIsGunstig
            // ? NIET: Context en Combinatie (circular references!)

            writer.WritePropertyName("Geval");
            JsonSerializer.Serialize(writer, value.Geval, options);

            writer.WritePropertyName("PermanentIsGunstig");
            writer.WriteBooleanValue(value.PermanentIsGunstig);

            // ?? SKIP: Combinatie en Context (circular references!)

            writer.WriteEndObject();
        }
    }
}
