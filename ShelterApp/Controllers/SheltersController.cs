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
        public SheltersController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
            if (_unitOfWork.ShelterRepository == null)
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
                await _unitOfWork.ShelterRepository.AddAsync(shelter);
                await _unitOfWork.SaveAsync();

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
