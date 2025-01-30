using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShelterApp.Tests
{
    public class SheltersControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly SheltersController _controller;

        public SheltersControllerTests()
        {
            _dbContext = new TestsSetup().GetInMemoryDbContext();
            _controller = new SheltersController(_dbContext);
        }

        public void Dispose()
        {
            _dbContext.Database.GetDbConnection().Close();
            _dbContext.Dispose();
        }

        [Fact]
        public async Task GetShelters_WhenListIsNotNull_ReturnsSheltersList()
        {
            // arrange

            var address = new Address
            {
                Id = Guid.NewGuid(),
                Country = "test",
                Region = "test",
                District = "test",
                City = "test"
            };

            var mockShelters = new List<Shelter>
            {
                new Shelter
                {
                    Name = "Test",
                    Rating = 0,
                    ReviewsCount = 0,
                    Description = "Test",
                    ImageUrl = "test",
                    AddressId = address.Id,
                    Address = address
                }
            };
            _dbContext.Shelters.AddRange(mockShelters);
            _dbContext.SaveChanges();

            var count = _dbContext.Shelters.Count();
            Console.WriteLine($"Number of shelters in the database: {count}");

            // act
            var result = await _controller.GetShelters();

            // assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsAssignableFrom<IEnumerable<Shelter>>(okResult.Value);
            Assert.Equal(1, returnValue.Count());
            Assert.Equal("Test", returnValue.First().Name);
            Assert.Equal(0, returnValue.First().Rating);
        }

        [Fact]  
        public async Task GetShelters_WhenListIsNull_ReturnsNotFound()
        {
            // arrange
            _dbContext.Shelters.RemoveRange(_dbContext.Shelters);
            _dbContext.Shelters = null;
            _dbContext.SaveChanges();

            var result = await _controller.GetShelters();

            var notFoundResult = Assert.IsType<NotFoundResult>(result.Result);
        }
    }
}
