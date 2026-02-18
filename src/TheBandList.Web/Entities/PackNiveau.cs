using System.ComponentModel.DataAnnotations.Schema;

namespace TheBandList.Web.Entities
{
    public class PackNiveau
    {
        [ForeignKey("PackId")]
        public int PackId { get; set; }
        public virtual Pack Pack { get; set; }

        [ForeignKey("NiveauId")]
        public int NiveauId { get; set; }
        public virtual Niveau Niveau { get; set; }
    }
}
