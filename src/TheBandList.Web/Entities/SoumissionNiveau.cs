using System.ComponentModel.DataAnnotations;

namespace TheBandList.Web.Entities
{
    public class SoumissionNiveau
    {
        [Key]
        public int IdSoumission { get; set; }
        public string NomNiveau { get; set; }
        public string NomUtilisateur { get; set; }
        public string UrlVideo { get; set; }
        public DateTime DateSoumission { get; set; } = DateTime.Now;
    }
}
