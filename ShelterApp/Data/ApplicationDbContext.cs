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

            builder.Entity<StatisticsView>()
                .HasNoKey()
                .ToView("totalstatistics");

            builder.Entity<UsersShelter>()
            .HasKey(us => new { us.ShelterId, us.UserId });
            
            builder.Entity<UsersAnimal>()
            .HasKey(us => new { us.AnimalId, us.UserId });

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