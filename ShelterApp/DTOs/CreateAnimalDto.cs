using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ShelterApp
{
    public class CreateAnimalDto
    {
        [Required] public string Name { get; set; }
        [Required] public string Species { get; set; }
        public string? Breed { get; set; }
        public int? Age { get; set; }
        [Required] public Guid ShelterId { get; set; }

        // Нові поля
        [Required] public string Sex { get; set; }
        [Required] public string Size { get; set; }
        [Required] public bool Sterilized { get; set; }
        [Required] public string HealthCondition { get; set; }
        [Required] public string Description { get; set; }
        public List<string>? PhotoUrls { get; set; } // Список URL фотографій
    }
}
