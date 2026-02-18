using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using TheBandList.Web.Service;

namespace TheBandList.Web.Components.Layout
{
    public partial class NavMenu : IDisposable
    {
        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        private ResetSelectionService ResetService { get; set; } = default!;

        [Inject]
        private DiscordPresenceService Presence { get; set; } = default!;

        [CascadingParameter]
        private Task<AuthenticationState>? AuthStateTask { get; set; }

        private static readonly Dictionary<string, (int left, int width)> IndicatorPositions = new()
        {
            { "/", (5, 65) },
            { "/classement", (79, 115) },
            { "/soumettre-une-reussite", (203, 196) }
        };

        private string currentUri = "/";
        private string hoveredItem = "/";
        private string? _discordId;
        private ulong _listenId;
        private bool isHovering;
        private string statusClass = "etat--offline";
        private CancellationTokenSource? _warmupCts;
        private bool _subscribed;
        private (int left, int width) _lastIndicatorPos = (5, 65);

        protected override void OnInitialized()
        {
            currentUri = NavigationManager.Uri;
            hoveredItem = new Uri(currentUri).AbsolutePath;
            NavigationManager.LocationChanged += HandleLocationChanged;
        }

        private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            currentUri = e.Location;
            if (!isHovering) hoveredItem = new Uri(currentUri).AbsolutePath;
            StateHasChanged();
        }

        protected override async Task OnInitializedAsync()
        {
            if (AuthStateTask is not null)
            {
                var authState = await AuthStateTask;
                _discordId = authState.User.FindFirst("discord:id")?.Value;
            }

            if (ulong.TryParse(_discordId, out _listenId))
            {
                if (!_subscribed)
                {
                    Presence.StatusChanged += OnPresenceChanged;
                    _subscribed = true;
                }

                if (Presence.TryGetCachedStatus(_listenId, out var cached))
                    ApplyStatus(cached);

                _warmupCts = new CancellationTokenSource();
                _ = Task.Run(async () =>
                {
                    var end = DateTime.UtcNow.AddSeconds(20);
                    while (DateTime.UtcNow < end && !_warmupCts.IsCancellationRequested)
                    {
                        var s = await Presence.GetStatusAsync(_listenId, _warmupCts.Token);
                        await InvokeAsync(() => { ApplyStatus(s); StateHasChanged(); });
                        if (s != "offline") break;
                        await Task.Delay(2000, _warmupCts.Token);
                    }
                }, _warmupCts.Token);
            }
        }

        private void OnPresenceChanged(ulong userId, string s)
        {
            if (userId != _listenId) return;
            _ = InvokeAsync(() =>
            {
                ApplyStatus(s);
                StateHasChanged();
            });
        }

        private void ApplyStatus(string s)
        {
            statusClass = s switch
            {
                "online" => "etat--online",
                "idle" => "etat--idle",
                "dnd" => "etat--dnd",
                _ => "etat--offline"
            };
        }

        private string GetSelectedClass(string href)
        {
            var absoluteUri = NavigationManager.ToAbsoluteUri(href).AbsolutePath;
            var currentAbsoluteUri = new Uri(currentUri).AbsolutePath;

            string classes = "";
            if (hoveredItem == href) classes += " hovered";
            if (currentAbsoluteUri.Equals(absoluteUri, StringComparison.OrdinalIgnoreCase)) classes += " selected";
            return classes.Trim();
        }

        private void SetHoveredItem(string href)
        {
            isHovering = true;
            hoveredItem = href;
            StateHasChanged();
        }

        private void HandleNavMouseLeave()
        {
            isHovering = false;
            hoveredItem = new Uri(currentUri).AbsolutePath;
            StateHasChanged();
        }

        private bool ShouldShowIndicator()
        {
            var path = hoveredItem ?? new Uri(currentUri).AbsolutePath;
            return IndicatorPositions.ContainsKey(path);
        }

        private string GetIndicatorClass()
        {
            return ShouldShowIndicator() ? "indicator indicator--visible" : "indicator indicator--hidden";
        }

        private string GetIndicatorStyle()
        {
            var path = hoveredItem ?? new Uri(currentUri).AbsolutePath;

            if (IndicatorPositions.TryGetValue(path, out var pos))
            {
                _lastIndicatorPos = pos;
            }

            var (left, width) = _lastIndicatorPos;
            const int height = 40;
            return $"left:{left}px;width:{width}px;height:{height}px;top:50%;transform:translateY(-50%);";
        }

        public void GoToHomePage()
        {
            ResetService.RequestReset();
            var uri = new Uri(NavigationManager.Uri);
            if (!uri.AbsolutePath.Equals("/", StringComparison.OrdinalIgnoreCase))
                NavigationManager.NavigateTo("/");
        }

        public void Dispose()
        {
            NavigationManager.LocationChanged -= HandleLocationChanged;
            if (_subscribed) Presence.StatusChanged -= OnPresenceChanged;
            _warmupCts?.Cancel();
            _warmupCts?.Dispose();
        }
    }
}