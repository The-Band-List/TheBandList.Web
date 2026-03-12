namespace TheBandList.Web.Services
{
    public class ResetSelectionService
    {
        public event Action? OnResetRequested;

        public void RequestReset() => OnResetRequested?.Invoke();
    }
}
