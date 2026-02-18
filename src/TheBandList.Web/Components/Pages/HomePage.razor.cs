using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TheBandList.Web.Entities;
using TheBandList.Web.Entities.Context;
using TheBandList.Web.Service;
using TheBandList.Web.Utils;

namespace TheBandList.Web.Components.Pages
{
    public partial class HomePage : IDisposable
    {
        [Inject]
        private DbContextOptions<TheBandListWebDbContext> DbContextOptions { get; set; } = default!;

        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        private IMemoryCache MemoryCache { get; set; } = default!;

        [Inject]
        private NiveauService NiveauService { get; set; } = default!;

        [Inject]
        private DifficulteFeatureService DifficulteFeatureService { get; set; } = default!;

        [Inject]
        private Chargement Chargement { get; set; } = default!;

        [Inject]
        private ResetSelectionService ResetService { get; set; } = default!;

        private List<Utilisateur> listeEntiereUtilisateurs = [];
        private List<Niveau> listeEntiereNiveaux = [];
        private List<Classement> listeEntiereClassements = [];
        private List<CreateurNiveau> listeEntiereCreateursNiveaux = [];
        private List<ReussiteNiveau> listeEntiereReussitesNiveaux = [];
        private List<DifficulteFeature> listeEntiereFeatures = [];
        private List<Niveau> niveaux = [];
        private List<Niveau> niveauxFiltres = [];
        private List<Classement> classements = [];
        private List<ReussiteNiveau> reussitesNiveau = [];
        private List<Utilisateur> createurs = [];

        private Niveau? niveauSelectionne;
        private string recherche = string.Empty;
        private string filtreDuree = string.Empty;
        private bool isLoading = true;

        private string? fondA;
        private string? fondB;
        private string fondActif = "a";
        private int? dernierNiveauId;

        protected override async Task OnInitializedAsync()
        {
            (listeEntiereClassements, listeEntiereCreateursNiveaux, listeEntiereUtilisateurs, listeEntiereNiveaux, listeEntiereReussitesNiveaux, listeEntiereFeatures)
                = Chargement.Cache(listeEntiereClassements, listeEntiereCreateursNiveaux, listeEntiereUtilisateurs, listeEntiereNiveaux, listeEntiereReussitesNiveaux, listeEntiereFeatures, DbContextOptions);

            await DifficulteFeatureService.UpdateAllDemonFaceImagesAsync(listeEntiereFeatures);
            await NiveauService.UpdateAllNiveauxImagesAsync(listeEntiereNiveaux);

            ResetService.OnResetRequested += HandleResetRequested;

            ChargerNiveauxEtClassementsProgressivement();
            RegarderSiUnNiveauEstSelectionne();
        }

        protected override void OnParametersSet()
        {
            MettreAJourFond();
        }

        private void MettreAJourFond()
        {
            if (niveauSelectionne is null) return;

            var url = $"/MiniaturesNiveaux/{niveauSelectionne.Id}.png";

            if (fondA is null && fondB is null)
            {
                fondA = url;
                fondB = url;
                fondActif = "a";
                dernierNiveauId = niveauSelectionne.Id;
                return;
            }

            if (dernierNiveauId == niveauSelectionne.Id) return;

            if (fondActif == "a")
            {
                fondB = url;
                fondActif = "b";
            }
            else
            {
                fondA = url;
                fondActif = "a";
            }

            dernierNiveauId = niveauSelectionne.Id;
            StateHasChanged();
        }

        public void Dispose()
        {
            ResetService.OnResetRequested -= HandleResetRequested;
        }

        private void HandleResetRequested()
        {
            ClearSelection();
        }

        private void RegarderSiUnNiveauEstSelectionne()
        {
            if (!MemoryCache.TryGetValue("NiveauSelectionner", out int niveauSelectionne))
                return;

            MemoryCache.Remove("NiveauSelectionner");

            if (niveauSelectionne > 0)
            {
                AfficherDetailsNiveau(niveauSelectionne);
                return;
            }

            ClearSelection();
        }

        private void ClearSelection()
        {
            niveauSelectionne = null;
            createurs.Clear();
            reussitesNiveau.Clear();
            StateHasChanged();
        }

        private void ChargerNiveauxEtClassementsProgressivement()
        {
            isLoading = true;

            niveaux = listeEntiereNiveaux;
            niveauxFiltres = niveaux;

            classements = listeEntiereClassements
                .OrderBy(c => c.ClassementPosition)
                .ToList();

            isLoading = false;
        }

        private void AfficherDetailsNiveau(int niveauId)
        {
            try
            {
                var niveau = listeEntiereNiveaux.FirstOrDefault(n => n.Id == niveauId);
                var createursList = listeEntiereCreateursNiveaux
                    .Where(cn => cn.NiveauId == niveauId)
                    .Select(cn => cn.Createur)
                    .ToList();

                var reussitesList = listeEntiereReussitesNiveaux
                    .Where(r => r.NiveauId == niveauId)
                    .ToList();

                niveauSelectionne = niveau;
                createurs = createursList;
                reussitesNiveau = reussitesList;

                MettreAJourFond();
                StateHasChanged();
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Chargement annulé.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du chargement des détails du niveau : {ex.Message}");
            }
        }

        private void UtilisateurSelectionnerClick(int? utilisateurSelectionne)
        {
            MemoryCache.Set("UtilisateurSelectionner", utilisateurSelectionne, TimeSpan.FromMinutes(1));
            NavigationManager.NavigateTo("/classement");
        }

        private void OnRechercheChanged(string valeurRecherchee)
        {
            recherche = valeurRecherchee;
            FiltrerNiveaux();
        }

        private void OnToutesLesDureesClicked() => OnFiltreDureeChanged(string.Empty);
        private void OnShortClicked() => OnFiltreDureeChanged("short");
        private void OnLongClicked() => OnFiltreDureeChanged("long");
        private void OnXlClicked() => OnFiltreDureeChanged("xl");
        private void OnXxlClicked() => OnFiltreDureeChanged("xxl");

        private string GetSelectedDureeLabel() => filtreDuree switch
        {
            "" => "Durée",
            "short" => "Short",
            "long" => "Long",
            "xl" => "XL",
            "xxl" => "XXL",
            _ => "Durée"
        };

        private void OnFiltreDureeChanged(string nouvelleDuree)
        {
            filtreDuree = nouvelleDuree;
            FiltrerNiveaux();
        }

        private void FiltrerNiveaux()
        {
            niveauxFiltres = niveaux
                .Where(n => string.IsNullOrEmpty(recherche) || n.Nom.Contains(recherche, StringComparison.OrdinalIgnoreCase))
                .Where(FiltrerParDuree)
                .ToList();
        }

        private bool FiltrerParDuree(Niveau niveau)
        {
            int dureeEnMinutes = niveau.Duree / 60;

            return filtreDuree switch
            {
                "short" => dureeEnMinutes >= 0.5 && dureeEnMinutes < 1,
                "long" => dureeEnMinutes >= 1 && dureeEnMinutes < 2,
                "xl" => dureeEnMinutes >= 2 && dureeEnMinutes < 5,
                "xxl" => dureeEnMinutes >= 5,
                _ => true
            };
        }

        private static string ConvertirDuree(int dureeEnSecondes)
        {
            int minutes = dureeEnSecondes / 60;
            int secondes = dureeEnSecondes % 60;
            return $"{minutes}min {secondes}sec";
        }
    }
}