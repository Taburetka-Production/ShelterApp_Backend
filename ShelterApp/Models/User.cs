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
        //public Guid? ShelterId { get; set; }
        //[ForeignKey("ShelterId")]
        //public Shelter? Shelter { get; set; }

        public virtual ICollection<Shelter>? Shelters { get; set; }

        public virtual ICollection<Animal>? Animals { get; set; }
    }
}