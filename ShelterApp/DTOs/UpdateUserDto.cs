using System.ComponentModel.DataAnnotations;

namespace ShelterApp
{
    public class UpdateUserDto
    {
        public string? UserName { get; set; }
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public int? Age { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        [EmailAddress]
        public string Email { get; set; }
    }

}
