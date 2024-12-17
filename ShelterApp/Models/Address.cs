namespace ShelterApp
{
    public class Address : BaseEntity
    {
        public string Country { get; set; }
        public string Region { get; set; }
        public string District { get; set; }
        public string City { get; set; }
        public string Street { get; set; }
        public string Apartments { get; set; }
        public string Coordinates { get; set; }
    }
}
