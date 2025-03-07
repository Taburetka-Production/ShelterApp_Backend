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
            var stats = await _unitOfWork.GetStats();
            var initiativeDays = (DateTime.UtcNow - _initiativeStartDate).Days;

            var result = new
            {
                stats.totalshelters,
                stats.totalanimals,
                stats.totalusers,
                stats.totalregions,
                stats.totaladoptions,
                initiativeDays
            };

            return Ok(result);
        }
    }
}