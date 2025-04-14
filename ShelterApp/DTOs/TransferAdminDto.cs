using System.ComponentModel.DataAnnotations;

namespace ShelterApp
{
    public class TransferAdminDto
    {
        [Required]
        [EmailAddress]
        public string NewAdminEmail { get; set; }
    }
}
