using System.ComponentModel.DataAnnotations.Schema;

namespace TheBandList.Web.Entities
{
    public class ReussitePack
    {
        [ForeignKey("UtilisateurId")]
        public int UtilisateurId { get; set; }
        public virtual Utilisateur Utilisateur { get; set; }

        [ForeignKey("PackId")]
        public int PackId { get; set; }
        public virtual Pack Pack { get; set; }

        public DateTime DateReussite { get; set; }
    }
}
