using Construct.Application.Interfaces;

namespace Construct.Application.Services
{
    public class LogService : ILogService
    {
        public event Action<string>? LogUpdated;

        private string _lastLine = string.Empty;
        private bool _isBusy;

        public string LastLine => _lastLine;

        public void WriteLine(string message)
        {
            _lastLine = message;
            Console.WriteLine(message); // optioneel: ook nog naar echte console
            LogUpdated?.Invoke(message); // notificatie voor UI
        }
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    LogUpdated?.Invoke(_lastLine);
                }
            }
        }

        public void StartBusy(string? message = null)
        {
            IsBusy = true;
            if (!string.IsNullOrWhiteSpace(message))
                LogUpdated?.Invoke(message);
        }

        public void StopBusy(string? message = null)
        {
            IsBusy = false;
            if (!string.IsNullOrWhiteSpace(message))
                LogUpdated?.Invoke(message);
        }
    }

}
