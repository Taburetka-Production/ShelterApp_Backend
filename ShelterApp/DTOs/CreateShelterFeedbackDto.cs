using System.ComponentModel.DataAnnotations;

namespace ShelterApp
{
    public class CreateShelterFeedbackDto
    {
        [Required]
        [StringLength(1000)]
        public string Comment { get; set; }

        [Required]
        [Range(1.0, 5.0)]
        public double Rating { get; set; }
    }
}
