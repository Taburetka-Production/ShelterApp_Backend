using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Data;
using System.Security.Claims;

namespace ShelterApp
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnimalsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public AnimalsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpPost("create-temporary-bulk")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateAnimalsTemporaryBulk([FromBody] List<CreateAnimalDto> dtos)
        {
            if (_unitOfWork.AnimalRepository == null || _unitOfWork.ShelterRepository == null)
            {
                return Problem("Required repository services are not available.", statusCode: StatusCodes.Status500InternalServerError);
            }

            if (dtos == null || !dtos.Any())
            {
                return BadRequest("Animal data array cannot be null or empty.");
            }

            // Pre-fetch necessary data for efficiency
            var shelterIds = dtos.Select(d => d.ShelterId).Distinct().ToList();
            var sheltersDict = (await _unitOfWork.ShelterRepository.GetAllAsync(s => shelterIds.Contains(s.Id)))
                                     .ToDictionary(s => s.Id);

            var allAnimalSlugs = (await _unitOfWork.AnimalRepository.GetAllAsync(a => !string.IsNullOrEmpty(a.Slug)))
                                     .Select(a => a.Slug)
                                     .ToHashSet();

            int successfullyAdded = 0;
            List<string> errors = new List<string>();

            foreach (var dto in dtos)
            {
                // Basic Validation per DTO
                if (!sheltersDict.TryGetValue(dto.ShelterId, out var shelter))
                {
                    errors.Add($"Shelter with ID {dto.ShelterId} not found for animal '{dto.Name ?? "N/A"}'. Skipped.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    errors.Add($"Animal name is missing for an entry with Shelter ID {dto.ShelterId}. Skipped.");
                    continue;
                }

                // Slug Generation (with collision check within batch and DB)
                string baseSlug = UrlSlugger.GenerateSlug(dto.Name);
                string finalSlug = baseSlug;
                int counter = 1;
                while (allAnimalSlugs.Contains(finalSlug))
                {
                    finalSlug = $"{baseSlug}-{counter}";
                    counter++;
                }
                allAnimalSlugs.Add(finalSlug); // Add to set for checks within this batch

                var animal = new Animal
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    Slug = finalSlug,
                    Species = dto.Species,
                    Breed = dto.Breed,
                    Age = dto.Age,
                    Status = "Free", // Default status
                    ShelterId = dto.ShelterId,
                    Sex = dto.Sex,
                    Size = dto.Size,
                    Sterilized = dto.Sterilized,
                    HealthCondition = dto.HealthCondition,
                    Description = dto.Description,
                    CreatedAtUtc = DateTime.UtcNow
                };

                if (dto.PhotoUrls != null && dto.PhotoUrls.Any())
                {
                    animal.Photos = dto.PhotoUrls.Where(url => !string.IsNullOrWhiteSpace(url)).Select(url => new AnimalPhoto
                    {
                        Id = Guid.NewGuid(),
                        PhotoURL = url.Trim(),
                        AnimalId = animal.Id,
                        CreatedAtUtc = DateTime.UtcNow
                    }).ToList();
                }
                else
                {
                    animal.Photos = new List<AnimalPhoto>();
                }

                await _unitOfWork.AnimalRepository.AddAsync(animal);

                // Increment shelter count (EF Core tracks changes on the fetched shelter entity)
                shelter.AnimalsCount++;
                shelter.UpdatedAtUtc = DateTime.UtcNow;

                successfullyAdded++;
            }

            if (successfullyAdded > 0)
            {
                try
                {
                    await _unitOfWork.SaveAsync();
                }
                catch (DbUpdateException ex)
                {
                    // Log ex
                    errors.Add($"A database error occurred during save: {ex.InnerException?.Message ?? ex.Message}. Some animals/counts might not be saved.");
                    // Consider more robust error handling/rollback if needed
                    return StatusCode(500, new { Message = "An error occurred while saving bulk animal data.", Errors = errors });
                }
                catch (Exception ex)
                {
                    // Log ex
                    errors.Add($"An unexpected error occurred during save: {ex.Message}. Some animals/counts might not be saved.");
                    return StatusCode(500, new { Message = "An unexpected error occurred while saving bulk animal data.", Errors = errors });
                }
            }

            if (errors.Any())
            {
                return Ok(new { Message = $"Processed {dtos.Count} records. Added: {successfullyAdded}. See errors for details.", Errors = errors });
            }

            return Ok(new { Message = $"Successfully added {successfullyAdded} animals." });
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Animal>>> GetAnimals()
        {
            if (_unitOfWork.AnimalRepository == null)
            {
                return NotFound();
            }

            var animals = await _unitOfWork.AnimalRepository.GetAllAsync(includeProperties: "Shelter,Shelter.Address");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var animalsResponse = animals.Select(a => new
            {
                id = a.Id,
                createdAtUtc = a.CreatedAtUtc,
                name = a.Name,
                slug = a.Slug,
                species = a.Species,
                breed = a.Breed,
                age = a.Age,
                sex = a.Sex,
                size = a.Size,
                sterilized = a.Sterilized,
                healthCondition = a.HealthCondition,
                description = a.Description,
                photoUrl = a.Photos.Count >= 1 ? a.Photos.First() : null,
                inFavourites = userId != null ? a.UsersAnimal.Select(ua => ua.UserId).ToList().Contains(userId) : false
            });

            return Ok(animalsResponse);
        }

        [HttpGet("{slug}")]
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

            var animalResponse = new
            {
                id = animal.Id,
                createdAtUtc = animal.CreatedAtUtc,
                updatedAtUtc = animal.UpdatedAtUtc,
                userLastModified = animal.UserLastModified,
                name = animal.Name,
                species = animal.Species,
                breed = animal.Breed,
                age = animal.Age,
                status = animal.Status,
                sex = animal.Sex,
                size = animal.Size,
                sterilized = animal.Sterilized,
                healthCondition = animal.HealthCondition,
                description = animal.Description,
                shelter = new
                {
                    name = animal.Shelter.Name,
                    slug = animal.Shelter.Slug
                },
                photos = animal.Photos.Select(p => new { photoUrl = p.PhotoURL }).ToList(),
                address = new
                {
                    lng = animal.Shelter.Address.lng,
                    lat = animal.Shelter.Address.lat
                }
            };

            return Ok(animalResponse);
        }

        [HttpPost("{slug}/toggle-save")]
        [Authorize]
        public async Task<IActionResult> ToggleSaveAnimalBySlug(string slug)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var animal = await _unitOfWork.AnimalRepository.GetFirstOrDefaultAsync(a => a.Slug == slug.ToLowerInvariant());
            if (animal == null)
            {
                return NotFound("Animal not found.");
            }

            var existingUserAnimal = await _unitOfWork.UsersAnimalRepository.GetFirstOrDefaultAsync(
                filter: ua => ua.AnimalId == animal.Id && ua.UserId == userId);

            bool isNowSaved;

            if (existingUserAnimal != null)
            {
                _unitOfWork.UsersAnimalRepository.Remove(existingUserAnimal);
                isNowSaved = false;
            }
            else
            {
                var newUserAnimal = new UsersAnimal
                {
                    Id = Guid.NewGuid(),
                    AnimalId = animal.Id,
                    UserId = userId,
                    CreatedAtUtc = DateTime.UtcNow
                };
                await _unitOfWork.UsersAnimalRepository.AddAsync(newUserAnimal);
                isNowSaved = true;
            }

            try
            {
                await _unitOfWork.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, "An error occurred while updating the save status.");
            }

            return Ok(new SaveToggleResultDto { IsSaved = isNowSaved });
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
                Slug = finalSlug,
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

            if (dto.PhotoUrls != null && dto.PhotoUrls.Any())
            {
                animal.Photos = dto.PhotoUrls.Select(url => new AnimalPhoto
                {
                    Id = Guid.NewGuid(),
                    PhotoURL = url,
                    AnimalId = animal.Id
                }).ToList();
            }
            else
            {
                animal.Photos = new List<AnimalPhoto>();
            }


            await _unitOfWork.AnimalRepository.AddAsync(animal);

            shelter.AnimalsCount++;
            shelter.UpdatedAtUtc = DateTime.UtcNow;
            _unitOfWork.ShelterRepository.Update(shelter);

            await _unitOfWork.SaveAsync();

            return CreatedAtAction(nameof(GetAnimalBySlug), new { slug = animal.Slug }, animal);
        }


        [HttpPut("ChangeAnimalInfo")]
        [Authorize(Roles = "ShelterAdmin,SuperAdmin")]
        public async Task<IActionResult> UpdateAnimal(Guid id, [FromBody] UpdateAnimalDto dto)
        {
            var animal = await _unitOfWork.AnimalRepository.GetByIdAsync(id, includeProperties: "Photos");
            if (animal == null) return NotFound("Animal not found.");

            if (dto.Name != null)
            {
                bool nameChanged = !animal.Name.Equals(dto.Name.Trim(), StringComparison.OrdinalIgnoreCase);

                if (nameChanged)
                {
                    animal.Name = dto.Name.Trim();

                    string baseSlug = UrlSlugger.GenerateSlug(animal.Name);
                    string finalSlug = baseSlug;
                    int counter = 1;

                    while (await _unitOfWork.AnimalRepository.ExistsAsync(s => s.Slug == finalSlug && s.Id != id))
                    {
                        finalSlug = $"{baseSlug}-{counter}";
                        counter++;
                    }
                    animal.Slug = finalSlug;
                }
            }

            animal.Name = dto.Name ?? animal.Name;
            animal.Species = dto.Species ?? animal.Species;
            animal.Breed = dto.Breed ?? animal.Breed;
            animal.Age = dto.Age ?? animal.Age;
            animal.Sex = dto.Sex ?? animal.Sex;
            animal.Size = dto.Size ?? animal.Size;
            animal.Sterilized = dto.Sterilized ?? animal.Sterilized;
            animal.HealthCondition = dto.HealthCondition ?? animal.HealthCondition;
            animal.Description = dto.Description ?? animal.Description;

            if (dto.NewPhotoUrls != null && dto.NewPhotoUrls.Any())
            {
                animal.Photos ??= new List<AnimalPhoto>();

                foreach (var newUrl in dto.NewPhotoUrls)
                {
                    if (!string.IsNullOrWhiteSpace(newUrl))
                    {
                        var newPhoto = new AnimalPhoto
                        {
                            Id = Guid.NewGuid(),
                            PhotoURL = newUrl.Trim(),
                            AnimalId = animal.Id,
                            CreatedAtUtc = DateTime.UtcNow
                        };
                        animal.Photos.Add(newPhoto);
                    }
                }
            }

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
                return StatusCode(500, $"An error occurred during animal slug population: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "ShelterAdmin,SuperAdmin")]
        public async Task<IActionResult> DeleteAnimal(Guid id)
        {
            var animal = await _unitOfWork.AnimalRepository.GetFirstOrDefaultAsync(filter: a => a.Id == id, includeProperties: "Shelter");
            if (animal == null)
            {
                return NotFound("Animal not found.");
            }

            var shelter = animal.Shelter;

            var animalPhotos = await _unitOfWork.AnimalPhotoRepository.GetAllAsync(ap => ap.AnimalId == id);
            _unitOfWork.AnimalPhotoRepository.RemoveRange(animalPhotos);

            var adoptionRequests = await _unitOfWork.AdoptionRequestRepository.GetAllAsync(ar => ar.AnimalId == id);
            _unitOfWork.AdoptionRequestRepository.RemoveRange(adoptionRequests);

            var usersAnimals = await _unitOfWork.UsersAnimalRepository.GetAllAsync(ua => ua.AnimalId == id);
            _unitOfWork.UsersAnimalRepository.RemoveRange(usersAnimals);

            _unitOfWork.AnimalRepository.Remove(animal);

            if (shelter != null)
            {
                if (shelter.AnimalsCount > 0)
                {
                    shelter.AnimalsCount--;
                }
                else
                {
                    shelter.AnimalsCount = 0;
                }
                shelter.UpdatedAtUtc = DateTime.UtcNow;
                _unitOfWork.ShelterRepository.Update(shelter);
            }

            await _unitOfWork.SaveAsync();

            return NoContent();
        }
    }
}
