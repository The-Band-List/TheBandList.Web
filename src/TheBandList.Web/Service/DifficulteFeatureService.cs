using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using TheBandList.Web.Entities;
using TheBandList.Web.Entities.Context;

namespace TheBandList.Web.Service
{
    public class DifficulteFeatureService
    {
        private readonly string imageDirectory;
        private readonly TheBandListWebDbContext _context;
        private readonly ILogger<DifficulteFeatureService> _logger;

        private static readonly Regex DataUri = new(
            @"^data:image/(?<type>[^;]+);base64,(?<data>.+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly HashSet<string> ExtensionsAutorisees =
            new(StringComparer.OrdinalIgnoreCase) { ".gif", ".png", ".jpg", ".jpeg", ".webp" };

        public DifficulteFeatureService(TheBandListWebDbContext context,
            IWebHostEnvironment env,
            ILogger<DifficulteFeatureService> logger)
        {
            _context = context;
            _logger = logger;

#if DEBUG
            var root = Path.Combine(env.ContentRootPath, "wwwroot", "PicturesDev");
#else
            var root = "/var/thebandlist/keys";
#endif            
            imageDirectory = Path.Combine(root, "DemonsFaces");
            EnsureDirectoryExists(imageDirectory);
        }

        public async Task<int> UpdateAllDemonFaceImagesAsync(List<DifficulteFeature> items)
        {
            EnsureDirectoryExists(imageDirectory);

            int changements = 0;

            foreach (var df in items)
            {
                bool modified = false;

                if (string.IsNullOrWhiteSpace(df.Image))
                    continue;

                try
                {
                    var (bytes, hinted) = DecodeBase64(df.Image);
                    var extSansPoint = GetExt(bytes, hinted);
                    if (extSansPoint is null)
                        continue;

                    var ext = "." + extSansPoint;
                    var cible = Path.Combine(imageDirectory, $"{df.Id}{ext}");
                    var existant = FindExistingPathForId(df.Id);

                    bool doitEcrire =
                        existant is null ||
                        !File.ReadAllBytes(existant).AsSpan().SequenceEqual(bytes) ||
                        !existant.EndsWith(ext, StringComparison.OrdinalIgnoreCase);

                    if (doitEcrire)
                    {
                        if (existant is not null && File.Exists(existant) && existant != cible)
                            File.Delete(existant);

                        WriteAtomic(cible, bytes);
                        changements++;
                    }

                    df.Image = null;
                    modified = true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erreur DemonFace {Id}", df.Id);
                }

                if (modified)
                    _context.Entry(df).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();
            return changements;
        }

        private void EnsureDirectoryExists(string d) { if (!Directory.Exists(d)) Directory.CreateDirectory(d); }

        private static (byte[] bytes, string? hinted) DecodeBase64(string input)
        {
            var m = DataUri.Match(input);
            var data = m.Success ? m.Groups["data"].Value : input;
            var hinted = m.Success ? m.Groups["type"].Value : null;
            data = data.Trim().Replace(" ", "+");
            return (Convert.FromBase64String(data), hinted);
        }

        private static string? GetExt(byte[] b, string? hinted)
        {
            if (b.Length >= 6 && b[0] == 'G' && b[1] == 'I' && b[2] == 'F' && b[3] == '8' && (b[4] == '7' || b[4] == '9') && b[5] == 'a') return "gif";
            if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return "png";
            if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "jpg";
            if (b.Length >= 12 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F' && b[8] == 'W') return "webp";
            return hinted?.Split('/').Last();
        }

        private string? FindExistingPathForId(int id) =>
            Directory.EnumerateFiles(imageDirectory, $"{id}.*")
            .Where(p => ExtensionsAutorisees.Contains(Path.GetExtension(p)))
            .FirstOrDefault();

        private static void WriteAtomic(string path, byte[] bytes)
        {
            var tmp = Path.Combine(Path.GetDirectoryName(path)!, Guid.NewGuid().ToString("N") + ".tmp");
            File.WriteAllBytes(tmp, bytes);
            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }
    }
}