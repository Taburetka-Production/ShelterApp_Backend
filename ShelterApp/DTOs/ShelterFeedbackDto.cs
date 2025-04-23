namespace ShelterApp
{
    public class ShelterFeedbackDto
    {
        public Guid Id { get; set; }
        public string Comment { get; set; }
        public double Rating { get; set; }
        public UserSummaryDto User { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
