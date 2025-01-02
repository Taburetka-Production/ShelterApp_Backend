using System.ComponentModel.DataAnnotations;

namespace ShelterApp
{
    public class BaseEntity
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public Guid? UserLastModified { get; set; }
    }
}
