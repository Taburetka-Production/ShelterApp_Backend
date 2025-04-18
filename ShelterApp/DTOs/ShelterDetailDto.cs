namespace ShelterApp
{
    public class ShelterDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public double Rating { get; set; }
        public int ReviewsCount { get; set; }
        public int AnimalsCount { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string Slug { get; set; }
        public AddressDto Address { get; set; }
        public List<AnimalSummaryDto> Animals { get; set; } = new List<AnimalSummaryDto>();
        public List<ShelterFeedbackDto> Feedbacks { get; set; } = new List<ShelterFeedbackDto>();

    }
}
