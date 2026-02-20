using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TheBandList.Web.Entities;
using TheBandList.Web.Entities.Context;
using TheBandList.Web.Utils;

namespace TheBandList.Web.Components.Pages
{
    public partial class EnvoyerUneVideoPage : IDisposable
    {
        [Inject]
        private DbContextOptions<TheBandListWebDbContext> DbContextOptions { get; set; } = default!;

        [Inject]
        private Chargement Chargement { get; set; } = default!;

        [CascadingParameter]
        private Task<AuthenticationState> AuthStateTask { get; set; } = default!;

        private bool isAuthenticated;
        private string? connectedUsername;

        private CancellationTokenSource? debounceNiveauToken;
        private CancellationTokenSource? debounceUtilisateurToken;
        private SoumissionNiveau newSubmission = new();
        private List<Niveau> listeEntiereNiveaux = [];
        private List<Utilisateur> listeEntiereUtilisateurs = [];
        private List<SoumissionNiveau> listeEntiereSoumissionsNiveaux = [];
        private List<ReussiteNiveau> listeEntiereReussitesNiveaux = [];
        private List<CreateurNiveau> listeEntiereCreateursNiveaux = [];
        private List<Classement> listeEntiereClassements = [];
        private List<DifficulteFeature> listeEntiereFeatures = [];
        private List<string> niveauSuggestions = [];
        private List<string> utilisateurSuggestions = [];

        private string? errorMessage;
        private string currentNiveauInput = string.Empty;
        private string currentUtilisateurInput = string.Empty;
        private bool submissionSuccess;
        private int selectedNiveauIndex = -1;
        private int selectedUtilisateurIndex = -1;
        private bool isLoading;

        private string bgA = "/Pictures/preview-placeholder.png";
        private string bgB = "/Pictures/preview-placeholder.png";
        private bool showBgA = true;
        private System.Timers.Timer? bgTimer;
        private readonly Random _rng = new();

        protected override async Task OnInitializedAsync()
        {
            (listeEntiereClassements, listeEntiereCreateursNiveaux, listeEntiereUtilisateurs, listeEntiereNiveaux, listeEntiereReussitesNiveaux, listeEntiereFeatures) =
                Chargement.Cache(listeEntiereClassements, listeEntiereCreateursNiveaux, listeEntiereUtilisateurs, listeEntiereNiveaux, listeEntiereReussitesNiveaux, listeEntiereFeatures, DbContextOptions);

            bgA = RandomBg();
            bgB = RandomBg(bgA);

            bgTimer = new System.Timers.Timer(10000)
            {
                AutoReset = true
            };

            bgTimer.Elapsed += (_, __) =>
            {
                if (showBgA)
                    bgB = RandomBg(bgA);
                else
                    bgA = RandomBg(bgB);

                showBgA = !showBgA;
                InvokeAsync(StateHasChanged);
            };

            bgTimer.Start();

            using (var ctx = new TheBandListWebDbContext(DbContextOptions))
            {
                listeEntiereSoumissionsNiveaux = ctx.SoumissionsNiveaux.AsNoTracking().ToList();
            }

            var authState = await AuthStateTask;
            var user = authState.User;
            isAuthenticated = user.Identity?.IsAuthenticated == true;

            if (isAuthenticated)
            {
                var discordId = user.FindFirst("discord:id")?.Value;
                var fallbackName = user.FindFirst("discord:global_name")?.Value
                                   ?? user.Identity?.Name
                                   ?? user.FindFirst(ClaimTypes.Name)?.Value;

                using var ctx = new TheBandListWebDbContext(DbContextOptions);
                if (!string.IsNullOrEmpty(discordId))
                {
                    var account = ctx.DiscordAccounts
                        .Include(a => a.Utilisateur)
                        .AsNoTracking()
                        .SingleOrDefault(a => a.DiscordId == discordId);

                    connectedUsername = account?.Utilisateur?.Nom ?? fallbackName;
                }
                else
                {
                    connectedUsername = fallbackName;
                }

                currentUtilisateurInput = connectedUsername ?? string.Empty;
                newSubmission.NomUtilisateur = connectedUsername;
                utilisateurSuggestions.Clear();
            }
        }

        public void Dispose()
        {
            bgTimer?.Stop();
            bgTimer?.Dispose();
        }

        private async Task OnNiveauInput(ChangeEventArgs e)
        {
            currentNiveauInput = e.Value?.ToString() ?? string.Empty;
            newSubmission.NomNiveau = currentNiveauInput;

            debounceNiveauToken?.Cancel();
            debounceNiveauToken = new CancellationTokenSource();
            var token = debounceNiveauToken.Token;

            if (string.IsNullOrWhiteSpace(currentNiveauInput))
            {
                niveauSuggestions.Clear();
                StateHasChanged();
                return;
            }

            try
            {
                await Task.Delay(50, token);
                if (!token.IsCancellationRequested)
                {
                    var exactMatchExists = listeEntiereNiveaux.Any(n =>
                        n.Nom.Equals(currentNiveauInput, StringComparison.OrdinalIgnoreCase));

                    niveauSuggestions = exactMatchExists
                        ? []
                        : listeEntiereNiveaux
                            .Where(n => n.Nom.Contains(currentNiveauInput, StringComparison.OrdinalIgnoreCase))
                            .Select(n => $"{n.Nom} par {n.Publisher.Nom}")
                            .Take(5)
                            .ToList();

                    StateHasChanged();
                }
            }
            catch (TaskCanceledException) { }
        }

        private async Task OnUtilisateurInput(ChangeEventArgs e)
        {
            if (isAuthenticated) return;

            currentUtilisateurInput = e.Value?.ToString() ?? string.Empty;
            newSubmission.NomUtilisateur = currentUtilisateurInput;

            debounceUtilisateurToken?.Cancel();
            debounceUtilisateurToken = new CancellationTokenSource();
            var token = debounceUtilisateurToken.Token;

            if (string.IsNullOrWhiteSpace(currentUtilisateurInput))
            {
                utilisateurSuggestions.Clear();
                StateHasChanged();
                selectedUtilisateurIndex = -1;
                return;
            }

            try
            {
                await Task.Delay(50, token);
                if (!token.IsCancellationRequested)
                {
                    var exactMatchExists = listeEntiereUtilisateurs.Any(u =>
                        u.Nom.Equals(currentUtilisateurInput, StringComparison.OrdinalIgnoreCase));

                    utilisateurSuggestions = exactMatchExists
                        ? []
                        : listeEntiereUtilisateurs
                            .Where(u => u.Nom.Contains(currentUtilisateurInput, StringComparison.OrdinalIgnoreCase))
                            .Select(u => u.Nom)
                            .Take(5)
                            .ToList();

                    StateHasChanged();
                }
            }
            catch (TaskCanceledException) { }
        }

        private async void HandleKeyDownNiveau(KeyboardEventArgs e)
        {
            if (!niveauSuggestions.Any()) return;

            if (e.Key == "ArrowDown")
                selectedNiveauIndex = (selectedNiveauIndex + 1) % niveauSuggestions.Count;
            else if (e.Key == "ArrowUp")
                selectedNiveauIndex = (selectedNiveauIndex - 1 + niveauSuggestions.Count) % niveauSuggestions.Count;
            else if (e.Key == "Enter" && selectedNiveauIndex >= 0)
            {
                string nomNiveau = niveauSuggestions[selectedNiveauIndex].Split(" par ")[0];
                currentNiveauInput = nomNiveau;
                newSubmission.NomNiveau = nomNiveau;
                niveauSuggestions.Clear();
                selectedNiveauIndex = -1;
                StateHasChanged();
            }
        }

        private async void HandleKeyDownUtilisateur(KeyboardEventArgs e)
        {
            if (isAuthenticated || !utilisateurSuggestions.Any()) return;

            if (e.Key == "ArrowDown")
                selectedUtilisateurIndex = (selectedUtilisateurIndex + 1) % utilisateurSuggestions.Count;
            else if (e.Key == "ArrowUp")
                selectedUtilisateurIndex = (selectedUtilisateurIndex - 1 + utilisateurSuggestions.Count) % utilisateurSuggestions.Count;
            else if (e.Key == "Enter" && selectedUtilisateurIndex >= 0)
            {
                string nomUtilisateur = utilisateurSuggestions[selectedUtilisateurIndex];
                currentUtilisateurInput = nomUtilisateur;
                newSubmission.NomUtilisateur = nomUtilisateur;
                utilisateurSuggestions.Clear();
                selectedUtilisateurIndex = -1;
                StateHasChanged();
            }
        }

        private void SelectNiveau(string suggestion)
        {
            var nomNiveau = suggestion.Split(" par ")[0];
            currentNiveauInput = nomNiveau;
            newSubmission.NomNiveau = nomNiveau;
            StateHasChanged();
        }

        private void SelectUtilisateur(string nomUtilisateur)
        {
            if (isAuthenticated) return;

            currentUtilisateurInput = nomUtilisateur;
            newSubmission.NomUtilisateur = nomUtilisateur;
            StateHasChanged();
        }

        private async void ValidateAndSubmit()
        {
            if (isAuthenticated)
            {
                currentUtilisateurInput = connectedUsername ?? string.Empty;
                newSubmission.NomUtilisateur = connectedUsername;
            }

            if (string.IsNullOrWhiteSpace(currentNiveauInput))
            {
                errorMessage = "Veuillez remplir le champ : Nom du niveau.";
                submissionSuccess = false;
                return;
            }

            if (!isAuthenticated && string.IsNullOrWhiteSpace(currentUtilisateurInput))
            {
                errorMessage = "Veuillez remplir le champ : Nom d'utilisateur.";
                submissionSuccess = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(newSubmission.UrlVideo))
            {
                errorMessage = "Veuillez remplir le champ : URL de la vidéo.";
                submissionSuccess = false;
                return;
            }

            errorMessage = string.Empty;
            submissionSuccess = false;
            isLoading = true;
            StateHasChanged();

            var isUnique = CheckIfAlreadySucceeded();
            if (!isUnique)
            {
                isLoading = false;
                submissionSuccess = false;
                StateHasChanged();
                return;
            }

            await HandleValidSubmit();
            isLoading = false;
            StateHasChanged();
        }

        private async Task HandleValidSubmit()
        {
            try
            {
                if (DbContextOptions is not null)
                {
                    using var dbContext = new TheBandListWebDbContext(DbContextOptions);
                    newSubmission.DateSoumission = DateTime.Now;

                    dbContext.SoumissionsNiveaux.Add(newSubmission);
                    await dbContext.SaveChangesAsync();

                    newSubmission = new SoumissionNiveau();
                    submissionSuccess = true;
                }
            }
            catch
            {
                submissionSuccess = false;
            }
            finally
            {
                selectedNiveauIndex = -1;
                selectedUtilisateurIndex = -1;
                currentNiveauInput = string.Empty;
                currentUtilisateurInput = string.Empty;
                StateHasChanged();
            }
        }

        private bool CheckIfAlreadySucceeded()
        {
            try
            {
                if (isAuthenticated && !string.IsNullOrWhiteSpace(connectedUsername))
                {
                    currentUtilisateurInput = connectedUsername;
                    newSubmission.NomUtilisateur = connectedUsername;
                }

                var utilisateur = listeEntiereUtilisateurs
                    .FirstOrDefault(u => u.Nom.Equals(currentUtilisateurInput.Trim(), StringComparison.OrdinalIgnoreCase));

                if (utilisateur != null)
                {
                    var niveau = listeEntiereNiveaux
                        .FirstOrDefault(n => n.Nom.Equals(currentNiveauInput.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (niveau != null)
                    {
                        var reussite = listeEntiereReussitesNiveaux
                            .FirstOrDefault(r => r.UtilisateurId == utilisateur.Id && r.NiveauId == niveau.Id);

                        if (reussite != null && reussite.Statut == "Accepter")
                        {
                            errorMessage = $"{utilisateur.Nom} a déjà réussi le niveau {niveau.Nom}.";
                            StateHasChanged();
                            return false;
                        }

                        var niveauVerifie = listeEntiereNiveaux
                            .Any(n => n.Id == niveau.Id && n.VerifieurId == utilisateur.Id);

                        if (niveauVerifie)
                        {
                            errorMessage = $"{utilisateur.Nom} a vérifié le niveau {niveau.Nom}, et ne peut donc pas soumettre de réussite pour ce niveau.";
                            StateHasChanged();
                            return false;
                        }
                    }
                }

                var existingSubmission = listeEntiereSoumissionsNiveaux
                    .FirstOrDefault(s => s.NomNiveau.Equals(currentNiveauInput.Trim(), StringComparison.OrdinalIgnoreCase)
                                         && s.NomUtilisateur.Equals(currentUtilisateurInput.Trim(), StringComparison.OrdinalIgnoreCase));

                if (existingSubmission != null)
                {
                    errorMessage = $"{currentUtilisateurInput} a déjà envoyé une soumission pour {currentNiveauInput}.";
                    StateHasChanged();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Erreur lors de la vérification : {ex.Message}";
                return false;
            }
        }

        private void HandleFocusOutNiveau(FocusEventArgs e)
        {
            Task.Delay(100).ContinueWith(_ =>
            {
                niveauSuggestions.Clear();
                selectedNiveauIndex = -1;
                InvokeAsync(StateHasChanged);
            });
        }

        private void HandleFocusOutUtilisateur(FocusEventArgs e)
        {
            if (isAuthenticated) return;

            Task.Delay(100).ContinueWith(_ =>
            {
                utilisateurSuggestions.Clear();
                selectedUtilisateurIndex = -1;
                InvokeAsync(StateHasChanged);
            });
        }

        private void OnFocusNiveau(FocusEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(newSubmission.NomNiveau)) return;

            var exactMatchExists = listeEntiereNiveaux.Any(n =>
                n.Nom.Equals(newSubmission.NomNiveau, StringComparison.OrdinalIgnoreCase));

            niveauSuggestions = exactMatchExists
                ? []
                : listeEntiereNiveaux
                    .Where(n => n.Nom.Contains(newSubmission.NomNiveau, StringComparison.OrdinalIgnoreCase))
                    .Select(n => $"{n.Nom} par {n.Publisher.Nom}")
                    .Take(5)
                    .ToList();

            StateHasChanged();
        }

        private void OnFocusUtilisateur(FocusEventArgs e)
        {
            if (isAuthenticated || string.IsNullOrWhiteSpace(newSubmission.NomUtilisateur)) return;

            var exactMatchExists = listeEntiereUtilisateurs.Any(u =>
                u.Nom.Equals(newSubmission.NomUtilisateur, StringComparison.OrdinalIgnoreCase));

            utilisateurSuggestions = exactMatchExists
                ? []
                : listeEntiereUtilisateurs
                    .Where(u => u.Nom.Contains(newSubmission.NomUtilisateur, StringComparison.OrdinalIgnoreCase))
                    .Select(u => u.Nom)
                    .Take(5)
                    .ToList();

            StateHasChanged();
        }

        private string RandomBg(string? exclude = null)
        {
            if (listeEntiereNiveaux.Count == 0)
                return exclude ?? "/Pictures/preview-placeholder.png";

            for (int i = 0; i < 4; i++)
            {
                var n = listeEntiereNiveaux[_rng.Next(listeEntiereNiveaux.Count)];
                var url = $"/MiniaturesNiveaux/{n.Id}.png";
                if (url != exclude) return url;
            }

            return $"/MiniaturesNiveaux/{listeEntiereNiveaux[0].Id}.png";
        }
    }
}
