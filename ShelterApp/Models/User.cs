using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShelterApp
{
    public class User : IdentityUser
    {
        public string? AvatarUrl { get; set; }
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public int? Age { get; set; }

        public Shelter? Shelter { get; set; }

        public virtual ICollection<UsersShelter>? UsersShelters { get; set; }

        public virtual ICollection<UsersAnimal>? UsersAnimals { get; set; }
    }
}