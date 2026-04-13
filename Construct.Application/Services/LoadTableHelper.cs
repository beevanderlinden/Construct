using ExportFactory.MigraDocContentModels;
using Mechanica.SimpleBeam;

namespace Construct.Application.Services;

/// <summary>
/// Helper klasse voor het genereren van belasting tabellen
/// Kan worden gebruikt door zowel Application als WebUI.Server projecten
/// </summary>
public static class LoadTableHelper
{
    /// <summary>
    /// Genereert een tabel met belastingen voor een belastinggeval
    /// </summary>
    /// <param name="loads">IEnumerable van ILoad objecten</param>
    /// <param name="title">Optionele titel voor de tabel</param>
    /// <returns>TableContent voor MigraDoc/HTML export</returns>
    public static TableContent GetTabelBelastingen(IEnumerable<ILoad> loads, string? title = null)
    {
        var table = new TableContent
        {
            HideHeaders = false,
            Title = title ?? "",
            Headers =
            [
                new() { CellContent = new("naam") },
                new() { CellContent = new("omschrijving") },
                new() { CellContent = new("van - tot") },
                new() { CellContent = new("waarde") },
                new() { CellContent = new("eenheid") },
            ]
        };

        foreach (var l in loads)
        {
            table.Rows.Add(
            [
                new TableCellContent(l.Name, "20mm"),
                new TableCellContent(l.Description ?? "...", "100mm"),
                new TableCellContent(l.UserFriendlyFromTo, "24mm"),
                new TableCellContent(l.UserFriendlyFromToValue, "12mm"),
                new TableCellContent(l.Unit, "12mm")
            ]);
        }

        return table;
    }
}
