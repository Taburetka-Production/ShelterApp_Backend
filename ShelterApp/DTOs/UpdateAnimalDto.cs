namespace ShelterApp
{
    public class UpdateAnimalDto
    {
        public string? Name { get; set; }
        public string? Species { get; set; }
        public string? Breed { get; set; }
        public int? Age { get; set; }
        public string? Status { get; set; }

        // Нові поля
        public string? Sex { get; set; }
        public string? Size { get; set; }
        public bool? Sterilized { get; set; }
        public string? HealthCondition { get; set; }
        public string? Description { get; set; }
    }
}
