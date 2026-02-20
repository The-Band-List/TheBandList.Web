using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using TheBandList.Web.Entities;
using TheBandList.Web.Entities.Context;

namespace TheBandList.Web.Components.Pages
{
    [Authorize]
    public partial class Profile : ComponentBase
    {
        [Inject]
        private DbContextOptions<TheBandListWebDbContext> DbContextOptions { get; set; } = default!;

        [Inject]
        private AuthenticationStateProvider AuthProvider { get; set; } = default!;

        bool isLoading = true;
        bool Saving = false;
        string? erreur;
        string? Validation;
        string? Succes;
        bool popupFusion = false;
        bool ConfirmerModificationDemande = false;
        string FusionTarget = "";

        string NomFusionFinal = "";
        string? FusionMessage;

        string? AvatarUrl;
        string? DiscordUsername;
        string? DiscordDisplayName;

        int UtilisateurId;
        string Nom = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var authState = await AuthProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (user?.Identity?.IsAuthenticated is not true)
                {
                    erreur = "Vous devez être connecté.";
                    isLoading = false;
                    return;
                }

                var discordId = user.FindFirst("discord:id")?.Value;
                var avatar = user.FindFirst("discord:avatar")?.Value;

                if (string.IsNullOrWhiteSpace(discordId))
                {
                    erreur = "Identifiant Discord introuvable.";
                    isLoading = false;
                    return;
                }

                AvatarUrl = (!string.IsNullOrWhiteSpace(discordId) && !string.IsNullOrWhiteSpace(avatar))
                    ? $"https://cdn.discordapp.com/avatars/{discordId}/{avatar}.png?size=128"
                    : "https://cdn.discordapp.com/embed/avatars/0.png";

                using (var dbContext = new TheBandListWebDbContext(DbContextOptions))
                {
                    var account = await dbContext.DiscordAccounts
                    .Include(a => a.Utilisateur)
                    .SingleOrDefaultAsync(a => a.DiscordId == discordId);

                    if (account is null || account.Utilisateur is null)
                    {
                        erreur = "Le compte Discord n'est pas relié à un utilisateur.";
                        isLoading = false;
                        return;
                    }

                    Nom = account.Utilisateur.Nom ?? string.Empty;
                    DiscordUsername = account.DiscordUsername ?? "—";
                    DiscordDisplayName = account.DiscordDisplayName ?? DiscordUsername ?? "—";
                    UtilisateurId = account.Utilisateur.Id;
                    Nom = account.Utilisateur.Nom ?? string.Empty;
                }

                isLoading = false;
            }
            catch (Exception ex)
            {
                erreur = $"Erreur lors du chargement : {ex.Message}";
                isLoading = false;
            }
        }

        protected async Task EnregistrerNom()
        {
            Validation = Succes = null;
            var nouveau = (Nom ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(nouveau))
            {
                Validation = "Le nom est requis.";
                return;
            }

            if (nouveau.Length < 3)
            {
                Validation = "Le nom doit contenir au moins 3 caractères.";
                return;
            }

            try
            {
                Saving = true;

                using (var dbContext = new TheBandListWebDbContext(DbContextOptions))
                {
                    var existe = await dbContext.Utilisateurs
                    .AsNoTracking()
                    .AnyAsync(u => u.Id != UtilisateurId && (u.Nom ?? string.Empty).ToLower() == nouveau.ToLower());

                    if (existe)
                    {
                        Validation = "Ce nom est déjà pris.";
                        return;
                    }

                    var utilisateur = await dbContext.Utilisateurs.FindAsync(UtilisateurId);
                    if (utilisateur is null)
                    {
                        Validation = "Utilisateur introuvable.";
                        return;
                    }

                    utilisateur.Nom = nouveau;
                    await dbContext.SaveChangesAsync();

                    Succes = "Nom mis à jour.";
                }
            }
            catch (DbUpdateException)
            {
                Validation = "Ce nom est déjà utilisé.";
            }
            catch (Exception ex)
            {
                Validation = $"Impossible d’enregistrer : {ex.Message}";
            }
            finally
            {
                Saving = false;
            }
        }

        void OuvrirPopupFusion()
        {
            FusionMessage = null;
            popupFusion = true;
        }

        async Task EnvoyerDemandeFusion()
        {
            FusionMessage = null;

            if (string.IsNullOrWhiteSpace(FusionTarget))
            {
                FusionMessage = "Veuillez indiquer un utilisateur.";
                return;
            }

            using (var dbContext = new TheBandListWebDbContext(DbContextOptions))
            {
                var demandeExistante = await dbContext.FusionsUtilisateurs
                    .FirstOrDefaultAsync(f => f.UtilisateurDemandeurId == UtilisateurId && f.Statut == "EnAttente");

                if (demandeExistante is not null)
                {
                    if (!ConfirmerModificationDemande)
                    {
                        FusionMessage = "Vous avez déjà une demande de fusion en attente. Voulez-vous la modifier ?";
                        ConfirmerModificationDemande = true;
                        return;
                    }
                    else
                    {
                        var cibleModif = await dbContext.Utilisateurs
                            .FirstOrDefaultAsync(u => u.Id.ToString() == FusionTarget ||
                                                     (u.Nom != null && u.Nom.ToLower() == FusionTarget.ToLower()));

                        if (cibleModif is null)
                        {
                            FusionMessage = "Utilisateur cible introuvable.";
                            return;
                        }

                        if (cibleModif.Id == UtilisateurId)
                        {
                            FusionMessage = "Impossible de fusionner votre compte avec lui-même.";
                            return;
                        }

                        demandeExistante.UtilisateurCibleId = cibleModif.Id;
                        demandeExistante.NomConserve = (NomFusionFinal == "cible" ? cibleModif.Nom! : Nom);

                        await dbContext.SaveChangesAsync();

                        FusionMessage = "La demande existante a été modifiée.";
                        ConfirmerModificationDemande = false;
                        return;
                    }
                }

                var cible = await dbContext.Utilisateurs
                    .FirstOrDefaultAsync(u => u.Id.ToString() == FusionTarget ||
                                             (u.Nom != null && u.Nom.ToLower() == FusionTarget.ToLower()));

                if (cible is null)
                {
                    FusionMessage = "Utilisateur cible introuvable.";
                    return;
                }

                if (cible.Id == UtilisateurId)
                {
                    FusionMessage = "Impossible de fusionner votre compte avec lui-même.";
                    return;
                }

                var fusion = new FusionUtilisateur
                {
                    UtilisateurDemandeurId = UtilisateurId,
                    UtilisateurCibleId = cible.Id,
                    NomConserve = (NomFusionFinal == "cible" ? cible.Nom! : Nom),
                    Statut = "EnAttente"
                };

                dbContext.FusionsUtilisateurs.Add(fusion);
                await dbContext.SaveChangesAsync();
            }

            FusionMessage = "Demande envoyée. Un administrateur doit maintenant valider la fusion.";
            ConfirmerModificationDemande = false;
        }
    }
}
