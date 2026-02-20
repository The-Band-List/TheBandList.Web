using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using TheBandList.Web.Entities;
using TheBandList.Web.Entities.Context;

namespace TheBandList.Web.Service
{
    public class NiveauService
    {
        private readonly string verificationDir;
        private readonly string niveauDir;
        private readonly TheBandListWebDbContext _context;
        private readonly ILogger<NiveauService> _logger;

        private static readonly Regex DataUriPrefix =
            new(@"^data:image\/[a-zA-Z0-9.+-]+;base64,", RegexOptions.Compiled);

        public NiveauService(TheBandListWebDbContext context, IWebHostEnvironment env, ILogger<NiveauService> logger)
        {
            _context = context;
            _logger = logger;

#if DEBUG
            var root = Path.Combine(env.ContentRootPath, "wwwroot", "PicturesDev");
#else
            var root = "/var/thebandlist/keys";
#endif

            verificationDir = Path.Combine(root, "MiniaturesVideosVerification");
            niveauDir = Path.Combine(root, "MiniaturesNiveaux");

            EnsureDirectoryExists(verificationDir);
            EnsureDirectoryExists(niveauDir);
        }

        public async Task<int> UpdateAllNiveauxImagesAsync(List<Niveau> niveaux)
        {
            EnsureDirectoryExists(verificationDir);
            EnsureDirectoryExists(niveauDir);

            int changements = 0;

            foreach (var n in niveaux)
            {
                bool modified = false;

                if (!string.IsNullOrWhiteSpace(n.MiniatureNiveau))
                {
                    var path = Path.Combine(niveauDir, $"{n.Id}.png");

                    try
                    {
                        var bytes = DecodeBase64ToBytes(n.MiniatureNiveau);

                        if (NeedsWrite(path, bytes))
                        {
                            WriteAtomic(path, bytes);
                            _logger.LogInformation("MAJ miniature niveau {Id}", n.Id);
                            changements++;
                        }

                        n.MiniatureNiveau = null;
                        modified = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erreur MiniatureNiveau {Id}", n.Id);
                    }
                }

                if (!string.IsNullOrWhiteSpace(n.MiniatureVideoVerification))
                {
                    var path = Path.Combine(verificationDir, $"{n.Id}.png");

                    try
                    {
                        var bytes = DecodeBase64ToBytes(n.MiniatureVideoVerification);

                        if (NeedsWrite(path, bytes))
                        {
                            WriteAtomic(path, bytes);
                            _logger.LogInformation("MAJ miniature verification {Id}", n.Id);
                            changements++;
                        }

                        n.MiniatureVideoVerification = null;
                        modified = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erreur MiniatureVideoVerification {Id}", n.Id);
                    }
                }

                if (modified)
                    _context.Entry(n).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();

            return changements;
        }

        private static byte[] DecodeBase64ToBytes(string input)
        {
            var b64 = DataUriPrefix.Replace(input.Trim(), string.Empty)
                                   .Replace(" ", "+");
            return Convert.FromBase64String(b64);
        }

        private static bool NeedsWrite(string path, byte[] newBytes)
        {
            if (!File.Exists(path)) return true;

            try
            {
                var oldBytes = File.ReadAllBytes(path);
                return oldBytes.Length != newBytes.Length ||
                       !oldBytes.AsSpan().SequenceEqual(newBytes);
            }
            catch
            {
                return true;
            }
        }

        private static void WriteAtomic(string path, byte[] bytes)
        {
            var dir = Path.GetDirectoryName(path)!;
            var tmp = Path.Combine(dir, Guid.NewGuid().ToString("N") + ".tmp");

            File.WriteAllBytes(tmp, bytes);

            if (File.Exists(path))
                File.Replace(tmp, path, null);
            else
                File.Move(tmp, path);
        }

        private void EnsureDirectoryExists(string dir)
        {
            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    _logger.LogInformation("Dossier créé : {Dir}", dir);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Impossible de créer le dossier {Dir}", dir);
                throw;
            }
        }
    }
}
