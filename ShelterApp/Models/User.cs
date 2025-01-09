using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ShelterApp
{
    public class User : IdentityUser
    {
        public string? AvatarUrl { get; set; }
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public int? Age { get; set; }
    }
}