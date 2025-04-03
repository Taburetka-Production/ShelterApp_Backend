using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ShelterApp
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) :
            base(options) { }

        public DbSet<Shelter> Shelters { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Animal> Animals { get; set; }
        public DbSet<AdoptionRequest> AdoptionRequests { get; set; }

        public DbSet<StatisticsView> Statistics { get; set; }
        public DbSet<AnimalPhoto> AnimalPhotos { get; set; }
        public DbSet<UsersShelter> UsersShelters { get; set; }
        public DbSet<UsersAnimal> UsersAnimals { get; set; }
        public DbSet<ShelterFeedback> ShelterFeedbacks { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = Environment.GetEnvironmentVariable("DefaultConnection");
                optionsBuilder.UseNpgsql(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Налаштування для Animal
            builder.Entity<Animal>()
                .HasMany(a => a.Photos)
                .WithOne(p => p.Animal)
                .HasForeignKey(p => p.AnimalId)
                .OnDelete(DeleteBehavior.Cascade);

            // Налаштування для ShelterFeedback (додано)
            builder.Entity<ShelterFeedback>()
                .HasKey(sf => new { sf.UserId, sf.ShelterId }); // Складений ключ

            builder.Entity<ShelterFeedback>()
                .HasOne(sf => sf.User)
                .WithMany()
                .HasForeignKey(sf => sf.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ShelterFeedback>()
                .HasOne(sf => sf.Shelter)
                .WithMany()
                .HasForeignKey(sf => sf.ShelterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Налаштування для StatisticsView
            builder.Entity<StatisticsView>()
                .HasNoKey()
                .ToView("totalstatistics");

            // Налаштування для зв'язків Many-to-Many
            builder.Entity<UsersShelter>()
                .HasKey(us => new { us.ShelterId, us.UserId });

            builder.Entity<UsersAnimal>()
                .HasKey(ua => new { ua.AnimalId, ua.UserId });

            // Налаштування для Shelter
            builder.Entity<Shelter>()
                .HasOne(s => s.User)
                .WithOne(u => u.Shelter)
                .HasForeignKey<Shelter>(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Shelter>()
                .HasIndex(s => s.UserId)
                .IsUnique();
        }
    }
}