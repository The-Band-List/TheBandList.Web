using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TheBandList.Web.Entities
{
    public class Niveau
    {
        [Key]
        public int Id { get; set; }
        public string IdDuNiveauDansLeJeu { get; set; }
        public string Nom { get; set; }
        public string MotDePasse { get; set; }
        public string UrlIframeSrcVerification { get; set; }
        public string UrlVerification { get; set; }
        public string? MiniatureVideoVerification { get; set; }
        public string? MiniatureNiveau { get; set; }
        public int Duree { get; set; }
        public DateTime DateAjout { get; set; }
        public int? Placement { get; set; }

        [NotMapped]
        public string MiniatureUrl => $"/Miniatures/{Id}.png";

        [ForeignKey("VerifieurId")]
        public int VerifieurId { get; set; }
        public virtual Utilisateur Verifieur { get; set; }

        [ForeignKey("PublisherId")]
        public int PublisherId { get; set; }
        public virtual Utilisateur Publisher { get; set; }

        [ForeignKey("RatingId")]
        public int RatingId { get; set; }
        public virtual DifficulteFeature Rating { get; set; }
    }
}
