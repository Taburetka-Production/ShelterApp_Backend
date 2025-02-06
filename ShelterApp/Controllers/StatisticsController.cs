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
        private IUnitOfWork _unitOfWork;
        private readonly DateTime _initiativeStartDate = new DateTime(2025, 1, 1); // Дата початку ініціативи

        public StatisticsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: api/statistics
        [HttpGet]
        public async Task<IActionResult> GetStatistics()
        {
            var totalShelters = await _unitOfWork.ShelterRepository.CountAsync();
            var totalAnimals = await _unitOfWork.AnimalRepository.CountAsync();
            var totalSponsors = await _unitOfWork.UserRepository.CountAsync();
            var totalRegions = await _unitOfWork.AddressRepository.RegionCount();
            var initiativeDays = (DateTime.UtcNow - _initiativeStartDate).Days;

            var monthlyAdoptions = await _unitOfWork.AdoptionRequestRepository.CountAsync();

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