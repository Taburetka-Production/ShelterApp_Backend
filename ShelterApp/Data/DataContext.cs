using Microsoft.EntityFrameworkCore;
using ShelterApp.Models;

namespace ShelterApp.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) 
        { 

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseSerialColumns();
        }

        public DbSet<User> User { get; set; }
    }
}
