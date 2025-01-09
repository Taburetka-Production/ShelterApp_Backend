using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ShelterApp.Tests
{
    public class TestsSetup
    {
        public ApplicationDbContext GetInMemoryDbContext()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;

            var dbContext = new ApplicationDbContext(options);

            dbContext.Database.EnsureCreated();

            return dbContext;
        }
    }
}
