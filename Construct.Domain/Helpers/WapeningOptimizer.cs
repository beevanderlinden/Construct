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
        /// Centrale methode die de wapening-tekst bepaalt op basis van de <see cref="WapeningAfhandelingEnum"/> instelling.
        /// </summary>
        /// <param name="huidigeTekst">De huidige wapening-tekst (invoer gebruiker, mag null/leeg zijn).</param>
        /// <param name="asRequired">Benodigde wapeningsdoorsnede (mm²).</param>
        /// <param name="wap">De <see cref="WapeningContext"/> met referentielengte en ondergrens.</param>
        /// <param name="instelling">De project-brede wapening-afhandeling instelling.</param>
        /// <returns>Nieuwe wapening-tekst en toegepaste As.</returns>
        public static (string tekst, double asProvided) BepaalWapeningMetInstelling(
            string? huidigeTekst,
            double asRequired,
            WapeningContext wap,
            WapeningAfhandelingEnum instelling)
        {
            bool invoerLeeg = string.IsNullOrWhiteSpace(huidigeTekst);

            // Altijd automatisch bepalen als de invoer leeg is
            if (invoerLeeg)
                return BepaalPlaatWapening(asRequired, wap);

            var huidig = ParseWapeningTekst(huidigeTekst);
            double asHuidig = huidig.HasValue
                ? BerekenAsPlaatWapeningPublic(huidig.Value.diameter, huidig.Value.hoh, wap.ReferentieLengte)
                : 0;

            switch (instelling)
            {
                case WapeningAfhandelingEnum.Gebruiker:
                    // Geen aanpassing – retourneer invoer ongewijzigd
                    return (huidigeTekst!, asHuidig);

                case WapeningAfhandelingEnum.AlleenVerhogen:
                    // Alleen verhogen als onvoldoende
                    if (asHuidig >= asRequired)
                        return (huidigeTekst!, asHuidig);
                    return BepaalPlaatWapening(asRequired, wap);

                case WapeningAfhandelingEnum.Optimaliseer:
                default:
                    // Altijd optimaal herberekenen (kan ook verlagen)
                    return BepaalPlaatWapening(asRequired, wap);
            }
        }

        /// <summary>
        /// Publieke wrapper voor het berekenen van As uit diameter en hoh (per meter breedte).
        /// </summary>
        public static double BerekenAsPlaatWapeningPublic(double diameter, double hoh, double breedte = 1000)
            => BerekenAsPlaatWapening(diameter, hoh, breedte);

        public static (string tekst, double asProvided) BepaalPlaatWapening(double asRequired, WapeningContext wap)
        {
            double startDiameter = 6;
            double startHoh = 150;
            var ondergrens = ParseWapeningTekst(wap.TekstOndergrens);
            if (ondergrens != null)
            {
                startHoh = ondergrens.Value.hoh;
                startDiameter = ondergrens.Value.diameter;
            }
            return BepaalPlaatWapening(asRequired, wap.ReferentieLengte, startDiameter, startHoh);
        }


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
            double startDiameter = 6,
            double startHoh = 150)
        {
            // Beschikbare diameters
            List<double> diameters = [6, 8, 10, 12, 16, 20, 25, 32, 40];
            
            // Haal constraints op
            //double minDiameter = constraint?.DiameterMin ?? startDiameter;
            //double maxHoh = constraint?.HohMax ?? 9999;
            
            // Start bij kleinste bruikbare diameter
            var beschikbareDiameters = diameters.Where(d => d >= startDiameter).ToList();
            if (!beschikbareDiameters.Any())
                beschikbareDiameters = [startDiameter];
            
            // Begin met startDiameter en startHoh (bijv. r6-150)
            double currentDiameter = beschikbareDiameters.First();
            double currentHoh = startHoh;
            
            // Bereken As voor start configuratie
            double asProvided = BerekenAsPlaatWapening(currentDiameter, currentHoh, beschikbareBreedte);
            
            // Als al voldoende ? klaar
            if (asProvided >= asRequired * 1.01) // neem 1% meer om afronding te voorkomen
            {
                return ($"r{currentDiameter:0}-{currentHoh:0}", asProvided);
            }
            
            // Stap 1: Bereken benodigde hoh voor huidige diameter
            double benodigdeHoh = BerekenBenodigdeHoh(asRequired * 1.01, currentDiameter, beschikbareBreedte);
            
            // Stap 2: Rond af naar veelvoud van 5 (naar beneden) indien hoh >= 75mm
            if (benodigdeHoh >= 75)
            {
                benodigdeHoh = Math.Floor(benodigdeHoh / 5.0) * 5.0;
                return ($"r{currentDiameter:0}-{benodigdeHoh:0}", asProvided);
            }
            
                       
            // Stap 3: verhoog diameter en herhaal
            foreach (var diameter in beschikbareDiameters.Skip(1))
            {
                currentDiameter = diameter;
                currentHoh = startHoh;
                
                // Bereken benodigde hoh voor deze diameter
                benodigdeHoh = BerekenBenodigdeHoh(asRequired * 1.05, currentDiameter, beschikbareBreedte); // bij verhoging diameter neem 105%
                
                // Rond af naar veelvoud van 5 indien >= 75mm
                if (benodigdeHoh >= 75)
                {
                    benodigdeHoh = Math.Floor(benodigdeHoh / 5.0) * 5.0;
                    return ($"r{currentDiameter:0}-{benodigdeHoh:0}", asProvided);
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
        /// Parseert plaatwapening tekst naar (diameter, hoh).
        /// Ondersteunt formaten: "r6-150", "Ø8-200", "d8-100", "8-150" (prefix optioneel).
        /// </summary>
        /// <param name="tekst">Wapening tekst (bijv. "r8-150", "8-100", "Ø10-125")</param>
        /// <returns>(diameter, hoh) of null als parsing faalt</returns>
        public static (double diameter, double hoh)? ParseWapeningTekst(string? tekst)
        {
            if (string.IsNullOrWhiteSpace(tekst))
                return null;

            // Format: "r6-150", "Ø8-200", "d8-100", of "8-150" (prefix optioneel)
            // Regex: optionele prefix [rØødD], gevolgd door diameter-hoh
            var match = System.Text.RegularExpressions.Regex.Match(
                tekst,
                @"[rØødD]?(\d+(?:[.,]\d+)?)-(\d+(?:[.,]\d+)?)",
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
        /// Parseert bijlegwapening tekst naar (aantal, diameter).
        /// Ondersteunt formaten: "3r12", "5Ø16", "2d10".
        /// </summary>
        /// <param name="tekst">Bijlegwapening tekst (bijv. "3r12", "5Ø16")</param>
        /// <returns>(aantal, diameter) of null als parsing faalt</returns>
        public static (int aantal, double diameter)? ParseBijlegWapening(string? tekst)
        {
            if (string.IsNullOrWhiteSpace(tekst))
                return null;

            // Format: "3r12", "5Ø16", "2d10"
            // Regex: aantal, prefix [rØødD], diameter
            var match = System.Text.RegularExpressions.Regex.Match(
                tekst,
                @"(\d+)[rØødD](\d+(?:[.,]\d+)?)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success)
            {
                int aantal = int.Parse(match.Groups[1].Value);
                string diamStr = match.Groups[2].Value.Replace(',', '.');
                double diameter = double.Parse(diamStr, CultureInfo.InvariantCulture);
                return (aantal, diameter);
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
            Func<double, double>? herberekening = null)
        {
            // Beschikbare diameters
            List<double> diameters = [6, 8, 10, 12, 16, 20, 25, 32];
            
            // Haal constraints op
            double minDiameter = currentDiameter;
            
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
            double dGemiddeld = 6,
            double dMin = 8,
            int nMin = 2,
            double dNuttig = 170)
        {
            // Beschikbare diameters voor bijleg
            List<double> diameters = [8, 10, 12, 16, 20, 25];

            // Haal constraints op
            double minDiameter = dMin;
            int minAantal = nMin;
            
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
        
        /// <summary>
        /// Verschaalt bestaande plaatwapening met een factor (bijv. 1.1 voor 110%).
        /// Probeert eerst hart-op-hart afstand te verkleinen tot minimaal 75mm,
        /// daarna wordt diameter verhoogd.
        /// </summary>
        /// <param name="huidigeWapening">Huidige wapening tekst (bijv. "r8-150")</param>
        /// <param name="factor">Schaalfactor (bijv. 1.1 voor 110%, 1.5 voor 150%)</param>
        /// <param name="beschikbareBreedte">Beschikbare breedte (mm)</param>
        /// <param name="constraint">Optionele constraints</param>
        /// <returns>Nieuwe wapening tekst en toegepaste As</returns>
        public static (string tekst, double asProvided) VerschaalPlaatWapening(
            string huidigeWapening,
            double factor,
            double beschikbareBreedte = 1000,
            string ondergrens = "6-150")
        {
            // Parse huidige wapening
            var parsed = ParseWapeningTekst(huidigeWapening);
            if (!parsed.HasValue)
            {
                Console.WriteLine($"⚠️ Kon wapening '{huidigeWapening}' niet parsen");
                return (huidigeWapening, 0);
            }
            
            var (huidigeDiameter, huidigeHoh) = parsed.Value;
            
            // Bereken huidige As
            double huidigeAs = BerekenAsPlaatWapening(huidigeDiameter, huidigeHoh, beschikbareBreedte);
            
            // Bereken benodigde As
            double benodigdeAs = huidigeAs * factor;
            
            Console.WriteLine($"📊 VerschaalPlaatWapening: {huidigeWapening} × {factor:P0} → benodigd As={benodigdeAs:0}mm² (was {huidigeAs:0}mm²)");
            
            // Beschikbare diameters
            List<double> diameters = [6, 8, 10, 12, 16, 20];

            var ondergrensParsed = ParseWapeningTekst(ondergrens);

            double minDiameter = ondergrensParsed?.diameter ?? 6;
            double minHoh = 75; // Minimale hart-op-hart afstand
            
            // ===================================
            // STAP 1: Probeer hoh te verkleinen
            // ===================================
            double nieuweHoh = BerekenBenodigdeHoh(benodigdeAs, huidigeDiameter, beschikbareBreedte);
            
            // Rond af naar veelvoud van 5 (naar beneden)
            nieuweHoh = Math.Floor(nieuweHoh / 5.0) * 5.0;
            
            if (nieuweHoh >= minHoh)
            {
                // Hoh verkleinen is voldoende!
                double asProvided = BerekenAsPlaatWapening(huidigeDiameter, nieuweHoh, beschikbareBreedte);
                Console.WriteLine($"✅ Hoh verkleinen: r{huidigeDiameter:0}-{nieuweHoh:0} (As={asProvided:0}mm²)");
                return ($"r{huidigeDiameter:0}-{nieuweHoh:0}", asProvided);
            }
            
            // ===================================
            // STAP 2: Hoh te klein → verhoog diameter
            // ===================================
            var beschikbareDiameters = diameters
                .Where(d => d > huidigeDiameter && d >= minDiameter)
                .ToList();
            
            if (!beschikbareDiameters.Any())
            {
                // Geen grotere diameter beschikbaar → gebruik huidige diameter met minimale hoh
                double asProvided = BerekenAsPlaatWapening(huidigeDiameter, minHoh, beschikbareBreedte);
                Console.WriteLine($"⚠️ Geen grotere diameter beschikbaar. Gebruik minimale hoh: r{huidigeDiameter:0}-{minHoh:0} (As={asProvided:0}mm²)");
                return ($"r{huidigeDiameter:0}-{minHoh:0}", asProvided);
            }
            
            // Probeer elke grotere diameter
            foreach (var diameter in beschikbareDiameters)
            {
                // Bereken benodigde hoh voor deze diameter
                double benodigdeHoh = BerekenBenodigdeHoh(benodigdeAs, diameter, beschikbareBreedte);
                
                // Rond af naar veelvoud van 5
                benodigdeHoh = Math.Floor(benodigdeHoh / 5.0) * 5.0;
                
                // Zorg dat hoh >= minHoh
                benodigdeHoh = Math.Max(benodigdeHoh, minHoh);
                
                double asProvided = BerekenAsPlaatWapening(diameter, benodigdeHoh, beschikbareBreedte);
                
                // Check of voldoende
                if (asProvided >= benodigdeAs)
                {
                    Console.WriteLine($"✅ Diameter verhoogd: r{diameter:0}-{benodigdeHoh:0} (As={asProvided:0}mm²)");
                    return ($"r{diameter:0}-{benodigdeHoh:0}", asProvided);
                }
            }
            
            // Fallback: gebruik grootste diameter met minimale hoh
            var maxDiameter = beschikbareDiameters.Last();
            double asFallback = BerekenAsPlaatWapening(maxDiameter, minHoh, beschikbareBreedte);
            Console.WriteLine($"⚠️ Fallback: r{maxDiameter:0}-{minHoh:0} (As={asFallback:0}mm² < {benodigdeAs:0}mm²)");
            return ($"r{maxDiameter:0}-{minHoh:0}", asFallback);
        }

        /// <summary>
        /// Past ondergrens toe op berekende wapening.
        /// Vergelijkt berekende wapening met ondergrens en neemt het maximum van beide.
        /// - Diameter: neem maximum van (berekend vs ondergrens)
        /// - Hart-op-hart: neem minimum van (berekend vs ondergrens)
        /// </summary>
        /// <param name="berekendeWapening">Berekende wapening tekst (bijv. "r6-150")</param>
        /// <param name="ondergrens">Ondergrens wapening tekst (bijv. "r8-100")</param>
        /// <returns>Definitieve wapening tekst na toepassing ondergrens</returns>
        public static string PasOndergrensToe(string? berekendeWapening, string? ondergrens)
        {
            // Als geen ondergrens, gebruik berekende waarde
            if (string.IsNullOrWhiteSpace(ondergrens))
                return berekendeWapening ?? "r6-150";

            // Als geen berekende waarde, gebruik ondergrens
            if (string.IsNullOrWhiteSpace(berekendeWapening))
                return ondergrens;

            // Parse beide waarden
            var parsedBerekend = ParseWapeningTekst(berekendeWapening);
            var parsedOndergrens = ParseWapeningTekst(ondergrens);

            if (!parsedBerekend.HasValue)
                return ondergrens; // Berekend ongeldig → gebruik ondergrens

            if (!parsedOndergrens.HasValue)
                return berekendeWapening; // Ondergrens ongeldig → gebruik berekend

            var (diameterBerekend, hohBerekend) = parsedBerekend.Value;
            var (diameterOndergrens, hohOndergrens) = parsedOndergrens.Value;

            // Neem maximum diameter (zwaarste wapening)
            double definitieveDiameter = Math.Max(diameterBerekend, diameterOndergrens);

            // Neem minimum hoh (meeste staven)
            double definitieveHoh = Math.Min(hohBerekend, hohOndergrens);

            string resultaat = $"r{definitieveDiameter:0}-{definitieveHoh:0}";

            // Logging voor debugging
            if (definitieveDiameter > diameterBerekend || definitieveHoh < hohBerekend)
            {
                Console.WriteLine($"📌 Ondergrens toegepast: {berekendeWapening} → {resultaat} (ondergrens: {ondergrens})");
            }

            return resultaat;
        }
    }


}

