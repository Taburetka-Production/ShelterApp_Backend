namespace ShelterApp.DTOs
{
    public class AnimalResponseDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string Name { get; set; }
        public string Species { get; set; }
        public string Breed { get; set; }
        public int? Age { get; set; }
        public string Sex { get; set; }
        public string Size { get; set; }
        public bool Sterilized { get; set; }
        public string Slug { get; set; }
        public string HealthCondition { get; set; }
        public string Description { get; set; }
        public string FirstPhotoUrl { get; set; } // Перше фото з масиву
    }

    public class ShelterResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public double Rating { get; set; }
        public int AnimalsCount { get; set; }
        public string ImageUrl { get; set; }
        public string Slug { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string Description { get; set; }
    }
}
