using System.ComponentModel.DataAnnotations;

namespace TheBandList.Web.Entities
{
    public class Pack
    {
        [Key]
        public int Id { get; set; }
        public string Nom { get; set; }
        public int PointsBonus { get; set; }
    }
}
