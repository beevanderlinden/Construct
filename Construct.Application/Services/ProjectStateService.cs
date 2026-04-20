using Construct.Application.Factories;
using Construct.Application.Interfaces;
using Construct.Domain;
using Construct.Domain.Entities;
using Construct.Domain.Common;
using CommonLibrary.Models;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Construct.Application.Services
{
    public class ProjectStateService : IProjectStateService
    {
        private readonly IUndoRedoService<ProjectEntity> _undoRedoService;
        private readonly IJSRuntime _js;


        public ProjectEntity? CurrentProject { get; private set; }
        public ProjectFileInfo? ProjectFileInfo { get; private set; }

        public event Action? OnProjectChanged;
        public event Action? OnProjectFileInfoChanged;
        public event Action? OnUndoRedoStateChanged;
        private void NotifyUndoRedoStateChanged() => OnUndoRedoStateChanged?.Invoke();

        public bool IsDirty { get; private set; } = false;

        public bool CanUndo => _undoRedoService.CanUndo;
        public bool CanRedo => _undoRedoService.CanRedo;

        public ProjectStateService(IUndoRedoService<ProjectEntity> undoRedoService, IJSRuntime js)
        {
            _undoRedoService = undoRedoService;
            _js = js;
        }

        public void SetProject(ProjectEntity project)
        {
            // ✅ Herstel navigation properties na deserialisatie
            // BELANGRIJK: Dit wordt ENKEL aangeroepen voor NIEUWE projecten (na JSON-load)
            RestoreNavigationProperties(project);
            
            // ✅ NIEUW: Valideer alle referenties en log diagnostische info
            var validationReport = ReferenceValidationHelper.ValidateProjectReferences(project);
            if (!validationReport.IsHealthy)
            {
                Console.Error.WriteLine(ReferenceValidationHelper.GetSummary(validationReport));
            }
            else
            {
                Console.WriteLine("✅ All entity references restored successfully!");
            }
            
            CurrentProject = project;
            OnProjectChanged?.Invoke();
        }

        /// <summary>
        /// Bijwerken van project state ZONDER relaties opnieuw in te stellen
        /// Gebruik deze methode voor eenvoudige updates (bijv. ProjectInfo wijzigen)
        /// </summary>
        public void UpdateProject(ProjectEntity project)
        {
            // ✅ Geen RestoreNavigationProperties! Dit is enkel voor state updates
            CurrentProject = project;
            OnProjectChanged?.Invoke();
        }

        /// <summary>
        /// Herstelt object-referenties (nav properties) na JSON-deserialisatie
        /// ✅ NIEUW SYSTEEM: Gebruikt EntityReference<T> voor generieke referentie-beheer
        /// Roept RestoreReferencesAfterDeserialization(ProjectEntity) aan voor elke assemblage
        /// </summary>
        private void RestoreNavigationProperties(ProjectEntity project)
        {
            if (project?.Assemblages == null) return;

            Console.WriteLine($"=== RestoreNavigationProperties START ===");
            Console.WriteLine($"📊 Project.Materialen Count: {project.Materialen?.Count ?? 0}");
            if (project.Materialen?.Count > 0)
            {
                Console.WriteLine($"   Beschikbare material IDs:");
                foreach (var key in project.Materialen.Keys)
                {
                    Console.WriteLine($"     - {key} ({project.Materialen[key]?.Naam ?? "?"})");
                }
            }

            // 🔍 DEBUG: Log alle assemblages met hun MateriaalId
            foreach (var assemblage in project.Assemblages)
            {
                Console.WriteLine($"[RestoreNav] {assemblage.GetType().Name} '{assemblage.Merk}'");
                Console.WriteLine($"   - MateriaalId: {assemblage.MateriaalId}");
                Console.WriteLine($"   - Materiaal: {assemblage.Materiaal?.Naam ?? "NULL"}");
                Console.WriteLine($"   - In dictionary? {(assemblage.MateriaalId.HasValue && (project.Materialen?.ContainsKey(assemblage.MateriaalId.Value) ?? false))}");
            }

            // ✅ Itereer door alle assemblages
            // Virtual call → roept de juiste subtype-implementatie aan (Ligger, Bordes, SteekTrap, etc.)
            foreach (var assemblage in project.Assemblages)
            {
                Console.WriteLine($"\n>>> BEFORE RESTORE: {assemblage.GetType().Name} '{assemblage.Merk}'");
                Console.WriteLine($"    MateriaalId: {assemblage.MateriaalId}");
                Console.WriteLine($"    Materiaal: {assemblage.Materiaal?.Naam ?? "NULL"}");
                
                //System.Diagnostics.Debugger.Break();  // ⬅️ BREAKPOINT
                
                Console.WriteLine($">>> CALLING RestoreReferencesAfterDeserialization()...");
                assemblage.RestoreReferencesAfterDeserialization(project);
                
                Console.WriteLine($">>> AFTER RESTORE: {assemblage.GetType().Name} '{assemblage.Merk}'");
                Console.WriteLine($"    MateriaalId: {assemblage.MateriaalId}");
                Console.WriteLine($"    Materiaal: {assemblage.Materiaal?.Naam ?? "NULL"}");
                
                // 🔍 DETAIL CHECK
                if (assemblage.Materiaal == null && assemblage.MateriaalId.HasValue)
                {
                    Console.Error.WriteLine($"  ❌ CRITICAL: MateriaalId is set ({assemblage.MateriaalId}) but Materiaal is NULL!");
                    if (project.Materialen?.ContainsKey(assemblage.MateriaalId.Value) == true)
                    {
                        Console.Error.WriteLine($"     BUT IT EXISTS IN DICTIONARY: {project.Materialen[assemblage.MateriaalId.Value]?.Naam}");
                        Console.Error.WriteLine($"     RESTORE FAILED!");
                    }
                    else
                    {
                        Console.Error.WriteLine($"     AND IT'S NOT IN THE DICTIONARY!");
                    }
                }
            }
            
            Console.WriteLine($"=== RestoreNavigationProperties END ===\n");
        }

        public void SetProjectFileInfo(ProjectFileInfo projectFileInfo)
        {
            ProjectFileInfo = projectFileInfo;
            OnProjectFileInfoChanged?.Invoke();
        }

        public void MarkDirty()
        {
            IsDirty = true;
            NotifyUndoRedoStateChanged();
        }
        public void MarkSaved() => IsDirty = false;


        // 👇 Undo/Redo-ondersteuning
        public void Snapshot()
        {
            if (CurrentProject is null)
                return;

            _undoRedoService.Snapshot(CurrentProject);

            // asynchroon wegschrijven, fire-and-forget
            _ = SaveProjectToLocalStorageAsync();

        }

        public void Undo()
        {
            if (CurrentProject is null)
                return;

            var previous = _undoRedoService.Undo(CurrentProject);
            if (previous is not null)
            {
                CurrentProject = previous;
                OnProjectChanged?.Invoke();
                NotifyUndoRedoStateChanged();
            }
        }

        public void Redo()
        {
            if (CurrentProject is null)
                return;

            var next = _undoRedoService.Redo(CurrentProject);
            if (next is not null)
            {
                CurrentProject = next;
                OnProjectChanged?.Invoke();
                NotifyUndoRedoStateChanged();
            }
        }

        /// <summary>
        /// ✅ NIEUW: Valideer alle entity-referenties in het huidige project
        /// Kan handmatig aangeroepen worden via Component code-behind
        /// Retourneert detailed diagnostische informatie
        /// </summary>
        public ReferenceValidationHelper.ValidationReport ValidateCurrentProjectReferences()
        {
            if (CurrentProject is null)
            {
                return new ReferenceValidationHelper.ValidationReport
                {
                    Issues = [ new ReferenceValidationHelper.ReferenceCheckResult
                    {
                        EntityName = "ProjectStateService",
                        IsValid = false,
                        ErrorMessage = "CurrentProject is null"
                    }]
                };
            }

            var report = ReferenceValidationHelper.ValidateProjectReferences(CurrentProject);
            
            // Log naar console
            Console.WriteLine(ReferenceValidationHelper.GetSummary(report));
            
            return report;
        }

        public async Task SaveProjectToLocalStorageAsync()
        {
            try
            {
                if (CurrentProject is null)
                    return;

                // ✅ Stel MateriaalId in VOOR serialisatie
                // Het Materiaal object zelf wordt NIET geserialiseerd (JsonIgnore)
                // In plaats daarvan gebruiken we MateriaalList (helper property)
                foreach (var assemblage in CurrentProject.Assemblages)
                {
                    assemblage.MateriaalId = assemblage.Materiaal?.Id;
                }

                var jsonString = JsonSerializer.Serialize(CurrentProject, ProjectJsonOptions.Fast);
                await _js.InvokeVoidAsync("localStorage.setItem", "currentProject", jsonString);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[LocalStorage Save] {ex.Message}");
            }
        }

        public async Task LoadProjectFromLocalStorageAsync()
        {
            try
            {
                var jsonString = await _js.InvokeAsync<string?>("localStorage.getItem", "currentProject");
                if (string.IsNullOrWhiteSpace(jsonString))
                    return;

                Console.WriteLine("=== JSON BEFORE DESERIALIZATION ===");
                // Controleer de Materialen in JSON
                if (jsonString.Contains("\"materialen\"", StringComparison.OrdinalIgnoreCase))
                {
                    var startIdx = jsonString.IndexOf("\"materialen\"", StringComparison.OrdinalIgnoreCase);
                    var snippet = jsonString.Substring(startIdx, Math.Min(500, jsonString.Length - startIdx));
                    Console.WriteLine($"Materialen JSON snippet:\n{snippet}");
                }
                
                // Controleer de materiaalId in SteekTrap
                if (jsonString.Contains("\"steektrap\"", StringComparison.OrdinalIgnoreCase))
                {
                    var startIdx = jsonString.IndexOf("\"steektrap\"", StringComparison.OrdinalIgnoreCase);
                    // Find materiaalId in this section
                    var steektrapSection = jsonString.Substring(startIdx, Math.Min(1000, jsonString.Length - startIdx));
                    if (steektrapSection.Contains("materiaalId", StringComparison.OrdinalIgnoreCase))
                    {
                        var matIdx = steektrapSection.IndexOf("materiaalId", StringComparison.OrdinalIgnoreCase);
                        var matSnippet = steektrapSection.Substring(matIdx, Math.Min(100, steektrapSection.Length - matIdx));
                        Console.WriteLine($"SteekTrap materiaalId:\n{matSnippet}");
                    }
                }
                Console.WriteLine("=== END JSON ===\n");

                var project = JsonSerializer.Deserialize<ProjectEntity>(jsonString, ProjectJsonOptions.Fast);
                if (project is null)
                    return;

                Console.WriteLine("=== AFTER DESERIALIZATION ===");
                Console.WriteLine($"Project.Materialen.Count: {project.Materialen?.Count ?? 0}");
                if (project.Materialen?.Count > 0)
                {
                    foreach (var kvp in project.Materialen)
                    {
                        Console.WriteLine($"  Key: {kvp.Key}, Value: {kvp.Value?.Naam ?? "NULL"}");
                    }
                }
                Console.WriteLine("=== END DESERIALIZATION ===\n");

                // ✅ VOLGORDE CRITICAL!
                // 1. EERST: RestoreNavigationProperties() - herstelt alle Materiaal-referenties
                //    Dit MOET vóór InitAll() omdat InitAll() de Materiaal objects nodig heeft!
                RestoreNavigationProperties(project);
                
                // 2. DAARNA: InitAll() - initialiseert nested properties nu met CORRECT materiaal
                //    Nu ziet Init() het echte Materiaal, niet null!
                project.InitAll();
                
                // 3. TENSLOTTE: SetProject() - triggers change event
                CurrentProject = project;
                OnProjectChanged?.Invoke();
                
                // ✅ Valideer alle referenties na load
                var validationReport = ReferenceValidationHelper.ValidateProjectReferences(project);
                if (!validationReport.IsHealthy)
                {
                    Console.Error.WriteLine(ReferenceValidationHelper.GetSummary(validationReport));
                }
                else
                {
                    Console.WriteLine("✅ All entity references restored successfully!");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[LocalStorage Load] {ex.Message}\n{ex.StackTrace}");
            }
        }



    }



    public class ProjectStateServiceBAK
    {
        public ProjectEntity? CurrentProject { get; private set; }
        public ProjectFileInfo? ProjectFileInfo { get; private set; }

        public event Action? OnProjectChanged;
        public event Action? OnProjectFileInfoChanged;
        //public event Action? OnChange;  // <-- Dit is van StateContainer

        public void SetProject(ProjectEntity project)
        {
            //Value = project;  // <-- Value van StateContainer → triggert OnChange
            CurrentProject = project;
            OnProjectChanged?.Invoke();
            //OnChange?.Invoke();  // <-- Dit is van StateContainer
        }

        public void SetProjectFileInfo(ProjectFileInfo projectFileInfo)
        {
            ProjectFileInfo = projectFileInfo;
            OnProjectFileInfoChanged?.Invoke();
            //OnChange?.Invoke();  // <-- Dit is van StateContainer
        }

        // ✅ Optioneel: Dirty flag ondersteuning
        public bool IsDirty { get; private set; } = false;

        public void MarkDirty()
        {
            IsDirty = true;
            //NotifyStateChanged();  // <-- Dit is van StateContainer
        }

        public void MarkSaved()
        {
            IsDirty = false;
            //NotifyStateChanged();
        }


    }



    //public class ProjectStateService : IProjectStateService
    //{
    //    public ProjectEntity? CurrentProject { get; private set; }

    //    public ProjectFileInfo? ProjectFileInfo { get; private set; }

    //    public event Action? OnProjectChanged;
    //    public event Action? OnProjectFileInfoChanged;

    //    public void SetProject(ProjectEntity project)
    //    {
    //        CurrentProject = project;
    //        OnProjectChanged?.Invoke();
    //    }

    //    public void SetProjectFileInfo(ProjectFileInfo projectFileInfo)
    //    {
    //        ProjectFileInfo = projectFileInfo;
    //        OnProjectFileInfoChanged?.Invoke();
    //    }

    //}


}
