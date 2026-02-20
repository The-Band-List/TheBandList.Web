using System.ComponentModel.DataAnnotations.Schema;

namespace TheBandList.Web.Entities
{
    public class CreateurNiveau
    {
        [ForeignKey("CreateurId")]
        public int CreateurId { get; set; }
        public virtual Utilisateur Createur { get; set; }

        [ForeignKey("NiveauId")]
        public int NiveauId { get; set; }
        public virtual Niveau Niveau { get; set; }
    }
}
