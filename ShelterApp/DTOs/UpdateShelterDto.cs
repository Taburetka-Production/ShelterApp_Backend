namespace ShelterApp
{
    public class UpdateShelterDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public UpdateAddressDto? Address { get; set; }
    }

    public class UpdateAddressDto
    {
        public string? Country { get; set; }
        public string? Region { get; set; }
        public string? District { get; set; }
        public string? City { get; set; }
        public string? Street { get; set; }
        public string? Apartments { get; set; }
        public double lng { get; set; }
        public double lat { get; set; }
    }
}