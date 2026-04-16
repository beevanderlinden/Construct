namespace Construct.WebUI.Server.Components.Shared;

/// <summary>
/// Beschrijft een eenheidsoptie voor KitInputNumberWithUnit.
/// Factor: displaywaarde = modelwaarde × Factor
/// </summary>
/// <param name="Label">Weergegeven eenheidslabel (bijv. "cm⁴")</param>
/// <param name="Factor">Conversiefactor: display = model × Factor</param>
/// <param name="Step">Stapgrootte voor de HTML input (default: "any")</param>
public record UnitOption(string Label, double Factor, string Step = "any");

/// <summary>
/// Voorgedefinieerde eenheidssets voor veelgebruikte grootheden.
/// </summary>
public static class UnitSets
{
    /// <summary>Traagheidsmoment I — model opgeslagen in m⁴</summary>
    public static readonly IReadOnlyList<UnitOption> Traagheid =
    [
        new("mm⁴", 1e12, "1"),
        new("cm⁴", 1e8,  "0.1"),
        new("dm⁴", 1e4,  "0.001"),
        new("m⁴",  1.0,  "0.000001"),
    ];

    /// <summary>Elasticiteitsmodulus E — model opgeslagen in kN/m²</summary>
    public static readonly IReadOnlyList<UnitOption> Elasticiteitsmodulus =
    [
        new("kN/m²", 1.0,   "1000"),
        new("N/mm²", 0.001, "1"),
    ];

    /// <summary>Kracht — model opgeslagen in kN</summary>
    public static readonly IReadOnlyList<UnitOption> Kracht =
    [
        new("N",  1000.0, "100"),
        new("kN", 1.0,    "0.1"),
    ];

    /// <summary>Kracht — model opgeslagen in kN/m</summary>
    public static readonly IReadOnlyList<UnitOption> KrachtPerLengte =
    [
        new("N/mm",  1.0, "1"),
        new("kN/m", 1.0,    "1.0"),
    ];


    public static readonly IReadOnlyList<UnitOption> KrachtPerOppervlak =
   [
        new("kN/m²", 1.0,    "0.5"),
        new("kg/m²", 100,    "50"),

    ];


    /// <summary>Moment — model opgeslagen in kNm</summary>
    public static readonly IReadOnlyList<UnitOption> Moment =
    [
        new("Nm",  1000.0, "100"),
        new("kNm", 1.0,    "0.01"),
    ];

    /// <summary>Lengte — model opgeslagen in m</summary>
    public static readonly IReadOnlyList<UnitOption> Lengte =
    [
        new("mm", 1000.0, "1"),
        new("cm", 100.0,  "0.1"),
        new("m",  1.0,    "0.001"),
    ];

    /// <summary>Lengte — model opgeslagen in mm</summary>
    public static readonly IReadOnlyList<UnitOption> LengteMM =
    [
        new("mm", 1, "1"),
        new("cm", 0.1,  "0.1"),
        new("m",  0.001,    "0.001"),
    ];



    /// <summary>Geen eenheid — verbergt de unit-selector; model- en displaywaarde zijn gelijk.</summary>
    public static readonly IReadOnlyList<UnitOption> Geen =
    [
        new("", 1.0),
    ];
}
