using Construct.Domain.Serialization;
using ExportFactory.MigraDocContentModels;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Construct.Domain
{
    /// <summary>
    /// Een statische helperklasse voor centrale opties bij serialization.
    /// </summary>
    public static class ProjectJsonOptions
    {
        public static readonly JsonSerializerOptions Default = CreateDefaultOptions();
        public static readonly JsonSerializerOptions Fast = CreateFastOptions();

        // ✅ Static constructor - registreer converters NA opties creation
        static ProjectJsonOptions()
        {
            // MaterialenDictionaryConverter wordt geregistreerd in Program.cs
            // Dit moet gedaan worden in Program.cs omdat die Application kan zien
        }



        private static JsonSerializerOptions CreateDefaultOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters =
                {
                    new MaterialenDictionaryConverter(),  // ✅ NIEUW: Custom converter voor Dictionary<Guid, BaseMateriaal>
                    new BelastingCombinatieItemConverter(),  // ✅ NIEUW: Custom converter om circular references te voorkomen
                    new JsonStringEnumConverter()  // ✅ Serialiseer enums als strings
                },
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                PropertyNameCaseInsensitive = true,
                IgnoreReadOnlyFields = true,
                IgnoreReadOnlyProperties = true,
                AllowTrailingCommas = true,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers =
                    {
                        ti =>
                        {
                            if (ti.Type == typeof(SectionElement))
                            {
                                ti.PolymorphismOptions = new JsonPolymorphismOptions
                                {
                                    TypeDiscriminatorPropertyName = "$type",
                                    DerivedTypes =
                                    {
                                        new JsonDerivedType(typeof(ParagraphContent), "ParagraphContent"),
                                        new JsonDerivedType(typeof(TableContent), "TableContent"),
                                        new JsonDerivedType(typeof(HeadingContent), "HeadingContent"),
                                        new JsonDerivedType(typeof(MigraDocElement), "MigraDocElement"),
                                        new JsonDerivedType(typeof(MigraDocTable), "MigraDocTable")
                                    }
                                };
                            }
                        }
                    }
                }
            };
        }

        private static JsonSerializerOptions CreateFastOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = false,
                IncludeFields = false,
                IgnoreReadOnlyFields = true,
                IgnoreReadOnlyProperties = true,
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                PropertyNameCaseInsensitive = true,  // ✅ NIEUW: Zorg voor case-insensitive deserialisatie
                Converters =
                {
                    new MaterialenDictionaryConverter(),  // ✅ NIEUW: Custom converter voor Dictionary<Guid, BaseMateriaal>
                    new BelastingCombinatieItemConverter(),  // ✅ NIEUW: Custom converter om circular references te voorkomen
                    new JsonStringEnumConverter()  // ✅ Serialiseer enums als strings
                },
            };
        }
    }
}

