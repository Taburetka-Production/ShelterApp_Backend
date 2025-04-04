﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ShelterApp
{
    public class AnimalPhoto : BaseEntity
    {
        [Required]
        public string PhotoURL { get; set; }
        public Guid AnimalId { get; set; }
        [ForeignKey("AnimalId")]
        public Animal Animal { get; set; }
    }
}
