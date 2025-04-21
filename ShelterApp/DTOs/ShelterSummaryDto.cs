namespace ShelterApp
{
    public class ShelterSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public double Rating { get; set; }
        public int AnimalsCount { get; set; }
        public string ImageUrl { get; set; }
        public string Slug { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
        public string Description { get; set; }
        public AddressDto Address { get; set; }

        public bool IsSaved { get; set; }
    }
}
