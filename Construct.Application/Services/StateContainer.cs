namespace Construct.Application.Services
{
    public class StateContainer<T>
    {
        private T? _value;

        public T? Value
        {
            get => _value;
            set
            {
                if (!EqualityComparer<T?>.Default.Equals(_value, value))
                {
                    _value = value;
                    NotifyStateChanged();
                }
            }
        }

        public event Action? OnChange;

        public void NotifyStateChanged() => OnChange?.Invoke();
    }

}
