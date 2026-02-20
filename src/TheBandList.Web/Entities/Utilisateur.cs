using System.ComponentModel.DataAnnotations;

namespace TheBandList.Web.Entities
{
    public class Utilisateur
    {
        [Key]
        public int Id { get; set; }
        public string Nom { get; set; }

        public ICollection<DiscordAccount> ComptesDiscord { get; set; } = new List<DiscordAccount>();
    }
}
