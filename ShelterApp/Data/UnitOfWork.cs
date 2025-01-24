using Microsoft.EntityFrameworkCore;

namespace ShelterApp.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDbContext _context;
        private GenericRepository<User> _userRepository;
        private GenericRepository<Animal> _animalRepository;
        private GenericRepository<Shelter> _shelterRepository;
        private GenericRepository<Address> _addressRepository;
        private GenericRepository<AdoptionRequest> _adoptionRequestRepository;

        public UnitOfWork(DbContextOptions<ApplicationDbContext> options)
        {
            _context = new ApplicationDbContext(options);
        }

        public GenericRepository<User> UserRepository =>
            _userRepository ??= new GenericRepository<User>(_context);
        public GenericRepository<Animal> AnimalRepository =>
            _animalRepository ??= new GenericRepository<Animal>(_context);
        public GenericRepository<Shelter> ShelterRepository =>
            _shelterRepository ??= new GenericRepository<Shelter>(_context);
        public GenericRepository<Address> AddressRepository =>
            _addressRepository ??= new GenericRepository<Address>(_context);
        public GenericRepository<AdoptionRequest> AdoptionRequestRepository =>
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
