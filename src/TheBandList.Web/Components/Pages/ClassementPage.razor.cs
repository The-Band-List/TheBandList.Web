using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TheBandList.Web.Entities;
using TheBandList.Web.Entities.Context;
using TheBandList.Web.Utils;

namespace TheBandList.Web.Components.Pages
{
    public partial class ClassementPage
    {
        [Inject]
        private DbContextOptions<TheBandListWebDbContext> DbContextOptions { get; set; }

        [Inject]
        private IMemoryCache MemoryCache { get; set; } = default!;

        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        private Chargement Chargement { get; set; } = default!;

        private List<Utilisateur> listeUtilisateurs = [];
        private List<Classement> listeClassements = [];
        private List<ReussiteNiveau> listeReussites = [];
        private List<Niveau> listeNiveaux = [];
        private List<CreateurNiveau> listeCreateurs = [];
        private List<DifficulteFeature> listeFeatures = [];

        private List<UtilisateurAvecPoints> utilisateursAvecPoints = [];
        private List<CreateurAvecNiveaux> createursAvecNiveaux = [];
        private List<NiveauAvecPoints>? niveauxReussis = [];
        private List<NiveauAvecPoints>? niveauxVerifies = [];
        private List<NiveauSimple>? niveauxCrees = [];

        private UtilisateurAvecPoints? utilisateurSelectionne;
        private CreateurAvecNiveaux? meilleurCreateur;
        private UtilisateurAvecPoints? meilleurReussiteur;
        private bool afficherMenuTri;
        private string searchQuery = string.Empty;

        private enum TabVue { Players, Creators, Wins }
        private enum TriParJoueur { PointsDesc, PointsAsc, NameAsc, NameDesc }
        private enum TriParCreateur { CountDesc, CountAsc, NameAsc, NameDesc }
        private enum TriParVictoire { WinsDesc, WinsAsc, NameAsc, NameDesc }

        private TabVue tabActuel = TabVue.Players;
        private TabVue tabEnAttente;
        private TriParJoueur triParJoueur = TriParJoueur.PointsDesc;
        private TriParCreateur triParCreateur = TriParCreateur.CountDesc;
        private TriParVictoire triParVictoire = TriParVictoire.WinsDesc;
        private TriParJoueur enAttenteTriParJoueur;
        private TriParCreateur enAttenteTriParCreateur;
        private TriParVictoire enAttenteTriParVictoire;

        protected override void OnInitialized()
        {
            (listeClassements, listeCreateurs, listeUtilisateurs, listeNiveaux, listeReussites, listeFeatures) =
                Chargement.Cache(listeClassements, listeCreateurs, listeUtilisateurs, listeNiveaux, listeReussites, listeFeatures, DbContextOptions);

            ChargerClassements();
            RegarderSiUnUtilisateurEstSelectionne();
        }

        private string TabClassEnAttente(TabVue tab) =>
            tabEnAttente == tab ? "tab active" : "tab";

        private void FermerMenuTri() => afficherMenuTri = false;
        private void TabEnAttente(TabVue tab) => tabEnAttente = tab;

        private void ToggleMenuTri()
        {
            tabEnAttente = tabActuel;
            enAttenteTriParJoueur = triParJoueur;
            enAttenteTriParCreateur = triParCreateur;
            enAttenteTriParVictoire = triParVictoire;

            afficherMenuTri = !afficherMenuTri;
        }

        private void AppliquerLeTriEtFermer()
        {
            tabActuel = tabEnAttente;
            triParJoueur = enAttenteTriParJoueur;
            triParCreateur = enAttenteTriParCreateur;
            triParVictoire = enAttenteTriParVictoire;
            afficherMenuTri = false;
        }

        private void ResetSort()
        {
            tabEnAttente = TabVue.Players;
            enAttenteTriParJoueur = TriParJoueur.PointsDesc;
            enAttenteTriParCreateur = TriParCreateur.CountDesc;
            enAttenteTriParVictoire = TriParVictoire.WinsDesc;
            AppliquerLeTriEtFermer();
        }

        private string ConvertirDuree(int secondes)
        {
            int minutes = secondes / 60;
            int s = secondes % 60;
            return $"{minutes}min {s}sec";
        }

        private string ObtenirClassBackground(int id) =>
            utilisateurSelectionne != null && utilisateurSelectionne.Id == id ? "selected" : string.Empty;

        private IEnumerable<UtilisateurAvecPoints> ObtenirLaVueJoueur() =>
            triParJoueur switch
            {
                TriParJoueur.PointsAsc => utilisateursAvecPoints.OrderBy(u => u.TotalPoints),
                TriParJoueur.NameAsc => utilisateursAvecPoints.OrderBy(u => u.Nom),
                TriParJoueur.NameDesc => utilisateursAvecPoints.OrderByDescending(u => u.Nom),
                _ => utilisateursAvecPoints.OrderByDescending(u => u.TotalPoints),
            };

        private IEnumerable<CreateurAvecNiveaux> ObtenirLaVueCreateur() =>
            triParCreateur switch
            {
                TriParCreateur.CountAsc => createursAvecNiveaux.OrderBy(c => c.NombreNiveaux),
                TriParCreateur.NameAsc => createursAvecNiveaux.OrderBy(c => c.Nom),
                TriParCreateur.NameDesc => createursAvecNiveaux.OrderByDescending(c => c.Nom),
                _ => createursAvecNiveaux.OrderByDescending(c => c.NombreNiveaux),
            };

        private IEnumerable<UtilisateurAvecPoints> ObtenirLaVueVictoire() =>
            triParVictoire switch
            {
                TriParVictoire.WinsAsc => utilisateursAvecPoints.OrderBy(u => u.TotalNiveauxReussis),
                TriParVictoire.NameAsc => utilisateursAvecPoints.OrderBy(u => u.Nom),
                TriParVictoire.NameDesc => utilisateursAvecPoints.OrderByDescending(u => u.Nom),
                _ => utilisateursAvecPoints.OrderByDescending(u => u.TotalNiveauxReussis),
            };

        private void ChargerClassements()
        {
            var verifieurParNiveauId = listeNiveaux.ToDictionary(n => n.Id, n => n.VerifieurId);
            var nomParNiveauId = listeNiveaux.ToDictionary(n => n.Id, n => n.Nom);

            int PointsVerifieur(int userId) =>
                listeClassements
                    .Where(c => verifieurParNiveauId.TryGetValue(c.NiveauId, out var verifId) && verifId == userId)
                    .Sum(c => c.Points);

            int PointsReussis(int userId) =>
                listeReussites
                    .Where(r => r.UtilisateurId == userId && r.Statut == "Accepter")
                    .Join(listeClassements, r => r.NiveauId, c => c.NiveauId, (_, c) => c.Points)
                    .Sum();

            int TotalNiveauxReussis(int userId) =>
                listeReussites.Count(r => r.UtilisateurId == userId && r.Statut == "Accepter")
                + listeNiveaux.Count(n => n.VerifieurId == userId);

            int CountNiveauxCrees(int userId) =>
                listeCreateurs.Count(cn => cn.CreateurId == userId);

            var utilisateursNonClasses = listeUtilisateurs
                .Select(u => new
                {
                    u.Id,
                    u.Nom,
                    TotalPoints = PointsVerifieur(u.Id) + PointsReussis(u.Id),
                    TotalNiveauxReussis = TotalNiveauxReussis(u.Id),
                    TotalNiveauxCreer = CountNiveauxCrees(u.Id)
                })
                .Where(x => x.TotalPoints > 0)
                .OrderByDescending(x => x.TotalPoints)
                .ToList();

            utilisateursAvecPoints = utilisateursNonClasses
                .Select((u, index) => new UtilisateurAvecPoints
                {
                    Id = u.Id,
                    Nom = u.Nom,
                    TotalPoints = u.TotalPoints,
                    TotalNiveauxReussis = u.TotalNiveauxReussis,
                    Classement = index + 1,
                    TotalNiveauxCreer = u.TotalNiveauxCreer
                })
                .ToList();

            createursAvecNiveaux = listeUtilisateurs
                .Where(u => listeCreateurs.Any(cn => cn.CreateurId == u.Id))
                .Select(u => new CreateurAvecNiveaux
                {
                    Id = u.Id,
                    Nom = u.Nom,
                    NombreNiveaux = listeCreateurs.Count(cn => cn.CreateurId == u.Id)
                })
                .OrderByDescending(c => c.NombreNiveaux)
                .ToList();

            meilleurCreateur = createursAvecNiveaux.FirstOrDefault();
            meilleurReussiteur = utilisateursAvecPoints.OrderByDescending(u => u.TotalNiveauxReussis).FirstOrDefault();
        }

        private void AfficherDetailsUtilisateur(int utilisateurId)
        {
            utilisateurSelectionne = utilisateursAvecPoints.FirstOrDefault(u => u.Id == utilisateurId)
                ?? createursAvecNiveaux
                    .Where(c => c.Id == utilisateurId)
                    .Select(c => new UtilisateurAvecPoints
                    {
                        Id = c.Id,
                        Nom = c.Nom,
                        TotalPoints = 0,
                        TotalNiveauxReussis = 0,
                        TotalNiveauxCreer = c.NombreNiveaux
                    })
                    .FirstOrDefault();

            if (utilisateurSelectionne == null) return;

            var nomParNiveauId = listeNiveaux.ToDictionary(n => n.Id, n => n.Nom);
            var urlParNiveauId = listeNiveaux.ToDictionary(n => n.Id, n => n.UrlVerification);
            var dureeParNiveauId = listeNiveaux.ToDictionary(n => n.Id, n => n.Duree);
            var classementParNiv = listeClassements.ToDictionary(c => c.NiveauId, c => c);

            niveauxReussis = listeReussites
                .Where(r => r.UtilisateurId == utilisateurId && r.Statut == "Accepter")
                .Join(listeClassements,
                    r => r.NiveauId,
                    c => c.NiveauId,
                    (r, c) => new { r.NiveauId, r.Video, c.Points, c.ClassementPosition })
                .Select(x => new NiveauAvecPoints
                {
                    Id = x.NiveauId,
                    Nom = nomParNiveauId.GetValueOrDefault(x.NiveauId, $"Niveau {x.NiveauId}"),
                    Points = x.Points,
                    ClassementPosition = x.ClassementPosition,
                    Video = x.Video,
                    Duree = dureeParNiveauId.GetValueOrDefault(x.NiveauId, 0)
                })
                .OrderBy(n => n.ClassementPosition)
                .ToList();

            var idsVerifies = listeNiveaux
                .Where(n => n.VerifieurId == utilisateurId)
                .Select(n => n.Id)
                .ToHashSet();

            niveauxVerifies = listeClassements
                .Where(c => idsVerifies.Contains(c.NiveauId))
                .Select(c => new NiveauAvecPoints
                {
                    Id = c.NiveauId,
                    Nom = nomParNiveauId.GetValueOrDefault(c.NiveauId, $"Niveau {c.NiveauId}"),
                    Points = c.Points,
                    ClassementPosition = c.ClassementPosition,
                    UrlVerification = urlParNiveauId.GetValueOrDefault(c.NiveauId),
                    Duree = dureeParNiveauId.GetValueOrDefault(c.NiveauId, 0)
                })
                .OrderBy(n => n.ClassementPosition)
                .ToList();

            niveauxCrees = listeCreateurs
                .Where(cn => cn.CreateurId == utilisateurId)
                .Select(cn =>
                {
                    classementParNiv.TryGetValue(cn.NiveauId, out var c);
                    return new NiveauSimple
                    {
                        Id = cn.NiveauId,
                        Nom = nomParNiveauId.GetValueOrDefault(cn.NiveauId, $"Niveau {cn.NiveauId}"),
                        ClassementPosition = c?.ClassementPosition ?? 0,
                        Points = c?.Points ?? 0,
                        Duree = dureeParNiveauId.GetValueOrDefault(cn.NiveauId, 0),
                        Video = urlParNiveauId.GetValueOrDefault(cn.NiveauId)
                    };
                })
                .OrderBy(n => n.ClassementPosition)
                .ToList();
        }

        private void RegarderSiUnUtilisateurEstSelectionne()
        {
            MemoryCache.TryGetValue("UtilisateurSelectionner", out int id);
            AfficherDetailsUtilisateur(id);
            MemoryCache.Remove("UtilisateurSelectionner");
        }

        private void NiveauSelectionnerClick(int niveauId)
        {
            MemoryCache.Set("NiveauSelectionner", niveauId, TimeSpan.FromMinutes(1));
            NavigationManager.NavigateTo("/");
        }

        private class UtilisateurAvecPoints
        {
            public int Id { get; set; }
            public string? Nom { get; set; }
            public int TotalPoints { get; set; }
            public int TotalNiveauxReussis { get; set; }
            public int Classement { get; set; }
            public int TotalNiveauxCreer { get; set; }
        }

        private class CreateurAvecNiveaux
        {
            public int Id { get; set; }
            public string? Nom { get; set; }
            public int NombreNiveaux { get; set; }
        }

        private class NiveauAvecPoints
        {
            public int Id { get; set; }
            public string? Nom { get; set; }
            public int Points { get; set; }
            public string? Video { get; set; }
            public int ClassementPosition { get; set; }
            public string? UrlVerification { get; set; }
            public int Duree { get; set; }
        }

        private class NiveauSimple
        {
            public int Id { get; set; }
            public string? Nom { get; set; }
            public int ClassementPosition { get; set; }
            public int Points { get; set; }
            public int Duree { get; set; }
            public string? Video { get; set; }
        }
    }
}
