using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TheBandList.Web.Entities;
using TheBandList.Web.Entities.Context;

namespace TheBandList.Web.Utils
{
    public class Chargement
    {
        private readonly IMemoryCache _cache;
        private const string KEY_CLASSEMENTS = "ListeEntiereClassements";
        private const string KEY_CREATEURS = "ListeEntiereCreateursNiveaux";
        private const string KEY_UTILISATEURS = "ListeEntiereUtilisateurs";
        private const string KEY_NIVEAUX = "ListeEntiereNiveaux";
        private const string KEY_REUSSITES = "ListeEntiereReussitesNiveaux";
        private const string KEY_FEATURES = "ListeEntiereDifficulteFeatures";

        private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(10);

        public Chargement(IMemoryCache cache)
        {
            _cache = cache;
        }

        public (List<Classement> ListeEntiereClassements,
                List<CreateurNiveau> ListeEntiereCreateursNiveaux,
                List<Utilisateur> ListeEntiereUtilisateurs,
                List<Niveau> ListeEntiereNiveaux,
                List<ReussiteNiveau> ListeEntiereReussitesNiveaux,
                List<DifficulteFeature> ListeEntiereFeatures)
            Cache(
                List<Classement> ListeEntiereClassements,
                List<CreateurNiveau> ListeEntiereCreateursNiveaux,
                List<Utilisateur> ListeEntiereUtilisateurs,
                List<Niveau> ListeEntiereNiveaux,
                List<ReussiteNiveau> ListeEntiereReussitesNiveaux,
                List<DifficulteFeature> ListeEntiereFeatures,
                DbContextOptions<TheBandListWebDbContext> dbContextOptions)
        {
            if (_cache.TryGetValue(KEY_CLASSEMENTS, out List<Classement>? classementsEnCache) &&
                _cache.TryGetValue(KEY_CREATEURS, out List<CreateurNiveau>? createursEnCache) &&
                _cache.TryGetValue(KEY_UTILISATEURS, out List<Utilisateur>? utilisateursEnCache) &&
                _cache.TryGetValue(KEY_NIVEAUX, out List<Niveau>? niveauxEnCache) &&
                _cache.TryGetValue(KEY_REUSSITES, out List<ReussiteNiveau>? reussitesEnCache) &&
                _cache.TryGetValue(KEY_FEATURES, out List<DifficulteFeature>? difficulteFeatures))
            {
                return (classementsEnCache!,
                        createursEnCache!,
                        utilisateursEnCache!,
                        niveauxEnCache!,
                        reussitesEnCache!,
                        difficulteFeatures!);
            }

            if (dbContextOptions is null)
            {
                return (ListeEntiereClassements,
                        ListeEntiereCreateursNiveaux,
                        ListeEntiereUtilisateurs,
                        ListeEntiereNiveaux,
                        ListeEntiereReussitesNiveaux,
                        ListeEntiereFeatures);
            }

            using (var dbContext = new TheBandListWebDbContext(dbContextOptions))
            {
                ListeEntiereNiveaux = dbContext.Niveaux
                    .AsNoTracking()
                    .Include(n => n.Rating)
                        .ThenInclude(r => r.DifficulteRate)
                    .Include(n => n.Verifieur)
                    .Include(n => n.Publisher)
                    .ToList();

                ListeEntiereClassements = dbContext.Classements
                    .AsNoTracking()
                    .ToList();

                ListeEntiereCreateursNiveaux = dbContext.CreateursNiveaux
                    .AsNoTracking()
                    .Include(cn => cn.Createur)
                    .ToList();

                ListeEntiereReussitesNiveaux = dbContext.ReussitesNiveaux
                    .AsNoTracking()
                    .Include(r => r.Utilisateur)
                    .ToList();

                ListeEntiereUtilisateurs = dbContext.Utilisateurs
                    .AsNoTracking()
                    .ToList();

                ListeEntiereFeatures = dbContext.DifficulteFeatures
                    .AsNoTracking()
                    .ToList();
            }

            _cache.Set(KEY_CLASSEMENTS, ListeEntiereClassements, DefaultTtl);
            _cache.Set(KEY_CREATEURS, ListeEntiereCreateursNiveaux, DefaultTtl);
            _cache.Set(KEY_UTILISATEURS, ListeEntiereUtilisateurs, DefaultTtl);
            _cache.Set(KEY_NIVEAUX, ListeEntiereNiveaux, DefaultTtl);
            _cache.Set(KEY_REUSSITES, ListeEntiereReussitesNiveaux, DefaultTtl);
            _cache.Set(KEY_FEATURES, ListeEntiereFeatures, DefaultTtl);

            return (ListeEntiereClassements,
                    ListeEntiereCreateursNiveaux,
                    ListeEntiereUtilisateurs,
                    ListeEntiereNiveaux,
                    ListeEntiereReussitesNiveaux,
                    ListeEntiereFeatures);
        }

        public void ClearCache()
        {
            _cache.Remove(KEY_CLASSEMENTS);
            _cache.Remove(KEY_CREATEURS);
            _cache.Remove(KEY_UTILISATEURS);
            _cache.Remove(KEY_NIVEAUX);
            _cache.Remove(KEY_REUSSITES);
            _cache.Remove(KEY_FEATURES);
        }
    }
}
