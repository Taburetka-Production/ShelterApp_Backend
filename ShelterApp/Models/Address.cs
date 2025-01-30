using System.ComponentModel.DataAnnotations;

namespace ShelterApp
{
    public class Address : BaseEntity
    {
        [Required]
        public string Country { get; set; }
        [Required]
        public string Region { get; set; }
        [Required]
        public string District { get; set; }
        [Required]
        public string City { get; set; }
        public string? Street { get; set; }
        public string? Apartments { get; set; }
        public string? Coordinates { get; set; }
    }
}