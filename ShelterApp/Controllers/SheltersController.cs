using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Data;

namespace ShelterApp
{
    [ApiController]
    [Route("api/[controller]")]
    public class SheltersController : ControllerBase
    {
        private IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;
        public SheltersController(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult> GetShelters()
        {
            var shelters = await _unitOfWork.ShelterRepository.GetAllAsync();

            return Ok(shelters);
        }

        [HttpPost]
        public async Task<ActionResult<Shelter>> CreateShelter([FromBody] Shelter shelter)
        {
            if (_context.Shelters == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Shelters' is null.");
            }

            // Валідація вхідних даних
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Додаємо шелтер до бази даних
                _context.Shelters.Add(shelter);
                await _context.SaveChangesAsync();

                // Повертаємо створений ресурс з його ID
                return CreatedAtAction(nameof(GetShelters), new { id = shelter.Id }, shelter);
            }
            catch (Exception ex)
            {
                // Логування або обробка помилки
                return StatusCode(500, $"Виникла помилка: {ex.Message}");
            }
        }
    }
}
