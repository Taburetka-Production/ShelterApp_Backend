using Microsoft.EntityFrameworkCore;


namespace ShelterApp.Data
{
    public interface IUnitOfWork : IDisposable
    {
        public IRepository<User> UserRepository { get; }
        public IRepository<Animal> AnimalRepository { get; }
        public IRepository<Shelter> ShelterRepository { get; }
        public AddressRepository AddressRepository { get; }
        public IRepository<AdoptionRequest> AdoptionRequestRepository { get; }
        public IRepository<UsersAnimal> UsersAnimalRepository { get; }
        public IRepository<UsersShelter> UsersShelterRepository { get; }
        public IRepository<ShelterFeedback> ShelterFeedbackRepository { get; }
        public IRepository<AnimalPhoto> AnimalPhotoRepository { get; }

        public void Save();

        Task SaveAsync();

        Task<StatisticsView> GetStats();
    }
}
