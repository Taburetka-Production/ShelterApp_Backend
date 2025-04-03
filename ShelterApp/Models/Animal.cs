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
        public string Sex { get; set; }       // Додано
        public string Size { get; set; }      // Додано
        public bool Sterilized { get; set; }  // Додано
        public string HealthCondition { get; set; } // Додано
        public string Description { get; set; }     // Додано
        [Required]
        public Guid ShelterId { get; set; }
        [ForeignKey("ShelterId")]
        public Shelter Shelter { get; set; }

        // Замінено PhotoURL на колекцію фотографій
        public virtual ICollection<AnimalPhoto> Photos { get; set; } = new List<AnimalPhoto>();
        public virtual ICollection<UsersAnimal>? UsersAnimal { get; set; }
    }
}