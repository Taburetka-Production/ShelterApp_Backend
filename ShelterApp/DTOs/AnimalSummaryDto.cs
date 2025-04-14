namespace ShelterApp
{
    public class AnimalSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Species { get; set; }
        public string Status { get; set; }
        public string Slug { get; set; }
        public string? PrimaryPhotoUrl { get; set; }
    }
}
