namespace ShelterApp
{
    public class Shelter : BaseEntity
    {
        public string Name { get; set; }
        public int AddressId { get; set; }
        public Address Address { get; set; }
    }
}
