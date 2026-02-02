using CommonLibrary.Models;
using Eurocode.BetonConstructies;
using Eurocode.HoutConstructies;
using Eurocode.StaalConstructies;
using ExportFactory.MigraDocContentModels;
using Mechanica.SimpleBeam;
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace Construct.WebUI.Server.Components.Beam;

/// <summary>
/// Gedeelde helper voor het genereren van snedekrachten en toets tabellen voor beams
/// Gebruikt door zowel PrintPage als BeemLoadCases
/// </summary>
public static class BeamResultsHelper
{
    /// <summary>
    /// Bepaalt de 3 kritieke punten (begin, veldmoment/midden, eind) met hun snedekrachten
    /// </summary>
    private static List<(double X, string Position, InternalForces Forces)> GetKritiekePunten(SBLigger beam)
    {
        // Verzamel alle moment/shear data
        var allResults = beam.ResultCollection?.All?.ToList() ?? new();
        
        if (allResults.Count == 0)
            return [];

        // Combineer normale momentendiagram met toevallige inklemming (voor meest ongunstige waarden)
        var allMoments = allResults
            .SelectMany(r => r.MomentDiagram.Concat(r.MomentDiagramAccidentalFixity))
            .ToList();
        
        
        if (allMoments.Count == 0)
            return [];

        // Kritieke posities
        var xBegin = 0.0;
        var xEind = beam.Length;
        
        // Vind punt met grootste veldmoment
        var minMoment = allMoments.Min(m => m.M);
        double xVeldMoment;
        
        if (minMoment < 0)
        {
            var (x, M) = allMoments.FirstOrDefault(m => Math.Abs(m.M - minMoment) < 0.01);
            xVeldMoment = (x == 0 && M == 0) 
                ? beam.Length / 2.0
                : x;
        }
        else
        {
            xVeldMoment = beam.Length / 2.0;
        }

        // Helper functie om V te vinden op positie x
        double GetVzAtPosition(double x, bool useMaxAbsOverAllResults)
        {
            if (useMaxAbsOverAllResults)
            {
                var allVzValues = allResults
                    .SelectMany(r => r.ShearDiagram)
                    .Where(s => Math.Abs(s.x - x) < 0.001)
                    .Select(s => s.V)
                    .ToList();
                
                return allVzValues.Count != 0
                    ? allVzValues.OrderByDescending(v => Math.Abs(v)).First()
                    : 0;
            }
            else
            {
                var resultWithMaxMoment = allResults
                    .FirstOrDefault(r => r.MomentDiagram.Any(m => 
                        Math.Abs(m.x - x) < 0.001 && Math.Abs(m.M - minMoment) < 0.01));
                
                if (resultWithMaxMoment != null)
                {
                    var vzValues = resultWithMaxMoment.ShearDiagram
                        .Where(s => Math.Abs(s.x - x) < 0.001)
                        .ToList();
                    
                    return vzValues.Count != 0
                        ? vzValues.OrderByDescending(s => Math.Abs(s.V)).First().V
                        : 0;
                }
                return 0;
            }
        }

        // Helper om My te vinden op positie x
        double GetMyAtPosition(double x)
        {
            var moments = allMoments.Where(m => Math.Abs(m.x - x) < 0.001).ToList();
            return moments.Count != 0
                ? moments.OrderByDescending(m => Math.Abs(m.M)).First().M
                : 0;
        }

        // Maak de 3 kritieke punten
        var selectedPoints = new List<(double X, string Position, InternalForces Forces)>
        {
            (xBegin, $"{xBegin:F3}m", new InternalForces 
            { 
                N = 0, My = GetMyAtPosition(xBegin),
                Vz = GetVzAtPosition(xBegin, useMaxAbsOverAllResults: true),
                Mz = 0, Vy = 0, T = 0
            }),
            
            (xVeldMoment, $"{xVeldMoment:F3}m", new InternalForces 
            { 
                N = 0, My = GetMyAtPosition(xVeldMoment),
                Vz = GetVzAtPosition(xVeldMoment, useMaxAbsOverAllResults: false),
                Mz = 0, Vy = 0, T = 0
            }),
            
            (xEind, $"{xEind:F3}m", new InternalForces 
            { 
                N = 0, My = GetMyAtPosition(xEind),
                Vz = GetVzAtPosition(xEind, useMaxAbsOverAllResults: true),
                Mz = 0, Vy = 0, T = 0
            })
        };

        // Verwijder duplicaten
        return selectedPoints
            .DistinctBy(p => Math.Round(p.X, 3))
            .OrderBy(p => p.X)
            .ToList();
    }
    /// <summary>
    /// Genereert een tabel met snedekrachten voor alle posities langs de beam
    /// </summary>
    /// <param name="beam">De beam waarvoor de snedekrachten getoond worden</param>
    /// <param name="maxRows">Maximum aantal rijen (voor performance)</param>
    /// <returns>TableContent voor MigraDoc/HTML export</returns>
    public static TableContent GetTabelSnedeKrachten(SBLigger beam, int maxRows = 10)
    {
        var table = new TableContent
        {
            LayoutOnly = false,
            IsPivotTable = false,
            HideHeaders = false
        };

        // Headers
        table.Headers =
        [
            new(new("positie", "6cm")),
            new(new("<i>N</i> [kN]", "2cm")),
            new(new("<i>M<sub>y</sub></i> [kNm]", "2cm")),
            new(new("<i>V<sub>z</sub></i> [kN]", "2cm")),
            new(new("<i>M<sub>z</sub></i> [kNm]", "2cm")),
            new(new("<i>V<sub>y</sub></i> [kN]", "2cm")),
            new(new("<i>M<sub>x</sub></i> [kNm]", "2cm")),
        ];

        // Gebruik gedeelde logica voor kritieke punten
        var selectedPoints = GetKritiekePunten(beam);
        
        if (!selectedPoints.Any())
            return table;

        // Max waarden voor bold formatting
        var maxN = selectedPoints.Max(p => Math.Abs(p.Forces.N));
        var maxMy = selectedPoints.Max(p => Math.Abs(p.Forces.My));
        var maxVz = selectedPoints.Max(p => Math.Abs(p.Forces.Vz));
        var maxMz = selectedPoints.Max(p => Math.Abs(p.Forces.Mz));
        var maxVy = selectedPoints.Max(p => Math.Abs(p.Forces.Vy));
        var maxT = selectedPoints.Max(p => Math.Abs(p.Forces.T));

        // Helper voor formatting
        string FormatForce(double value, double maxValue, string format = "0.#")
        {
            string formatted = value.ToString(format, CultureInfo.InvariantCulture);
            
            // Bold als max waarde
            if (Math.Abs(Math.Abs(value) - maxValue) < 0.01 && maxValue > 0.01)
                return $"**{formatted}**";
            
            return formatted;
        }

        // Rijen toevoegen - ALLEEN de 3 geselecteerde punten
        foreach (var point in selectedPoints)
        {
            table.Rows.Add(
            [
                new(point.Position, "6cm"),
                new(FormatForce(point.Forces.N, maxN), "2cm"),
                new(FormatForce(point.Forces.My, maxMy), "2cm"),
                new(FormatForce(point.Forces.Vz, maxVz), "2cm"),
                new(FormatForce(point.Forces.Mz, maxMz), "2cm"),
                new(FormatForce(point.Forces.Vy, maxVy), "2cm"),
                new(FormatForce(point.Forces.T, maxT), "2cm"),
            ]);
        }

        return table;
    }



    public static TableContent GetTabelBuigingY(SBLigger beam)
    {
        var tbl = new TableContent();
        if (beam == null) return tbl;

        // 
        //List<BendingResults> bendingResults = [];
        //var kritiekePunten = GetKritiekePunten(beam);
        //foreach (var kp in kritiekePunten)
        //{
        //    BendingResults br = new()
        //    {
        //        Beton = new(),
        //        Profiel = beam.Profiel, 
        //        PosLabel = kp.Position,
        //        //br.PosLabelVisible = true;
        //        Wapening = null,
        //        Snedekrachten = kp.Forces
        //    };
        //    bendingResults.Add(br);
        //}


        //foreach (var fx in ForceCollection.OrderBy(fc => fc.Pos))
        //{
        //    BendingResults br = new()
        //    {
        //        Beton = beton ?? new(),
        //        Profiel = this.Profiel,
        //        PosLabel = fx.Pos.ToString("0.000", CultureInfo.InvariantCulture),
        //        //br.PosLabelVisible = true;
        //        Wapening = fx.Forces.My < 0 ? WapOnder : WapBoven,
        //        Snedekrachten = fx.Forces
        //    };
        //    BendingResults.Add(br);
        //}

        return tbl;
    }

    /// <summary>
    /// Genereert twee tabellen met buigingsresultaten gericht op betonwapening:
    /// - hoofdwapening (bending reinforcement) met positie en benodigde/voorziene As
    /// - dwarskrachtwapening (shear) met positie en benodigde/voorziene waarden
    ///
    /// Op dit moment bevat de berekening placeholders voor As_required / As_prov en moet
    /// later vervangen worden door daadwerkelijke Eurocode-berekeningen.
    /// </summary>
    public static (TableContent Hoofd, TableContent Dwars) GetTabelBendingResults(SBLigger beam, BaseProfiel profiel, BaseMateriaal materiaal)
    {
        // Gebruik kritieke punten als rijen (begin, veldmoment, einde)
        var punten = GetKritiekePunten(beam);

        // Hoofdwapening tabel
        var hoofd = new TableContent { HideHeaders = false };
        hoofd.Headers =
        [
            new(new("positie", "4cm")),
            new(new("vlak", "4cm")),
            new(new("*M~y,Ed~* [kNm]", "4cm")),
            new(new("*A~s,req~* [mm²]", "4cm")),
            new(new("*A~s,prov~* [mm²]", "4cm")),
            new(new("U.C.", "4cm")),

        ];

        // Dwarskracht tabel
        var dwars = new TableContent { HideHeaders = false };
        dwars.Headers =
        [
            new(new("positie", "3cm")),
            new(new("*V~z,Ed~* [kN]", "4cm")),
            new(new("*A~sw,req~*", "4cm")),
            new(new("*A~sw,prov~*", "4cm")),
            
        ];

        if (!punten.Any())
            return (hoofd, dwars);

        // toevallige inklemming
        //var toevInklemming = punten.Min(p => p.Forces.My) * -0.15; // 15% vaste waarde


        // eenvoudige placeholder-berekeningen: Vervang met echte Eurocode checks.
        foreach (var p in punten)
        {
            double My = p.Forces.My; // kNm
           // if (My >=0 && My < toevInklemming) 
             //   My = toevInklemming;
            double Vz = p.Forces.Vz; // kN

            // Placeholder: As_required: simpele schatting op basis van moment
            var betonProfiel = profiel as Profielen.Beton.BetonProfiel;
            var wapening = My < 0 ? beam.PlaatWapening.Onder.BasisWapening : beam.PlaatWapening.Boven.BasisWapening;

            var bending = new BendingResults((BetonContext)materiaal, betonProfiel, wapening, new() { My = My });



            double asReq = bending.AsRequired;
            double asProv = wapening.As;
            string asProvTekst = wapening.GetUserFriendlyText("");
            string vlak = My > 0 ? "boven" : "onder";
            double uc = asReq / asProv;

            hoofd.Rows.Add([
                new(p.Position, "2cm"),
                new(vlak, "2cm"),
                new(My.ToString("0.0", CultureInfo.InvariantCulture), "4cm"),
                new(asReq.ToString("0", CultureInfo.InvariantCulture), "4cm"),
                new($"{asProvTekst}", "4cm"),
                new($"{uc:0.00}" + (uc > 1.01? "⚠️" : ""),"2cm")
            ]);

            // Dwars
            double aswReq = Math.Max(0.0, Math.Abs(Vz) * 5.0); // dummy
            double aswProv = 0.0;
            dwars.Rows.Add([
                new(p.Position, "6cm"),
                new(Vz.ToString("0.00", CultureInfo.InvariantCulture), "4cm"),
                new(aswReq.ToString("0.##", CultureInfo.InvariantCulture), "4cm"),
                new(aswProv > 0 ? aswProv.ToString("0")  : string.Empty, "4cm")
            ]);
        }

        return (hoofd, dwars);
    }

    /// <summary>
    /// Genereert een tabel met Eurocode snede-toetsresultaten voor staal OF hout
    /// Gebruikt DEZELFDE kritieke punten als GetTabelSnedeKrachten
    /// </summary>
    /// <param name="beam">De beam waarvoor de toetsen uitgevoerd worden</param>
    /// <param name="profiel">Het profiel (staal of hout)</param>
    /// <param name="materiaal">Het materiaal context (StaalContext of HoutContext)</param>
    /// <param name="sectionClass">Doorsnedeklasse (1 = plastisch, 3 = elastisch) - alleen voor staal</param>
    /// <param name="maxRows">Maximum aantal rijen (voor performance)</param>
    /// <returns>TableContent voor MigraDoc/HTML export</returns>
    public static TableContent GetTabelSnedeToetsen(
        SBLigger beam, 
        BaseProfiel profiel, 
        BaseMateriaal materiaal,
        int sectionClass = 1,
        int maxRows = 10)
    {
        var table = new TableContent
        {
            LayoutOnly = false,
            IsPivotTable = false,
            HideHeaders = false
        };

        // Headers
        table.Headers =
        [
            new(new("positie", "3cm")),
            new(new("norm", "3cm")),
            new(new("artikel", "3cm")),
            new(new("formule", "3cm")),
            new(new("U.C.", "3cm")),
            new(new("[N/mm²]", "3cm")),
        ];

        // Gebruik DEZELFDE kritieke punten als snedekrachten tabel
        var kritiekePunten = GetKritiekePunten(beam);
        
        if (!kritiekePunten.Any())
            return table;

        // Verzamel toetsen voor elk kritiek punt
        var toetsen = new List<EurocodeResultaat>();

        foreach (var punt in kritiekePunten)
        {
            // Moment toets (als My > 0.01)
            if (Math.Abs(punt.Forces.My) > 0.01)
            {
                var forces = new InternalForces { My = punt.Forces.My };
                
                if (materiaal is StaalContext staalContext)
                {
                    var toetsMy = new Eurocode.StaalConstructies.BendingMyToets(sectionClass) 
                    { 
                        Positie = $"{punt.Position} (M~y~)" 
                    };
                    var toets = toetsMy.Check(forces, profiel, staalContext);
                    toetsen.Add(toets);
                }
                else if (materiaal is HoutContext houtContext)
                {
                    var toetsMy = new Eurocode.HoutConstructies.BendingMyToets() 
                    { 
                        Positie = $"{punt.Position} (M~y~)" 
                    };
                    var toets = toetsMy.Check(forces, profiel, houtContext);
                    toetsen.Add(toets);
                }
            }

            // Dwarskracht toets (als Vz > 0.01)
            if (Math.Abs(punt.Forces.Vz) > 0.01)
            {
                var forces = new InternalForces { Vz = punt.Forces.Vz };
                
                if (materiaal is StaalContext staalContext)
                {
                    var checkVz = new Eurocode.StaalConstructies.ShearVzCheck() 
                    { 
                        Positie = $"{punt.Position} (V~z~)" 
                    };
                    var toets = checkVz.Check(forces, profiel, staalContext);
                    toetsen.Add(toets);
                }
                else if (materiaal is HoutContext houtContext)
                {
                    var checkVz = new Eurocode.HoutConstructies.ShearVzCheck() 
                    { 
                        Positie = $"{punt.Position} (V~z~)" 
                    };
                    var toets = checkVz.Check(forces, profiel, houtContext);
                    toetsen.Add(toets);
                }
            }
        }

        if (!toetsen.Any())
            return table;

        // Max benutting
        var maxBenutting = toetsen.Max(t => t.Benutting);

        // Rijen toevoegen (gesorteerd op benutting, hoogste eerst)
        foreach (var toets in toetsen.Take(maxRows))
        {
            var isBold = Math.Abs(toets.Benutting - maxBenutting) < 0.001 && maxBenutting > 0.001;
            
            // Formatteer benutting
            var benuttingTekst = toets.Benutting.ToString("0.000", CultureInfo.InvariantCulture);
            
            // Bold voor max
            if (isBold)
                benuttingTekst = $"**{benuttingTekst}**";
            
            // Waarschuwing voor overschrijding
            if (!toets.Voldoet)
                benuttingTekst += " ⚠️";

            // Spanning (StaalSpanning of andere property, beide in N/mm²)
            var spanningWaarde = toets.StaalSpanning; // Property bestaat op EurocodeResultaat
            
            // Maak cellen
            var positieCell = new TableCellContent(toets.Positie, "3cm");
            var normCell = new TableCellContent(toets.Norm, "3cm");
            var artikelCell = new TableCellContent(toets.Artikel, "3cm");
            var formuleCell = new TableCellContent(toets.Formule, "3cm");
            var benuttingCell = new TableCellContent(benuttingTekst, "3cm");
            var spanningCell = new TableCellContent(spanningWaarde.ToString("0", CultureInfo.InvariantCulture), "3cm");

            // Rode markering als niet voldoet
            if (!toets.Voldoet)
            {
                benuttingCell.Markdown = $"{{red:{benuttingTekst}}}";
                spanningCell.Markdown = $"{{red:{spanningWaarde:0}}}";
            }

            table.Rows.Add(
            [
                positieCell,
                normCell,
                artikelCell,
                formuleCell,
                benuttingCell,
                spanningCell,
            ]);
        }

        return table;
    }

    /// <summary>
    /// Genereert een tabel met profiel eigenschappen voor staal of hout
    /// Layout: 4 kolomparen (symbol:value) horizontaal verdeeld
    /// </summary>
    /// <param name="profiel">Het profiel waarvoor eigenschappen getoond worden</param>
    /// <returns>TableContent voor MigraDoc/HTML export</returns>
    public static TableContent GetTabelProfielEigenschappen(BaseProfiel profiel)
    {
        var table = new TableContent
        {
            LayoutOnly = false,
            IsPivotTable = false,
            HideHeaders = true
        };

        int n = 4; // aantal symbol/value kolomparen
        string w1 = "10mm"; // symbol width
        string w2 = "35mm"; // value width

        // Headers: 4x (symbol, value)
        for (int i = 0; i < n; i++)
        {
            table.Headers.Add(new TableCellHeaderContent() { CellContent = new("symbol", w1) });
            table.Headers.Add(new TableCellHeaderContent() { CellContent = new("value", w2) });
        }

        // Helper voor wetenschappelijke notatie
        string FormatWithPower(double value, int powerOf10, string unit, string format = "0.#")
        {
            double adjustedValue = value * Math.Pow(10, -powerOf10);
            string formattedValue = adjustedValue.ToString(format, CultureInfo.InvariantCulture);
            
            if (powerOf10 == 0)
                return $"{formattedValue} {unit}";
            
            string formattedUnit = unit.Contains("^") ? unit : unit;
            return $"{formattedValue} ·10^{powerOf10}^ {formattedUnit}";
        }

        // ProfielIH (HEA, HEB, HEM, IPE, etc.)
        if (profiel is Profielen.Staal.ProfielIH p)
        {
            table.Rows.Add([
                new("*h* :", w1), new(p.H.ToString("0.# mm", CultureInfo.InvariantCulture), w2),
                new("*i~y~* :", w1), new(p.TraagheidsStraalIy.ToString("0.# mm", CultureInfo.InvariantCulture), w2), 
                new("*W~el,y~* :", w1), new(FormatWithPower(p.WelY, 3, "mm^3^", "0"), w2),
                new("*I~y~* :", w1), new(FormatWithPower(p.Iy, 4, "mm^4^", "0.#"), w2),
            ]);

            table.Rows.Add([
                new("*b* :", w1), new(p.B.ToString("0.# mm", CultureInfo.InvariantCulture), w2),
                new("*i~z~* :", w1), new(p.TraagheidsStraalIz.ToString("0.# mm", CultureInfo.InvariantCulture), w2),
                new("*W~el,z~* :", w1), new(FormatWithPower(p.WelZ, 3, "mm^3^", "0.#"), w2),
                new("*I~z~* :", w1), new(FormatWithPower(p.Iz, 4, "mm^4^", "0.#"), w2),
            ]);

            table.Rows.Add([
                new("*t~w~* :", w1), new(p.Tw.ToString("0.# mm", CultureInfo.InvariantCulture), w2),
                new("*r* :", w1), new(p.R.ToString("0.# mm", CultureInfo.InvariantCulture), w2),
                new("*W~pl,y~* :", w1), new(FormatWithPower(p.WplY, 3, "mm^3^", "0.#"), w2),
                new("*I~t~* :", w1), new(FormatWithPower(p.It, 4, "mm^4^", "0.#"), w2),
            ]);

            table.Rows.Add([
                new("*t~f~* :", w1), new(p.Tf.ToString("0.# mm", CultureInfo.InvariantCulture), w2),
                new("*A* :", w1), new(p.A.ToString("0 mm²", CultureInfo.InvariantCulture), w2),
                new("*W~pl,z~* :", w1), new(FormatWithPower(p.WplZ, 3, "mm^3^", "0.#"), w2),
                new("*I~w~* :", w1), new(FormatWithPower(p.Iw, 6, "mm^6^", "0.#"), w2),
            ]);
        }

        // ParametrischProfielContext
        if (profiel is Profielen.Parametrisch.ParametrischProfielContext pp)
        {
            table.Rows.Add([
                new("*h* :", w1), new(pp.H.ToString("0.# mm", CultureInfo.InvariantCulture), w2),
                new("*i~y~* :", w1), new(pp.TraagheidsStraalIy.ToString("0.# mm", CultureInfo.InvariantCulture), w2),
                new("*W~y~* :", w1), new(FormatWithPower(pp.Wy, 3, "mm^3^", "0"), w2),
                new("*I~y~* :", w1), new(FormatWithPower(pp.Iy, 4, "mm^4^", "0.#"), w2),
            ]);

            table.Rows.Add([
                new("*b* :", w1), new(pp.B.ToString("0.# mm", CultureInfo.InvariantCulture), w2),
                new("*i~z~* :", w1), new(pp.TraagheidsStraalIz.ToString("0.# mm", CultureInfo.InvariantCulture), w2),
                new("*W~z~* :", w1), new(FormatWithPower(pp.Wz, 3, "mm^3^", "0.#"), w2),
                new("*I~z~* :", w1), new(FormatWithPower(pp.Iz, 4, "mm^4^", "0.#"), w2),
            ]);
        }

        return table;
    }

    /// <summary>
    /// Genereert een tabel met materiaal eigenschappen (basis eigenschappen voor alle materialen)
    /// </summary>
    /// <param name="materialen">Lijst van materialen om te tonen</param>
    /// <returns>TableContent voor MigraDoc/HTML export</returns>
    public static TableContent GetTabelMateriaalEigenschappen(IEnumerable<BaseMateriaal> materialen)
    {
        var table = new TableContent
        {
            LayoutOnly = false,
            IsPivotTable = false,
            HideHeaders = false
        };

        string width = "45mm";

        // Headers
        table.Headers =
        [
            new(new("type", width)),
            new(new("kwaliteit", width)),
            new(new("E-modulus [N/mm²]", width)),
            new(new("s.g. [kg/m³]", width))
        ];

        // Rijen
        foreach (var m in materialen ?? Enumerable.Empty<BaseMateriaal>())
        {
            table.Rows.Add([
                new(m.Type.ToString(), width),
                new(m.Naam, width),
                new(m.E.ToString("0", CultureInfo.InvariantCulture), width),
                new(m.SoortelijkGewicht.ToString("0", CultureInfo.InvariantCulture), width)
            ]);
        }

        return table;
    }

    /// <summary>
    /// Genereert een tabel met staal-specifieke eigenschappen
    /// </summary>
    /// <param name="staalMaterialen">Lijst van StaalContext materialen</param>
    /// <returns>TableContent voor MigraDoc/HTML export</returns>
    public static TableContent GetTabelMateriaalEigenschappen(IEnumerable<StaalContext> staalMaterialen)
    {
        var table = new TableContent
        {
            LayoutOnly = false,
            IsPivotTable = false,
            HideHeaders = false
        };

        string width = "36mm";

        // Headers met staal-specifieke kolommen
        table.Headers =
        [
            new(new("type", width)),
            new(new("kwaliteit", width)),
            new(new("E [N/mm²]", width)),
            new(new("s.g. [kg/m³]", width)),
            new(new("*f~y~* [N/mm²]", width)),
            new(new("*f~u~* [N/mm²]", width))
        ];

        // Rijen
        foreach (var staal in staalMaterialen ?? Enumerable.Empty<StaalContext>())
        {
            table.Rows.Add([
                new(staal.Type.ToString(), width),
                new(staal.Naam, width),
                new(staal.E.ToString("0", CultureInfo.InvariantCulture), width),
                new(staal.SoortelijkGewicht.ToString("0", CultureInfo.InvariantCulture), width),
                new(staal.Fy.ToString("0", CultureInfo.InvariantCulture), width),
                new(staal.Fu.ToString("0", CultureInfo.InvariantCulture), width)
            ]);
        }

        return table;
    }

    /// <summary>
    /// Genereert een tabel met hout-specifieke eigenschappen
    /// </summary>
    /// <param name="houtMaterialen">Lijst van HoutContext materialen</param>
    /// <returns>TableContent voor MigraDoc/HTML export</returns>
    public static TableContent GetTabelMateriaalEigenschappen(IEnumerable<HoutContext> houtMaterialen)
    {
        var table = new TableContent
        {
            LayoutOnly = false,
            IsPivotTable = false,
            HideHeaders = false
        };

        string width = "30mm";

        // Headers met hout-specifieke kolommen
        table.Headers =
        [
            new(new("type", width)),
            new(new("kwaliteit", width)),
            new(new("E [N/mm²]", width)),
            new(new("s.g. [kg/m³]", width)),
            new(new("*f~m,k~* [N/mm²]", width)),
            new(new("*f~v,k~* [N/mm²]", width)),
            new(new("*f~c,0,k~* [N/mm²]", width))
        ];

        // Rijen
        foreach (var hout in houtMaterialen ?? Enumerable.Empty<HoutContext>())
        {
            table.Rows.Add([
                new(hout.Type.ToString(), width),
                new(hout.Naam, width),
                new(hout.E.ToString("0", CultureInfo.InvariantCulture), width),
                new(hout.SoortelijkGewicht.ToString("0", CultureInfo.InvariantCulture), width),
                new(hout.Fmk.ToString("0", CultureInfo.InvariantCulture), width),
                new(hout.Fvk.ToString("0", CultureInfo.InvariantCulture), width),
                new(hout.Fc0k.ToString("0", CultureInfo.InvariantCulture), width)
            ]);
        }

        return table;
    }

    /// <summary>
    /// Genereert een tabel met beton-specifieke eigenschappen
    /// </summary>
    /// <param name="betonMaterialen">Lijst van BetonContext materialen</param>
    /// <returns>TableContent voor MigraDoc/HTML export</returns>
    public static TableContent GetTabelMateriaalEigenschappen(IEnumerable<BetonContext> betonMaterialen)
    {
        var table = new TableContent
        {
            LayoutOnly = false,
            IsPivotTable = false,
            HideHeaders = false
        };

        string width = "36mm";

        // Headers met beton-specifieke kolommen
        table.Headers =
        [
            new(new("type", width)),
            new(new("kwaliteit", width)),
            new(new("E [N/mm²]", width)),
            new(new("s.g. [kg/m³]", width)),
            new(new("*f~ck~* [N/mm²]", width)),
            new(new("betonstaal", width))
        ];

        // Rijen
        foreach (var beton in betonMaterialen ?? Enumerable.Empty<BetonContext>())
        {
            table.Rows.Add([
                new(beton.Type.ToString(), width),
                new(beton.Naam, width),
                new(beton.E.ToString("0", CultureInfo.InvariantCulture), width),
                new(beton.SoortelijkGewicht.ToString("0", CultureInfo.InvariantCulture), width),
                new(beton.Fck.ToString("0", CultureInfo.InvariantCulture), width),
                new(beton.BetonStaal.ToString(), width)
            ]);
        }

        return table;
    }
}

