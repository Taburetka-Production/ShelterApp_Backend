using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelterApp;
using System;
using System.Linq;
using System.Threading.Tasks;
using ShelterApp.Data;

namespace ShelterApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly DateTime _initiativeStartDate = new DateTime(2025, 1, 1); // Дата початку ініціативи

        public StatisticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/statistics
        [HttpGet]
        public async Task<IActionResult> GetStatistics()
        {
            var totalShelters = await _context.Shelters.CountAsync();
            var totalAnimals = await _context.Animals.CountAsync();
            var totalSponsors = await _context.Users.CountAsync();
            var totalRegions = await _context.Addresses.Select(a => a.Region).Distinct().CountAsync();
            var initiativeDays = (DateTime.UtcNow - _initiativeStartDate).Days;

            var monthlyAdoptions = await _context.AdoptionRequests.CountAsync();

            return Ok(new
            {
                TotalShelters = totalShelters,
                TotalAnimals = totalAnimals,
                TotalSponsors = totalSponsors,
                InitiativeDays = initiativeDays,
                TotalRegions = totalRegions,
                MonthlyAdoptions = monthlyAdoptions
            });
        }
    }
}