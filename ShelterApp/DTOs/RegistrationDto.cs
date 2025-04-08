using System.ComponentModel.DataAnnotations;

namespace ShelterApp
{
    public class RegistrationDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [Required]
        public string UserName { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Surname { get; set; }

        [Required]
        [Range(1, 120)]
        public int Age { get; set; }

        public string? AvatarUrl { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
