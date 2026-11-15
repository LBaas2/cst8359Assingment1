using System.ComponentModel.DataAnnotations;

namespace Assignment1.DTOs
{
    public class EventDto
    {
        [Required]
        public string Title { get; set; }

        public string Description { get; set; } =string.Empty;

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public string Location { get; set; }

        
        public IFormFile? BannerImage { get; set; }
    }
}
