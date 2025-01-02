using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ShelterApp
{
    public class Animal : BaseEntity
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Species { get; set; }
        public string? Breed { get; set; }
        public int? Age { get; set; }
        [Required]
        public string Status { get; set; }
        public string? PhotoURL { get; set; }
        [Required]
        public Guid ShelterId { get; set; }
        [ForeignKey("ShelterId")]
        public Shelter Shelter { get; set; }
    }
}
