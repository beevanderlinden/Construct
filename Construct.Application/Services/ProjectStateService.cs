using Construct.Application.Factories;
using Construct.Application.Interfaces;
using Construct.Domain;
using Construct.Domain.Entities;
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
            CurrentProject = project;
            OnProjectChanged?.Invoke();
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

                // ⚡ Sla polymorfe Materiaal properties op als JsonElement
                foreach (var assemblage in CurrentProject.Assemblages)
                {
                    assemblage.MateriaalJson =
                        JsonSerializer.SerializeToNode(assemblage.Materiaal, ProjectJsonOptions.Fast);
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

                // ⚡ Polymorfe assemblages vullen
                foreach (var assemblage in project.Assemblages.Where(a=>a.MateriaalJson != null))
                {
                    assemblage.Materiaal =
                        MateriaalFactory.Create(assemblage.MateriaalJson!.AsObject(), ProjectJsonOptions.Fast);
                }

                project.InitAll();
                SetProject(project);
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
