namespace ShelterApp
{
    public class AnimalSummaryDto
    {
        public string Name { get; set; }
        public string Species { get; set; }
        public string Status { get; set; }
        public string Slug { get; set; }
        public string? PrimaryPhotoUrl { get; set; }
        public bool IsSaved { get; set; }
    }
}
