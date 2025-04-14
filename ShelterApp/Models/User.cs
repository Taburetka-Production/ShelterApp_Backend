using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShelterApp
{
    public class User : IdentityUser
    {
        public string? AvatarUrl { get; set; }
        [Required]
        public string? Name { get; set; }
        [Required]
        public string? Surname { get; set; }
        [Required]
        public int? Age { get; set; }

        public Shelter? Shelter { get; set; }

        public AdoptionRequest? AdoptionRequest { get; set; }

        public virtual ICollection<UsersShelter> UsersShelters { get; set; } = new List<UsersShelter>();
        public virtual ICollection<ShelterFeedback> UserFeedbacks { get; set; } = new List<ShelterFeedback>();

        public virtual ICollection<UsersAnimal> UsersAnimals { get; set; } = new List<UsersAnimal>();
    }
}