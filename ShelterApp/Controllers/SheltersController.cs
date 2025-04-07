using Microsoft.AspNetCore.Authorization;
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
            var shelters = await _unitOfWork.ShelterRepository.GetAllAsync(includeProperties: "Address");

            return Ok(shelters);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetShelterById(Guid id)
        {
            var shelter = await _unitOfWork.ShelterRepository.GetByIdAsync(id, includeProperties: "Address");
            if (shelter == null)
            {
                return NotFound("Shelter not found.");
            }
            return Ok(shelter);
        }

        [HttpPost]
        [Authorize(Roles = "ShelterAdmin,SuperAdmin")]
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

        //[HttpPut("UpdateShelter")]
        //[Authorize(Roles = "ShelterAdmin,SuperAdmin")]
        //public async Task<IActionResult> UpdateShelter(Guid id, [FromBody] UpdateShelterDto dto)
        //{
        //    var shelter = await _context.Shelters
        //        .Include(s => s.Address)
        //        .FirstOrDefaultAsync(s => s.Id == id);

        //    if (shelter == null)
        //    {
        //        return NotFound("Shelter not found.");
        //    }

        //    shelter.Name = dto.Name ?? shelter.Name;
        //    shelter.Rating = dto.Rating ?? shelter.Rating;
        //    shelter.ReviewsCount = dto.ReviewsCount ?? shelter.ReviewsCount;
        //    shelter.AnimalsCount = dto.AnimalsCount ?? shelter.AnimalsCount;
        //    shelter.Description = dto.Description ?? shelter.Description;
        //    shelter.ImageUrl = dto.ImageUrl ?? shelter.ImageUrl;

        //    if (dto.AddressId.HasValue && dto.AddressId != shelter.AddressId)
        //    {
        //        var newAddress = await _context.Addresses.FindAsync(dto.AddressId.Value);
        //        if (newAddress == null)
        //        {
        //            return NotFound("New address not found.");
        //        }
        //        shelter.AddressId = newAddress.Id;
        //        shelter.Address = newAddress;
        //    }

        //    shelter.UpdatedAtUtc = DateTime.UtcNow;

        //    await _context.SaveChangesAsync();

        //    return Ok(shelter);
        //}

        [HttpDelete("{id}")]
        [Authorize(Roles = "ShelterAdmin,SuperAdmin")]
        public async Task<IActionResult> DeleteShelter(Guid id)
        {

            // Отримання притулку з репозиторію
            var shelter = await _unitOfWork.ShelterRepository.GetByIdAsync(id, includeProperties: "Address,UsersShelters");
            if (shelter == null)
            {
                return NotFound("Shelter not found.");
            }

            // Видалення UsersShelters (зв'язки користувачів з притулком)
            var usersShelters = await _unitOfWork.UsersShelterRepository.GetAllAsync(us => us.ShelterId == id);
            _unitOfWork.UsersShelterRepository.RemoveRange(usersShelters);

            // Видалення відгуків (ShelterFeedback)
            var shelterFeedbacks = await _unitOfWork.ShelterFeedbackRepository.GetAllAsync(sf => sf.ShelterId == id);
            _unitOfWork.ShelterFeedbackRepository.RemoveRange(shelterFeedbacks);

            // Видалення тварин (Animal) та їхніх залежностей
            var animals = await _unitOfWork.AnimalRepository.GetAllAsync(a => a.ShelterId == id);
            foreach (var animal in animals)
            {
                // Видалення фотографій тварини (AnimalPhoto)
                var animalPhotos = await _unitOfWork.AnimalPhotoRepository.GetAllAsync(ap => ap.AnimalId == animal.Id);
                _unitOfWork.AnimalPhotoRepository.RemoveRange(animalPhotos);

                // Видалення зв'язків UsersAnimal
                var usersAnimals = await _unitOfWork.UsersAnimalRepository.GetAllAsync(ua => ua.AnimalId == animal.Id);
                _unitOfWork.UsersAnimalRepository.RemoveRange(usersAnimals);

                // Видалення заявок на усиновлення (AdoptionRequest)
                var adoptionRequests = await _unitOfWork.AdoptionRequestRepository.GetAllAsync(ar => ar.AnimalId == animal.Id);
                _unitOfWork.AdoptionRequestRepository.RemoveRange(adoptionRequests);
            }
            _unitOfWork.AnimalRepository.RemoveRange(animals); // Видалити всіх тварин притулку

            // Видалення самого притулку
            _unitOfWork.ShelterRepository.Remove(shelter);

            // Збереження змін
            await _unitOfWork.SaveAsync();

            return NoContent();
        }
    }
}
