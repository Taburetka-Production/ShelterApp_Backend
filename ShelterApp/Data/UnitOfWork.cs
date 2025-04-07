using Microsoft.EntityFrameworkCore;
using ShelterApp.Controllers;

namespace ShelterApp.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDbContext _context;
        private IRepository<User> _userRepository;
        private IRepository<Animal> _animalRepository;
        private IRepository<Shelter> _shelterRepository;
        private AddressRepository _addressRepository;
        private IRepository<AdoptionRequest> _adoptionRequestRepository;
        private IRepository<UsersAnimal> _usersAnimalRepository;
        private IRepository<UsersShelter> _usersShelterRepository;
        private IRepository<ShelterFeedback> _shelterFeedbackRepository;
        private IRepository<AnimalPhoto> _animalPhotoRepository;

        public UnitOfWork(DbContextOptions<ApplicationDbContext> options)
        {
            _context = new ApplicationDbContext(options);
        }

        public IRepository<User> UserRepository =>
            _userRepository ??= new GenericRepository<User>(_context);
        public IRepository<Animal> AnimalRepository =>
            _animalRepository ??= new GenericRepository<Animal>(_context);
        public IRepository<Shelter> ShelterRepository =>
            _shelterRepository ??= new GenericRepository<Shelter>(_context);
        public AddressRepository AddressRepository =>
            _addressRepository ??= new AddressRepository(_context);
        public IRepository<AdoptionRequest> AdoptionRequestRepository =>
            _adoptionRequestRepository ??= new GenericRepository<AdoptionRequest>(_context);
        public IRepository<UsersAnimal> UsersAnimalRepository =>
        _usersAnimalRepository ??= new GenericRepository<UsersAnimal>(_context);
        public IRepository<UsersShelter> UsersShelterRepository =>
            _usersShelterRepository ??= new GenericRepository<UsersShelter>(_context);
        public IRepository<ShelterFeedback> ShelterFeedbackRepository =>
            _shelterFeedbackRepository ??= new GenericRepository<ShelterFeedback>(_context);
        public IRepository<AnimalPhoto> AnimalPhotoRepository =>
        _animalPhotoRepository ??= new GenericRepository<AnimalPhoto>(_context);


        public void Save()
        {
            _context.SaveChanges();
        }
        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<StatisticsView> GetStats()
        {
            var stats = await _context.Statistics.FirstOrDefaultAsync();

            return stats;
        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
