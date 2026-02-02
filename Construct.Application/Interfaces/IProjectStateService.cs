using Construct.Domain.Entities;

namespace Construct.Application.Interfaces
{
    public interface IProjectStateService
    {
        ProjectEntity? CurrentProject { get; }
        event Action? OnProjectChanged;
        event Action? OnUndoRedoStateChanged;

        ProjectFileInfo? ProjectFileInfo { get; }
        event Action? OnProjectFileInfoChanged;


        /// ✅ Toevoegen voor StateContainer compatibiliteit
        //event Action? OnChange;

        void SetProject(ProjectEntity project);
        void SetProjectFileInfo(ProjectFileInfo projectFileInfo);

        // 👇 Nieuw voor Undo/Redo service
        void Snapshot();
        void Undo();
        void Redo();
        bool CanUndo { get; }
        bool CanRedo { get; }
        void MarkDirty();

        Task SaveProjectToLocalStorageAsync();
        Task LoadProjectFromLocalStorageAsync();


    }

}
