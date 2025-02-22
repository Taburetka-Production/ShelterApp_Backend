namespace ShelterApp
{
    public class UpdateShelterDto
    {
        public string? Name { get; set; }
        public double? Rating { get; set; }
        public int? ReviewsCount { get; set; }
        public int? AnimalsCount { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public Guid? AddressId { get; set; }
    }
}