using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Data;

namespace ShelterApp
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnimalsController : ControllerBase
    {
        //private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;

        //public AnimalsController(ApplicationDbContext context)
        //{
        //    _context = context;
        //}

        public AnimalsController(IUnitOfWork unitOfWork) {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Animal>>> GetAnimals()
        {
            if (_unitOfWork.AnimalRepository == null)
            {
                return NotFound();
            }

            var animals = await _unitOfWork.AnimalRepository.GetAllAsync(includeProperties: "Shelter,Shelter.Address");

            //var animals = await _context.Animals
            //    .Include(a => a.Shelter)
            //    .ThenInclude(s => s.Address)
            //    .ToListAsync();

            return Ok(animals);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Animal>> GetAnimal(Guid id)
        {
            if (_unitOfWork.AnimalRepository == null)
            {
                return NotFound();
            }

            var animal = await _unitOfWork.AnimalRepository.GetByIdAsync(id, includeProperties: "Shelter,Shelter.Address");

            //var animal = await _context.Animals
            //    .Include(a => a.Shelter)
            //    .ThenInclude(s => s.Address)
            //    .FirstOrDefaultAsync(a => a.Id == id);

            if (animal == null)
            {
                return NotFound();
            }

            return Ok(animal);
        }

        [HttpPost("AddAnimal")]
        [Authorize(Roles = "ShelterAdmin")]
        public async Task<IActionResult> CreateAnimal([FromBody] CreateAnimalDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var shelter = await _unitOfWork.ShelterRepository.GetByIdAsync(dto.ShelterId);
            if (shelter == null) return NotFound("Shelter not found.");

            var animal = new Animal
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Species = dto.Species,
                Breed = dto.Breed,
                Age = dto.Age,
                Status = "Free",
                ShelterId = dto.ShelterId,
                Sex = dto.Sex,
                Size = dto.Size,
                Sterilized = dto.Sterilized,
                HealthCondition = dto.HealthCondition,
                Description = dto.Description,
                CreatedAtUtc = DateTime.UtcNow
            };

            // Додавання фотографій
            if (dto.PhotoUrls != null)
            {
                animal.Photos = dto.PhotoUrls.Select(url => new AnimalPhoto
                {
                    Id = Guid.NewGuid(),
                    PhotoURL = url,
                    AnimalId = animal.Id
                }).ToList();
            }

            await _unitOfWork.AnimalRepository.AddAsync(animal);
            await _unitOfWork.SaveAsync();

            return CreatedAtAction(nameof(GetAnimal), new { id = animal.Id }, animal);
        }

        [HttpPut("ChangeAnimalInfo")]
        [Authorize(Roles = "ShelterAdmin,SuperAdmin")]
        public async Task<IActionResult> UpdateAnimal(Guid id, [FromBody] UpdateAnimalDto dto)
        {
            var animal = await _unitOfWork.AnimalRepository.GetByIdAsync(id, includeProperties: "Photos");
            if (animal == null) return NotFound("Animal not found.");

            // Оновлення полів
            animal.Name = dto.Name ?? animal.Name;
            animal.Species = dto.Species ?? animal.Species;
            animal.Breed = dto.Breed ?? animal.Breed;
            animal.Age = dto.Age ?? animal.Age;
            animal.Sex = dto.Sex ?? animal.Sex;
            animal.Size = dto.Size ?? animal.Size;
            animal.Sterilized = dto.Sterilized ?? animal.Sterilized;
            animal.HealthCondition = dto.HealthCondition ?? animal.HealthCondition;
            animal.Description = dto.Description ?? animal.Description;

            try
            {
                await _unitOfWork.SaveAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict("Error occurred while updating the animal.");
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "ShelterAdmin,SuperAdmin")]
        public async Task<IActionResult> DeleteAnimal(Guid id)
        {
            // Отримання тварини з репозиторію (включаючи залежні сутності, якщо потрібно)
            var animal = await _unitOfWork.AnimalRepository.GetByIdAsync(id);
            if (animal == null)
            {
                return NotFound("Animal not found.");
            }

            // Видалення пов'язаних сутностей
            var animalPhotos = await _unitOfWork.AnimalPhotoRepository.GetAllAsync(ap => ap.AnimalId == id);
            _unitOfWork.AnimalPhotoRepository.RemoveRange(animalPhotos);

            var adoptionRequests = await _unitOfWork.AdoptionRequestRepository.GetAllAsync(ar => ar.AnimalId == id);
            _unitOfWork.AdoptionRequestRepository.RemoveRange(adoptionRequests);

            var usersAnimals = await _unitOfWork.UsersAnimalRepository.GetAllAsync(ua => ua.AnimalId == id);
            _unitOfWork.UsersAnimalRepository.RemoveRange(usersAnimals);

            // Видалення самої тварини
            _unitOfWork.AnimalRepository.Remove(animal);

            // Збереження змін
            await _unitOfWork.SaveAsync();

            return NoContent();
        }
    }
}
