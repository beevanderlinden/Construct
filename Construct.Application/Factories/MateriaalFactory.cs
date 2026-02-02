using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonLibrary.Models;
using Eurocode.BetonConstructies;
using Eurocode.HoutConstructies;
using Eurocode.StaalConstructies;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Construct.Application.Factories
{

    /// <summary>
    /// Factory voor het aanmaken van Materiaal-objecten op basis van JSON-inhoud.
    /// Dit is nodig omdat Materiaal een polymorf type is.
    /// Daarbij staat BaseMateriaal in een gedeelde bibliotheek.
    /// De betreffende afgeleide Context-types (Beton, Staal, Hout, etcetera) staan via aparte NuGet-packages in de Domain laag.
    /// </summary>
    public static class MateriaalFactory
    {
        public static BaseMateriaal Create(JsonNode node, JsonSerializerOptions options)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            // ophalen
            var typeProp = node["MateriaalType"]?.GetValue<string>() 
                ?? throw new JsonException("MateriaalType ontbreekt");

            var type = Enum.Parse<MateriaalType>(typeProp);

            return type switch
            {
                MateriaalType.Beton => node.Deserialize<BetonContext>(options)!,
                MateriaalType.Staal => node.Deserialize<StaalContext>(options)!,
                MateriaalType.Hout => node.Deserialize<HoutContext>(options)!,
                _ => throw new NotSupportedException($"Onbekend MateriaalType: {type}")
            };

            
        }
    }

}
