namespace Construct.Application.Interfaces
{
    public interface ILogService
    {
        public event Action<string>? LogUpdated;

        public void WriteLine(string message);
        public string LastLine { get; }
        public bool IsBusy { get; }
        public void StartBusy(string? message = null);
        public void StopBusy(string? message = null);
    }

}
