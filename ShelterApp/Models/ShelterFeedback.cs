using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShelterApp
{
    public class ShelterFeedback
    {
        [Required]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }
        [Required]
        public Guid ShelterId { get; set; }
        [ForeignKey("ShelterId")]
        public Shelter Shelter { get; set; }
        [Required]
        public string Comment { get; set; }
        [Required]
        public double Rating { get; set; }
    }
}
