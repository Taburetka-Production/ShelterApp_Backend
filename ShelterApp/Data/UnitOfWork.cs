using Microsoft.EntityFrameworkCore;

namespace ShelterApp.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDbContext _context;
        private IRepository<User> _userRepository;
        private IRepository<Animal> _animalRepository;
        private IRepository<Shelter> _shelterRepository;
        private IRepository<Address> _addressRepository;
        private IRepository<AdoptionRequest> _adoptionRequestRepository;

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
        public  IRepository<Address> AddressRepository =>
            _addressRepository ??= new GenericRepository<Address>(_context);
        public IRepository<AdoptionRequest> AdoptionRequestRepository =>
            _adoptionRequestRepository ??= new GenericRepository<AdoptionRequest>(_context);

        public void Save()
        {
            _context.SaveChanges();
        }
        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
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
