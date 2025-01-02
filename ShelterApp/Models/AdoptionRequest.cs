using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ShelterApp
{
    public class AdoptionRequest : BaseEntity
    {
        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public IdentityUser User { get; set; }

        [Required]
        public Guid AnimalId { get; set; }

        [ForeignKey("AnimalId")]
        public Animal Animal { get; set; }

        [Required]
        public DateTime RequestDate { get; set; }

        [Required]
        public string Status { get; set; }
    }
}
