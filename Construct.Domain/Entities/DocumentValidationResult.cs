using CommonLibrary;

namespace Construct.Domain.Entities
{
    /// <summary>
    /// Resultaat van document validatie met alle verzamelde berichten per assemblage
    /// </summary>
    public class DocumentValidationResult
    {
        public List<AssemblageValidation> AssemblageValidations { get; set; } = new();
        
        public bool HasWarnings => AssemblageValidations.Any(a => a.Warnings.Any());
        public bool HasErrors => AssemblageValidations.Any(a => a.Errors.Any());
        public bool IsValid => !HasErrors;
        
        public int TotalWarnings => AssemblageValidations.Sum(a => a.Warnings.Count);
        public int TotalErrors => AssemblageValidations.Sum(a => a.Errors.Count);

        /// <summary>
        /// Voeg een nieuwe assemblage validatie toe
        /// </summary>
        public AssemblageValidation AddAssemblage(string merk, string naam)
        {
            var validation = new AssemblageValidation { Merk = merk, Naam = naam };
            AssemblageValidations.Add(validation);
            return validation;
        }

        /// <summary>
        /// Geeft een samenvatting string van alle validatie resultaten
        /// </summary>
        public string GetSummary()
        {
            if (IsValid && !HasWarnings)
                return "✓ Document validatie geslaagd, geen berichten";
            
            var summary = $"Document validatie: {TotalErrors} error(s), {TotalWarnings} waarschuwing(en)";
            return summary;
        }
    }

    /// <summary>
    /// Validatie resultaat voor een specifieke assemblage
    /// </summary>
    public class AssemblageValidation
    {
        public string Merk { get; set; } = "";
        public string Naam { get; set; } = "";
        public List<Melding> Warnings { get; set; } = new();
        public List<Melding> Errors { get; set; } = new();
        
        public bool HasWarnings => Warnings.Any();
        public bool HasErrors => Errors.Any();
        public bool IsValid => !HasErrors;

        public void AddWarning(string message, string? detail = null)
        {
            var bericht = detail != null ? $"{message}\n{detail}" : message;
            Warnings.Add(new Melding(MeldingType.Waarschuwing, bericht));
        }

        public void AddError(string message, string? detail = null)
        {
            var bericht = detail != null ? $"{message}\n{detail}" : message;
            Errors.Add(new Melding(MeldingType.Error, bericht));
        }

        public void AddInfo(string message, string? detail = null)
        {
            var bericht = detail != null ? $"{message}\n{detail}" : message;
            Warnings.Add(new Melding(MeldingType.Opmerking, bericht));
        }

        /// <summary>
        /// Voeg bestaande meldingen uit een context toe
        /// </summary>
        public void AddMeldingen(IEnumerable<Melding> meldingen)
        {
            foreach (var melding in meldingen)
            {
                if (melding.Type == MeldingType.Error)
                    Errors.Add(melding);
                else
                    Warnings.Add(melding);
            }
        }

        /// <summary>
        /// Geeft een korte samenvatting string
        /// </summary>
        public string GetSummary()
        {
            if (IsValid && !HasWarnings)
                return $"{Merk}: OK";
            
            var parts = new List<string> { Merk };
            if (Errors.Any())
                parts.Add($"{Errors.Count} error(s)");
            if (Warnings.Any())
                parts.Add($"{Warnings.Count} waarschuwing(en)");
            
            return string.Join(" - ", parts);
        }
    }
}
