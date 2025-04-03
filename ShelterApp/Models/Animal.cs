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
        [Required]
        public string Breed { get; set; }
        [Required]
        public int? Age { get; set; }
        [Required]
        public string Status { get; set; }
        [Required]
        public string Sex { get; set; }
        [Required]
        public string Size { get; set; }
        [Required]
        public bool Sterilized { get; set; }  
        public string? HealthCondition { get; set; } 
        public string Description { get; set; }    
        [Required]
        public Guid ShelterId { get; set; }
        [ForeignKey("ShelterId")]
        public Shelter Shelter { get; set; }

        // Замінено PhotoURL на колекцію фотографій
        public virtual ICollection<AnimalPhoto> Photos { get; set; } = new List<AnimalPhoto>();
        public virtual ICollection<UsersAnimal>? UsersAnimal { get; set; }
    }
}