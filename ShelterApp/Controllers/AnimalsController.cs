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
        private readonly IUnitOfWork _unitOfWork;

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

            return Ok(animals);
        }

        [HttpGet("byslug/{slug}")]
        public async Task<ActionResult<Animal>> GetAnimalBySlug(string slug)
        {
            if (_unitOfWork.AnimalRepository == null)
            {
                return Problem("Animal repository is not available.", statusCode: 500);
            }

            if (string.IsNullOrWhiteSpace(slug))
            {
                return BadRequest("Slug cannot be empty.");
            }

            var animal = await _unitOfWork.AnimalRepository.GetFirstOrDefaultAsync(
                filter: a => a.Slug == slug.ToLowerInvariant(),
                includeProperties: "Shelter,Shelter.Address,Photos",
                tracked: false
            );

            if (animal == null)
            {
                return NotFound($"Animal with slug '{slug}' not found.");
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

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest("Animal name is required to generate a slug.");
            }

            string baseSlug = UrlSlugger.GenerateSlug(dto.Name);
            string finalSlug = baseSlug;
            int counter = 1;

            while (await _unitOfWork.AnimalRepository.ExistsAsync(a => a.Slug == finalSlug))
            {
                finalSlug = $"{baseSlug}-{counter}";
                counter++;
            }

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

            return CreatedAtAction(nameof(GetAnimalBySlug), new { slug = animal.Slug }, animal);
        }

        [HttpPut("ChangeAnimalInfo")]
        [Authorize(Roles = "ShelterAdmin,SuperAdmin")]
        public async Task<IActionResult> UpdateAnimal(Guid id, [FromBody] UpdateAnimalDto dto)
        {
            var animal = await _unitOfWork.AnimalRepository.GetByIdAsync(id, includeProperties: "Photos");
            if (animal == null) return NotFound("Animal not found.");

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

        [HttpPost("populate-slugs")]
        [AllowAnonymous]
        public async Task<IActionResult> PopulateExistingAnimalSlugs()
        {
            if (_unitOfWork.AnimalRepository == null)
            {
                return Problem("Animal repository is not available.", statusCode: 500);
            }

            int updatedCount = 0;
            var animalsToUpdate = new List<Animal>();

            try
            {
                var allSlugsQuery = _unitOfWork.AnimalRepository.GetAllAsync(
                     filter: s => !string.IsNullOrEmpty(s.Slug)
                );
                var existingSlugsSet = (await allSlugsQuery)
                                         .Select(s => s.Slug)
                                         .ToHashSet();

                animalsToUpdate = (await _unitOfWork.AnimalRepository.GetAllAsync(
                     filter: s => string.IsNullOrEmpty(s.Slug)
                )).ToList();

                if (!animalsToUpdate.Any())
                {
                    return Ok("No animals found needing slug population.");
                }

                foreach (var animal in animalsToUpdate)
                {
                    if (string.IsNullOrWhiteSpace(animal.Name))
                    {
                        continue;
                    }

                    string baseSlug = UrlSlugger.GenerateSlug(animal.Name);
                    string finalSlug = baseSlug;
                    int counter = 1;

                    while (existingSlugsSet.Contains(finalSlug))
                    {
                        finalSlug = $"{baseSlug}-{counter}";
                        counter++;
                    }

                    if (finalSlug != null)
                    {
                        animal.Slug = finalSlug;
                        animal.UpdatedAtUtc = DateTime.UtcNow;
                        existingSlugsSet.Add(finalSlug);
                        updatedCount++;
                    }
                }

                await _unitOfWork.SaveAsync();

                return Ok($"Successfully populated slugs for {updatedCount} animals out of {animalsToUpdate.Count} processed.");
            }
            catch (Exception ex)
            {
                // Log the exception ex
                return StatusCode(500, $"An error occurred during animal slug population: {ex.Message}");
            }
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
