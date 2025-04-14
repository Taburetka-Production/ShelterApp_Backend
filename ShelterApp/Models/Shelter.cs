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
        public int AnimalsCount { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public string ImageUrl { get; set; }
        [Required]
        public Guid AddressId { get; set; }
        [ForeignKey("AddressId")]
        public Address Address { get; set; }
        [Required]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }
        [Required]
        public string Slug { get; set; } = String.Empty;

        public virtual ICollection<Animal> Animals { get; set; } = new List<Animal>();
        public virtual ICollection<UsersShelter> UsersShelters { get; set; } = new List<UsersShelter>();
        public virtual ICollection<ShelterFeedback> ShelterFeedbacks { get; set; } = new List<ShelterFeedback>();
    }
}