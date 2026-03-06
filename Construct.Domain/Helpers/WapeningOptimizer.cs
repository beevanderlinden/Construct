using Construct.Domain.Entities;
using Eurocode.BetonConstructies;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Construct.Domain.Helpers
{
    /// <summary>
    /// Helper class voor DRY wapening optimalisatie.
    /// Gebruikt door BordesEntity en SteekTrapEntity voor automatische wapening bepaling.
    /// </summary>
    public static class WapeningOptimizer
    {
        /// <summary>
        /// Bepaalt optimale plaatwapening (diameter + hoh) op basis van benodigde As.
        /// </summary>
        /// <param name="asRequired">Benodigde wapeningsdoorsnede (mm²)</param>
        /// <param name="beschikbareBreedte">Beschikbare breedte voor wapening (mm)</param>
        /// <param name="constraint">Optionele constraints (min diameter, max hoh)</param>
        /// <param name="startDiameter">Start diameter (default 6mm)</param>
        /// <param name="startHoh">Start hoh-maat (default 150mm)</param>
        /// <returns>Wapening tekst (bijv. "r8-125") en toegepaste As</returns>
        public static (string tekst, double asProvided) BepaalPlaatWapening(
            double asRequired,
            double beschikbareBreedte,
            WapeningConstraint? constraint = null,
            double startDiameter = 6,
            double startHoh = 150)
        {
            // Beschikbare diameters
            List<double> diameters = [6, 8, 10, 12, 16, 20];
            
            // Haal constraints op
            double minDiameter = constraint?.DiameterMin ?? startDiameter;
            double maxHoh = constraint?.HohMax ?? 9999;
            
            // Start bij kleinste bruikbare diameter
            var beschikbareDiameters = diameters.Where(d => d >= minDiameter).ToList();
            if (!beschikbareDiameters.Any())
                beschikbareDiameters = [minDiameter];
            
            // Begin met startDiameter en startHoh (bijv. r6-150)
            double currentDiameter = beschikbareDiameters.First();
            double currentHoh = Math.Min(startHoh, maxHoh);
            
            // Bereken As voor start configuratie
            double asProvided = BerekenAsPlaatWapening(currentDiameter, currentHoh, beschikbareBreedte);
            
            // Als al voldoende ? klaar
            if (asProvided >= asRequired)
            {
                return ($"r{currentDiameter:0}-{currentHoh:0}", asProvided);
            }
            
            // Stap 1: Bereken benodigde hoh voor huidige diameter
            double benodigdeHoh = BerekenBenodigdeHoh(asRequired, currentDiameter, beschikbareBreedte);
            
            // Stap 2: Rond af naar veelvoud van 5 (naar beneden) indien hoh >= 75mm
            if (benodigdeHoh >= 75)
            {
                benodigdeHoh = Math.Floor(benodigdeHoh / 5.0) * 5.0;
            }
            
            // Stap 3: Check of hoh binnen constraints valt
            if (benodigdeHoh >= 50 && benodigdeHoh <= maxHoh)
            {
                currentHoh = benodigdeHoh;
                asProvided = BerekenAsPlaatWapening(currentDiameter, currentHoh, beschikbareBreedte);
                
                if (asProvided >= asRequired)
                {
                    return ($"r{currentDiameter:0}-{currentHoh:0}", asProvided);
                }
            }
            
            // Stap 4: Als hoh < 100mm ? verhoog diameter en herhaal
            foreach (var diameter in beschikbareDiameters.Skip(1))
            {
                currentDiameter = diameter;
                
                // Bereken benodigde hoh voor deze diameter
                benodigdeHoh = BerekenBenodigdeHoh(asRequired, currentDiameter, beschikbareBreedte);
                
                // Rond af naar veelvoud van 5 indien >= 75mm
                if (benodigdeHoh >= 75)
                {
                    benodigdeHoh = Math.Floor(benodigdeHoh / 5.0) * 5.0;
                }
                
                // Clamp tussen 50mm en maxHoh
                currentHoh = Math.Max(50, Math.Min(benodigdeHoh, maxHoh));
                
                asProvided = BerekenAsPlaatWapening(currentDiameter, currentHoh, beschikbareBreedte);
                
                if (asProvided >= asRequired)
                {
                    return ($"r{currentDiameter:0}-{currentHoh:0}", asProvided);
                }
                
                // Als hoh >= 100mm en nog steeds onvoldoende ? probeer volgende diameter
                if (currentHoh >= 100)
                    continue;
                
                // Hoh < 100mm maar toch onvoldoende ? probeer met kleinere hoh (min 50mm)
                for (double hoh = currentHoh - 5; hoh >= 50; hoh -= 5)
                {
                    asProvided = BerekenAsPlaatWapening(currentDiameter, hoh, beschikbareBreedte);
                    if (asProvided >= asRequired)
                    {
                        return ($"r{currentDiameter:0}-{hoh:0}", asProvided);
                    }
                }
            }
            
            // Fallback: gebruik grootste diameter met minimale hoh
            var maxDiameter = beschikbareDiameters.Last();
            currentHoh = 50; // Minimale hoh
            asProvided = BerekenAsPlaatWapening(maxDiameter, currentHoh, beschikbareBreedte);
            
            Console.WriteLine($"?? Geen optimale wapening gevonden. Fallback: r{maxDiameter:0}-{currentHoh:0} (As={asProvided:0}mm², benodigd={asRequired:0}mm²)");
            return ($"r{maxDiameter:0}-{currentHoh:0}", asProvided);
        }
        
        /// <summary>
        /// Berekent totale As voor plaatwapening met gegeven diameter en hoh-maat.
        /// </summary>
        private static double BerekenAsPlaatWapening(double diameter, double hoh, double breedte)
        {
            // Aantal staven = breedte / hoh (rond naar boven voor veiligheid)
            int aantalStaven = (int)Math.Ceiling(breedte / hoh);
            
            // As per staaf
            double asPerStaaf = Math.PI * Math.Pow(diameter, 2) / 4.0;
            
            // Totale As over referentie breedte (meestal 1000mm)
            return asPerStaaf * (1000.0 / hoh);
        }
        
        /// <summary>
        /// Berekent benodigde hoh-maat voor gegeven As en diameter.
        /// </summary>
        private static double BerekenBenodigdeHoh(double asRequired, double diameter, double breedte)
        {
            // As per staaf
            double asPerStaaf = Math.PI * Math.Pow(diameter, 2) / 4.0;
            
            // Benodigde hoh = As_staaf * 1000 / As_required
            double hoh = (asPerStaaf * 1000.0) / asRequired;
            
            return hoh;
        }
        
        /// <summary>
        /// Parseert wapening tekst zoals "r6-150" naar (diameter, hoh).
        /// </summary>
        public static (double diameter, double hoh)? ParseWapeningTekst(string? tekst)
        {
            if (string.IsNullOrWhiteSpace(tekst))
                return null;

            // Format: "r6-150" of "Ø8-200"
            var match = System.Text.RegularExpressions.Regex.Match(
                tekst,
                @"[rØø](\d+(?:[.,]\d+)?)-(\d+(?:[.,]\d+)?)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success)
            {
                string diamStr = match.Groups[1].Value.Replace(',', '.');
                string hohStr = match.Groups[2].Value.Replace(',', '.');
                double diameter = double.Parse(diamStr, CultureInfo.InvariantCulture);
                double hoh = double.Parse(hohStr, CultureInfo.InvariantCulture);
                return (diameter, hoh);
            }

            return null;
        }
        
        /// <summary>
        /// Maakt wapening tekst van aantal en diameter: "3r12".
        /// </summary>
        public static string MaakBijlegTekst(int aantal, double diameter)
        {
            string diamStr = diameter == Math.Floor(diameter)
                ? diameter.ToString("0")
                : diameter.ToString("0.#", CultureInfo.InvariantCulture);
            return $"{aantal}r{diamStr}";
        }
        
        /// <summary>
        /// Berekent As voor gegeven aantal staven en diameter (mm²).
        /// </summary>
        public static double BerekenAs(int aantal, double diameterMm)
        {
            double perStaaf = Math.PI * Math.Pow(diameterMm, 2) / 4.0;
            return aantal * perStaaf;
        }
        
        /// <summary>
        /// Bepaalt optimale plaatwapening met FIXED hoh-maat (alleen diameter aanpassen).
        /// Gebruikt voor basisstrook bordes waar hoh niet mag wijzigen.
        /// </summary>
        /// <param name="asRequired">Benodigde wapeningsdoorsnede (mm²)</param>
        /// <param name="fixedHoh">Vaste hoh-maat (mm)</param>
        /// <param name="currentDiameter">Huidige diameter (mm)</param>
        /// <param name="constraint">Optionele constraints</param>
        /// <param name="herberekening">Callback voor AsRequired herberekening bij diameter wijziging (ontvangt nieuwe diameter, geeft nieuwe AsRequired)</param>
        /// <returns>Wapening tekst en toegepaste As</returns>
        public static (string tekst, double asProvided) BepaalPlaatWapeningFixedHoh(
            double asRequired,
            double fixedHoh,
            double currentDiameter,
            WapeningConstraint? constraint = null,
            Func<double, double>? herberekening = null)
        {
            // Beschikbare diameters
            List<double> diameters = [6, 8, 10, 12];
            
            // Haal constraints op
            double minDiameter = constraint?.DiameterMin ?? 6.0;
            
            // Filter diameters >= minDiameter en > currentDiameter
            var beschikbareDiameters = diameters
                .Where(d => d >= minDiameter && d > currentDiameter)
                .ToList();
            
            if (!beschikbareDiameters.Any())
            {
                // Geen hogere diameter beschikbaar ? gebruik huidige
                var asProvided = BerekenAsPlaatWapening(currentDiameter, fixedHoh, 1000);
                Console.WriteLine($"?? Geen hogere diameter beschikbaar dan Ø{currentDiameter}. AsProvided={asProvided:0}mm², AsRequired={asRequired:0}mm²");
                return ($"r{currentDiameter:0}-{fixedHoh:0}", asProvided);
            }
            
            // Probeer elke diameter (klein naar groot)
            foreach (var diameter in beschikbareDiameters)
            {
                // Bereken As voor deze diameter
                var asProvided = BerekenAsPlaatWapening(diameter, fixedHoh, 1000);
                
                // Als herberekening callback aanwezig: herbereken AsRequired
                var asRequiredAangepast = asRequired;
                if (herberekening != null)
                {
                    asRequiredAangepast = herberekening(diameter);
                    Console.WriteLine($"[HERBEREKEN] AsRequired aangepast van {asRequired:0} ? {asRequiredAangepast:0}mm² (Ø{diameter}, hoh={fixedHoh})");
                }
                
                // Check of voldoende
                if (asProvided >= asRequiredAangepast)
                {
                    Console.WriteLine($"? Wapening gevonden: r{diameter:0}-{fixedHoh:0} (As={asProvided:0}mm² >= {asRequiredAangepast:0}mm²)");
                    return ($"r{diameter:0}-{fixedHoh:0}", asProvided);
                }
            }
            
            // Fallback: gebruik grootste diameter
            var maxDiameter = beschikbareDiameters.Last();
            var asFallback = BerekenAsPlaatWapening(maxDiameter, fixedHoh, 1000);
            Console.WriteLine($"?? Geen voldoende wapening gevonden. Fallback: r{maxDiameter:0}-{fixedHoh:0} (As={asFallback:0}mm² < {asRequired:0}mm²)");
            return ($"r{maxDiameter:0}-{fixedHoh:0}", asFallback);
        }
        
        /// <summary>
        /// Bepaalt bijlegwapening (aantal staven + diameter) voor versterkte strook.
        /// Format: "3r12" (3 staven van Ø12).
        /// </summary>
        /// <param name="asRequired">Benodigde extra wapeningsdoorsnede (mm²)</param>
        /// <param name="strookBreedte">Breedte van versterkte strook (mm)</param>
        /// <param name="constraint">Optionele constraints (min aantal, min diameter)</param>
        /// <param name="dGemiddeld">Gemiddelde diameter basiswapening (voor nuttige hoogte correctie)</param>
        /// <param name="dNuttig">Huidige nuttige hoogte (mm)</param>
        /// <returns>Bijlegwapening tekst ("3r12"), aantal, diameter, AsProvided</returns>
        public static (string tekst, int aantal, double diameter, double asProvided) BepaalBijlegWapening(
            double asRequired,
            double strookBreedte,
            WapeningConstraint? constraint = null,
            double dGemiddeld = 6,
            double dNuttig = 170)
        {
            // Beschikbare diameters voor bijleg
            List<double> diameters = [8, 10, 12, 16, 20, 25];
            
            // Haal constraints op
            double minDiameter = constraint?.DiameterMin ?? 8.0;
            int minAantal = constraint?.AantalMin ?? 0;
            
            // Bereken automatisch aantal staven op basis van strookbreedte
            // Vuistregel: 1 staaf per 100mm
            int aantalBerekend = Math.Max(2, (int)(strookBreedte / 100.0));
            
            // Start met berekend aantal
            int aantalStaven = Math.Max(aantalBerekend, minAantal);
            
            // Probeer elke diameter
            foreach (var d in diameters.Where(d => d >= minDiameter))
            {
                // Bereken As voor dit aantal + diameter
                var asTest = BerekenAs(aantalStaven, d);
                
                // Correctie voor afname nuttige hoogte (grotere diameter ? kleinere z)
                double verhogingFactor = 1.0;
                if (d > dGemiddeld && dNuttig > 0)
                {
                    // d neemt af met ((d_nieuw - d_gemiddeld) / 2)
                    double dNieuw = dNuttig - ((d - dGemiddeld) / 2.0);
                    verhogingFactor = dNuttig / dNieuw;
                }
                
                double asRequiredAangepast = asRequired * verhogingFactor;
                
                if (asTest >= asRequiredAangepast)
                {
                    Console.WriteLine($"? Bijlegwapening: {aantalStaven}r{d:0} (As={asTest:0}mm² >= {asRequiredAangepast:0}mm², factor={verhogingFactor:0.###})");
                    return (MaakBijlegTekst(aantalStaven, d), aantalStaven, d, asTest);
                }
            }
            
            // Geen oplossing met automatisch aantal ? verhoog aantal staven
            var maxDiameter = diameters.Where(d => d >= minDiameter).LastOrDefault();
            if (maxDiameter == 0) maxDiameter = diameters.Last();
            
            aantalStaven = (int)Math.Ceiling(asRequired / BerekenAs(1, maxDiameter));
            aantalStaven = Math.Max(aantalStaven, minAantal);
            
            var asFinal = BerekenAs(aantalStaven, maxDiameter);
            Console.WriteLine($"?? Bijlegwapening verhoogd aantal: {aantalStaven}r{maxDiameter:0} (As={asFinal:0}mm²)");
            return (MaakBijlegTekst(aantalStaven, maxDiameter), aantalStaven, maxDiameter, asFinal);
        }
    }
}
