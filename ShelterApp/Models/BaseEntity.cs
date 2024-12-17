namespace ShelterApp
{
    public class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public Guid? UserLastModified { get; set; }
    }
}
