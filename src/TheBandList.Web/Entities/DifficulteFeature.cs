using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TheBandList.Web.Entities
{
    public class DifficulteFeature
    {
        [Key]
        public int Id { get; set; }
        public string NomDuFeature { get; set; }
        public string? Image { get; set; }

        [NotMapped]
#if DEBUG
        public string ImageUrl => $"/PicturesDev/DemonsFaces/{Id}.gif";
#else
        public string ImageUrl => $"/DemonsFaces/{Id}.gif";
#endif
        [ForeignKey("DifficulteRateId")]
        public int DifficulteRateId { get; set; }
        public virtual NiveauDifficulteRate DifficulteRate { get; set; }
    }
}
