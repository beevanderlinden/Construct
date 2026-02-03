using Construct.Application.Factories;
using Construct.Application.Interfaces;
using Construct.Domain;
using Construct.Domain.Entities;
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
        /// omdat ReferenceHandler.IgnoreCycles deze negeert
        /// </summary>
        private void RestoreNavigationProperties(ProjectEntity project)
        {
            if (project?.Assemblages == null) return;

            // ✅ Materialen Dictionary is al gevuld door JSON-deserialisatie (ReferenceHandler)
            // Zorg alleen dat Assemblages naar dezelfde Materiaal references wijzen
            
            foreach (var assemblage in project.Assemblages)
            {
                if (assemblage.MateriaalId.HasValue && assemblage.MateriaalId != Guid.Empty)
                {
                    if (project.Materialen.TryGetValue(assemblage.MateriaalId.Value, out var materiaal))
                    {
                        assemblage.Materiaal = materiaal;
                    }
                }
            }

            // Herstel AansluitendeElementen in BordesEntity's
            foreach (var bordes in project.Assemblages.OfType<BordesEntity>())
            {
                if (bordes.Trap1AansluitendElementGuid.HasValue)
                {
                    var element = project.Assemblages.FirstOrDefault(a => a.Guid == bordes.Trap1AansluitendElementGuid);
                    if (element != null)
                    {
                        bordes.Trap1.AansluitendElement = element;
                    }
                }

                if (bordes.Trap2AansluitendElementGuid.HasValue)
                {
                    var element = project.Assemblages.FirstOrDefault(a => a.Guid == bordes.Trap2AansluitendElementGuid);
                    if (element != null)
                    {
                        bordes.Trap2.AansluitendElement = element;
                    }
                }
            }

            // Roep RestoreReferencesAfterDeserialization aan per assemblage
            foreach (var assemblage in project.Assemblages)
            {
                assemblage.RestoreReferencesAfterDeserialization(project.ProjectInfo);
            }
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

                var project = JsonSerializer.Deserialize<ProjectEntity>(jsonString, ProjectJsonOptions.Fast);
                if (project is null)
                    return;

                // ✅ VEREENVOUDIGD: Geen MateriaalFactory meer nodig!
                // Materialen worden opgebouwd uit project.Materialen Dictionary
                // (ReferenceHandler doet zijn werk via JSON $ref/$id patterns)

                project.InitAll();
                SetProject(project);  // ← RestoreNavigationProperties wordt hier aangeroepen
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[LocalStorage Load] {ex.Message}");
            }
        }



    }



    public class ProjectStateServiceBAK
    {
        private readonly IUndoRedoService<ProjectEntity> _undoRedoService;
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
