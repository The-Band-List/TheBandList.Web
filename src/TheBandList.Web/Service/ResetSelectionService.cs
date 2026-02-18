namespace TheBandList.Web.Service
{
    public class ResetSelectionService
    {
        public event Action? OnResetRequested;

        public void RequestReset() => OnResetRequested?.Invoke();
    }
}
