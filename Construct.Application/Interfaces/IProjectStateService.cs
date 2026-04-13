using Construct.Domain.Entities;
using Construct.Domain.Common;

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

        /// <summary>
        /// Laad een project na deserialisatie
        /// Herstelt alle navigation properties en relaties
        /// Gebruik dit ENKEL na het laden uit JSON
        /// </summary>
        void SetProject(ProjectEntity project);

        /// <summary>
        /// Bijwerken van project state ZONDER relaties opnieuw in te stellen
        /// Gebruik dit voor eenvoudige updates (bijv. ProjectInfo wijzigen)
        /// </summary>
        void UpdateProject(ProjectEntity project);

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

        /// <summary>
        /// ✅ NIEUW: Valideer alle entity-referenties in het huidige project
        /// Kan handmatig aangeroepen worden via Component code-behind
        /// Retourneert detailed diagnostische informatie
        /// </summary>
        ReferenceValidationHelper.ValidationReport ValidateCurrentProjectReferences();

    }

}
