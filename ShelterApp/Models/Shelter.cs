using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ShelterApp
{
    public class Shelter : BaseEntity
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public double Rating { get; set; }
        [Required]
        public int ReviewsCount { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public string ImageUrl { get; set; }
        [Required]
        public Guid AddressId { get; set; }
        [ForeignKey("AddressId")]
        public Address Address { get; set; }
    }
}