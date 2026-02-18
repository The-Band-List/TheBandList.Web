using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TheBandList.Web.Entities
{
    public class Classement
    {
        [Key]
        public int Id { get; set; }
        public int ClassementPosition { get; set; }
        public int Points { get; set; }

        [ForeignKey("NiveauId")]
        public int NiveauId { get; set; }
        public virtual Niveau Niveau { get; set; }
    }
}
