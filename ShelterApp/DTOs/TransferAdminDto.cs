using System.ComponentModel.DataAnnotations;

namespace ShelterApp.DTOs
{
    public class TransferAdminDto
    {
        [Required]
        [EmailAddress]
        public string NewAdminEmail { get; set; }
    }
}
