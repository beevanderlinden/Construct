using Construct.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Concurrent;

namespace Construct.Application.Services
{
    /// <summary>
    /// LogoService is verantwoordelijk voor het vinden van logo's op basis van e-mailadressen of domeinen.
    /// Primaire functionaliteit is het zoeken naar logo's in de /wwwroot/images/logos/ map.
    /// </summary>
    /// <param name="env"></param>
    public class LogoService(IWebHostEnvironment env) : ILogoService
    {
        private readonly ConcurrentDictionary<string, string> _cache = new();

        public string FindLogoByEmail(string email)
        {
            var domain = email.Split('@').LastOrDefault()?.ToLowerInvariant() ?? "";
            return FindLogoByDomain(domain);
        }

        /// <summary>
        /// Zoek een logo op basis van een domein.
        /// Bijvoorbeeld: als het domein "ah.nl" is, dan zoekt het naar bestanden als "ah.nl.png", "ah.nl.jpg", etc.
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public string FindLogoByDomain(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return "/images/logos/defaultlogo.png";

            return _cache.GetOrAdd(domain, d =>
            {
                var folder = Path.Combine(env.WebRootPath, "images", "logos");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var files = Directory.EnumerateFiles(folder, $"{d}.*")
                    .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                             || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                             || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (files.Any())
                {
                    return "/images/logos/" + Path.GetFileName(files.First());
                }

                return "/images/logos/defaultlogo.png";
            });
        }

        /// <summary>
        /// Leeg de interne cache van logo's.
        /// Noodzakelijk na het uploaden van nieuwe logo's om ervoor te zorgen dat de cache up-to-date is.
        /// Door deze methode aan te roepen, worden alle eerder gevonden logo's verwijderd uit de cache,
        /// zodat nieuwe logo's kunnen worden gevonden.
        /// </summary>
        public void ClearCache() => _cache.Clear();
    }
}
