using System.ComponentModel.DataAnnotations;

namespace Assignment1.Models
{
    public class Attendee
    {
        [Key]
        public int id { get; set; }
        [Required]
        public string name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string email { get; set; } = string.Empty;

        public int EventID { get; set; }
    }
}
