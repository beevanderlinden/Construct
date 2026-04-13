using Construct.Domain.Entities;
using CommonLibrary.Models;
using System.Collections.Generic;
using System.Linq;

namespace Construct.Domain.Common
{
    /// <summary>
    /// Helper klasse om alle entity-referenties na deserialisatie te valideren.
    /// Biedt diagnostische informatie over welke referenties ontbreken of NULL zijn.
    /// </summary>
    public class ReferenceValidationHelper
    {
        /// <summary>
        /// Validatie-resultaat voor een enkele referentie
        /// </summary>
        public class ReferenceCheckResult
        {
            public string EntityName { get; set; } = "";
            public string ReferenceType { get; set; } = "";
            public Guid? ReferenceId { get; set; }
            public bool IsValid { get; set; }
            public string? ErrorMessage { get; set; }
        }

        /// <summary>
        /// Validatie-rapport voor het hele project
        /// </summary>
        public class ValidationReport
        {
            public int TotalAssemblages { get; set; }
            public int TotalMaterials { get; set; }
            public int ValidMaterialReferences { get; set; }
            public int InvalidMaterialReferences { get; set; }
            public int ValidAssemblageReferences { get; set; }
            public int InvalidAssemblageReferences { get; set; }
            public List<ReferenceCheckResult> Issues { get; set; } = [];
            public bool IsHealthy => InvalidMaterialReferences == 0 && InvalidAssemblageReferences == 0;
        }

        /// <summary>
        /// Valideert alle referenties in het project na deserialisatie.
        /// Retourneert een gedetailleerd rapport met eventuele issues.
        /// </summary>
        public static ValidationReport ValidateProjectReferences(ProjectEntity project)
        {
            var report = new ValidationReport
            {
                TotalAssemblages = project.Assemblages.Count,
                TotalMaterials = project.Materialen.Count,
            };

            if (project.Assemblages == null || project.Materialen == null)
            {
                report.Issues.Add(new ReferenceCheckResult
                {
                    EntityName = "ProjectEntity",
                    ReferenceType = "Structure",
                    IsValid = false,
                    ErrorMessage = "Assemblages of Materialen is NULL"
                });
                return report;
            }

            // Valideer materiaal-referenties in alle assemblages
            foreach (var assemblage in project.Assemblages)
            {
                ValidateMaterialReference(assemblage, project, report);
            }

            // Valideer trap-referenties in Bordes entities
            foreach (var bordes in project.Assemblages.OfType<BordesEntity>())
            {
                ValidateBordesReferences(bordes, project, report);
            }

            return report;
        }

        /// <summary>
        /// Valideert materiaal-referentie voor een assemblage
        /// </summary>
        private static void ValidateMaterialReference(AssemblageEntity assemblage, ProjectEntity project, ValidationReport report)
        {
            if (assemblage.MateriaalId.HasValue)
            {
                if (assemblage.Materiaal == null)
                {
                    report.InvalidMaterialReferences++;
                    report.Issues.Add(new ReferenceCheckResult
                    {
                        EntityName = $"{assemblage.GetType().Name} '{assemblage.Merk}'",
                        ReferenceType = "Materiaal",
                        ReferenceId = assemblage.MateriaalId,
                        IsValid = false,
                        ErrorMessage = $"MateriaalId={assemblage.MateriaalId} maar Materiaal is NULL"
                    });
                }
                else if (!project.Materialen.ContainsKey(assemblage.MateriaalId.Value))
                {
                    report.InvalidMaterialReferences++;
                    report.Issues.Add(new ReferenceCheckResult
                    {
                        EntityName = $"{assemblage.GetType().Name} '{assemblage.Merk}'",
                        ReferenceType = "Materiaal",
                        ReferenceId = assemblage.MateriaalId,
                        IsValid = false,
                        ErrorMessage = $"MateriaalId={assemblage.MateriaalId} niet gevonden in Materialen Dictionary"
                    });
                }
                else
                {
                    report.ValidMaterialReferences++;
                }
            }
        }

        /// <summary>
        /// Valideert trap-referenties in een BordesEntity
        /// </summary>
        private static void ValidateBordesReferences(BordesEntity bordes, ProjectEntity project, ValidationReport report)
        {
            // Valideer Trap1 referentie
            if (bordes.Trap1?.AansluitendElementId.HasValue == true)
            {
                if (bordes.Trap1.AansluitendElement == null)
                {
                    report.InvalidAssemblageReferences++;
                    report.Issues.Add(new ReferenceCheckResult
                    {
                        EntityName = $"BordesEntity '{bordes.Merk}' - Trap1",
                        ReferenceType = "AansluitendElement",
                        ReferenceId = bordes.Trap1.AansluitendElementId,
                        IsValid = false,
                        ErrorMessage = $"AansluitendElementId={bordes.Trap1.AansluitendElementId} maar AansluitendElement is NULL"
                    });
                }
                else if (!project.Assemblages.Any(a => a.Id == bordes.Trap1.AansluitendElementId))
                {
                    report.InvalidAssemblageReferences++;
                    report.Issues.Add(new ReferenceCheckResult
                    {
                        EntityName = $"BordesEntity '{bordes.Merk}' - Trap1",
                        ReferenceType = "AansluitendElement",
                        ReferenceId = bordes.Trap1.AansluitendElementId,
                        IsValid = false,
                        ErrorMessage = $"AansluitendElementId={bordes.Trap1.AansluitendElementId} niet gevonden in Assemblages"
                    });
                }
                else
                {
                    report.ValidAssemblageReferences++;
                }
            }

            // Valideer Trap2 referentie
            if (bordes.Trap2?.AansluitendElementId.HasValue == true)
            {
                if (bordes.Trap2.AansluitendElement == null)
                {
                    report.InvalidAssemblageReferences++;
                    report.Issues.Add(new ReferenceCheckResult
                    {
                        EntityName = $"BordesEntity '{bordes.Merk}' - Trap2",
                        ReferenceType = "AansluitendElement",
                        ReferenceId = bordes.Trap2.AansluitendElementId,
                        IsValid = false,
                        ErrorMessage = $"AansluitendElementId={bordes.Trap2.AansluitendElementId} maar AansluitendElement is NULL"
                    });
                }
                else if (!project.Assemblages.Any(a => a.Id == bordes.Trap2.AansluitendElementId))
                {
                    report.InvalidAssemblageReferences++;
                    report.Issues.Add(new ReferenceCheckResult
                    {
                        EntityName = $"BordesEntity '{bordes.Merk}' - Trap2",
                        ReferenceType = "AansluitendElement",
                        ReferenceId = bordes.Trap2.AansluitendElementId,
                        IsValid = false,
                        ErrorMessage = $"AansluitendElementId={bordes.Trap2.AansluitendElementId} niet gevonden in Assemblages"
                    });
                }
                else
                {
                    report.ValidAssemblageReferences++;
                }
            }
        }

        /// <summary>
        /// Geeft een user-friendly samenvatting van het validatie-rapport
        /// Gebruikt plain ASCII voor Debug Console compatibility
        /// </summary>
        public static string GetSummary(ValidationReport report)
        {
            var lines = new List<string>
            {
                $"Reference Validation Report",
                $"===============================",
                $"Total Assemblages: {report.TotalAssemblages}",
                $"Total Materials: {report.TotalMaterials}",
                $"",
                $"[OK] Valid Material References: {report.ValidMaterialReferences}",
                $"[!] Invalid Material References: {report.InvalidMaterialReferences}",
                $"",
                $"[OK] Valid Assemblage References: {report.ValidAssemblageReferences}",
                $"[!] Invalid Assemblage References: {report.InvalidAssemblageReferences}",
                $"",
                report.IsHealthy 
                    ? "[SUCCESS] ALL REFERENCES OK!" 
                    : "[WARNING] ISSUES FOUND:",
            };

            if (!report.IsHealthy)
            {
                foreach (var issue in report.Issues)
                {
                    lines.Add($"  - {issue.EntityName}");
                    lines.Add($"    Type: {issue.ReferenceType}");
                    lines.Add($"    ID: {issue.ReferenceId}");
                    lines.Add($"    Error: {issue.ErrorMessage}");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Geeft een HTML-gestyled samenvatting voor UI rendering
        /// Perfecte Unicode/emoji ondersteuning
        /// </summary>
        public static string GetSummaryHtml(ValidationReport report)
        {
            var html = new System.Text.StringBuilder();
            html.AppendLine("<div style=\"font-family: monospace; line-height: 1.6; color: #333;\">");
            html.AppendLine($"<div style=\"font-weight: bold; margin-bottom: 0.5rem;\">?? Reference Validation Report</div>");
            html.AppendLine($"<div style=\"border-bottom: 1px solid #ccc; margin-bottom: 0.5rem; padding-bottom: 0.5rem;\">");
            html.AppendLine($"  Total Assemblages: {report.TotalAssemblages} | Total Materials: {report.TotalMaterials}");
            html.AppendLine($"</div>");
            
            html.AppendLine($"<div style=\"margin: 0.5rem 0;\">");
            html.AppendLine($"  ? Valid Material References: <span style=\"font-weight: bold; color: green;\">{report.ValidMaterialReferences}</span>");
            html.AppendLine($"  ? Invalid Material References: <span style=\"font-weight: bold; color: red;\">{report.InvalidMaterialReferences}</span>");
            html.AppendLine($"</div>");
            
            html.AppendLine($"<div style=\"margin: 0.5rem 0;\">");
            html.AppendLine($"  ? Valid Assemblage References: <span style=\"font-weight: bold; color: green;\">{report.ValidAssemblageReferences}</span>");
            html.AppendLine($"  ? Invalid Assemblage References: <span style=\"font-weight: bold; color: red;\">{report.InvalidAssemblageReferences}</span>");
            html.AppendLine($"</div>");
            
            if (report.IsHealthy)
            {
                html.AppendLine($"<div style=\"color: green; font-weight: bold; margin-top: 0.5rem;\">?? ALL REFERENCES OK!</div>");
            }
            else
            {
                html.AppendLine($"<div style=\"color: #d32f2f; font-weight: bold; margin-top: 0.5rem;\">?? ISSUES FOUND:</div>");
                html.AppendLine($"<div style=\"margin-left: 1rem; margin-top: 0.5rem;\">");
                
                foreach (var issue in report.Issues)
                {
                    html.AppendLine($"<div style=\"margin-bottom: 0.75rem; padding: 0.5rem; background: #fff3cd; border-left: 3px solid #ffc107;\">");
                    html.AppendLine($"  <div><strong>{issue.EntityName}</strong> ({issue.ReferenceType})</div>");
                    html.AppendLine($"  <div style=\"font-size: 0.9rem; color: #666;\">ID: {issue.ReferenceId}</div>");
                    html.AppendLine($"  <div style=\"font-size: 0.9rem; color: #d32f2f;\">?? {issue.ErrorMessage}</div>");
                    html.AppendLine($"</div>");
                }
                
                html.AppendLine($"</div>");
            }
            
            html.AppendLine("</div>");
            return html.ToString();
        }
    }
}
