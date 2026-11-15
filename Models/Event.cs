using System.ComponentModel.DataAnnotations;

namespace Assignment1.Models
{
    public class Event
    {
        [Key]
        public int id { get; set; }

        [Required]
        public string title { get; set; }

        public string description { get; set; } = string.Empty;

        [Required]
        public DateTime date { get; set; }

        [Required]
        public string location { get; set; }

        public string bannerurl {get; set; }
        public List<Attendee> attendees { get; set; }

    }
}
