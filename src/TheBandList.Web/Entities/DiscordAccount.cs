namespace TheBandList.Web.Entities
{
    public class DiscordAccount
    {
        public int Id { get; set; }

        public string DiscordId { get; set; } = default!;

        public string? Username { get; set; }
        public string? AvatarHash { get; set; }

        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? AccessTokenExpiresAt { get; set; }

        public int UtilisateurId { get; set; }
        public Utilisateur Utilisateur { get; set; } = default!;
    }
}
