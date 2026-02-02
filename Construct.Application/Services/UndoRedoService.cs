using Construct.Application.Interfaces;

namespace Construct.Application.Services
{
    public class UndoRedoService<T> : IUndoRedoService<T>
    {
        private readonly Stack<T> _undoStack = new();
        private readonly Stack<T> _redoStack = new();
        private readonly Func<T, T> _cloneFunc;

        public UndoRedoService(Func<T, T> cloneFunc)
        {
            _cloneFunc = cloneFunc;
        }

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public void Snapshot(T currentState)
        {
            _undoStack.Push(_cloneFunc(currentState));
            _redoStack.Clear();
        }

        public T? Undo(T currentState)
        {
            if (!CanUndo)
                return default;

            _redoStack.Push(_cloneFunc(currentState));
            return _undoStack.Pop();
        }

        public T? Redo(T currentState)
        {
            if (!CanRedo)
                return default;

            _undoStack.Push(_cloneFunc(currentState));
            return _redoStack.Pop();
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }
    }


}
