using System.ComponentModel.DataAnnotations;

namespace TheBandList.Web.Entities
{
    public class NiveauDifficulteRate
    {
        [Key]
        public int Id { get; set; }
        public string NomDeLaDifficulte { get; set; }
    }
}
