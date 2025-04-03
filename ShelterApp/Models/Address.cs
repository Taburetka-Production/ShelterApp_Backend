﻿using System.ComponentModel.DataAnnotations;

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
        [Required]
        public string Street { get; set; }
        [Required]
        public string Apartments { get; set; }
        [Required]
        public double lng { get; set; }
        [Required]
        public double lat { get; set; }
    }
}