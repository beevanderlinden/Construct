namespace Construct.Application.Interfaces
{
    public interface IUndoRedoService<T>
    {
        void Snapshot(T currentState);
        T? Undo(T currentState);
        T? Redo(T currentState);
        void Clear();

        bool CanUndo { get; }
        bool CanRedo { get; }
    }

}
