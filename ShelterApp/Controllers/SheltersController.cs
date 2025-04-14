using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Data;
using ShelterApp.DTOs;
using System.Security.Claims;

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
            var shelters = await _unitOfWork.ShelterRepository.GetAllAsync(includeProperties: "Animals");

            return Ok(shelters);
        }

        [HttpGet("byslug/{slug}")]
        public async Task<ActionResult<Shelter>> GetShelterBySlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return BadRequest("Slug cannot be empty.");
            }

            var shelter = await _unitOfWork.ShelterRepository.GetFirstOrDefaultAsync(
                filter: s => s.Slug == slug.ToLowerInvariant(),
                includeProperties: "Address",
                tracked: false
            );

            if (shelter == null)
            {
                return NotFound($"Shelter with slug '{slug}' not found.");
            }

            return Ok(shelter);
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
        [Authorize(Roles = "ShelterAdmin")]
        public async Task<ActionResult<Shelter>> CreateShelter([FromBody] CreateShelterDto dto)
        {
            if (_unitOfWork.ShelterRepository == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Shelters' is null.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Користувач не автентифікований.");
            }

            var address = new Address
            {
                Country = dto.Country,
                Region = dto.Region,
                District = dto.District,
                City = dto.City,
                Street = dto.Street,
                Apartments = dto.Apartments,
                lng = dto.lng,
                lat = dto.lat
            };

            await _unitOfWork.AddressRepository.AddAsync(address);
            await _unitOfWork.SaveAsync();

            string baseSlug = UrlSlugger.GenerateSlug(dto.Name);
            string finalSlug = baseSlug;
            int counter = 1;

            while (await _unitOfWork.ShelterRepository.ExistsAsync(s => s.Slug == finalSlug))
            {
                finalSlug = $"{baseSlug}-{counter}";
                counter++;
            }

            var shelter = new Shelter
            {
                Name = dto.Name,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                Rating = 0.0,
                ReviewsCount = 0,
                AnimalsCount = 0,
                AddressId = address.Id,
                UserId = userId,
                Slug = finalSlug
            };

            try
            {
                await _unitOfWork.ShelterRepository.AddAsync(shelter);
                await _unitOfWork.SaveAsync();

                var createdShelter = await _unitOfWork.ShelterRepository.GetByIdAsync(shelter.Id, includeProperties: "Address");
                return CreatedAtAction(nameof(GetShelterBySlug), new { slug = createdShelter.Slug }, createdShelter);
            }
            catch (Exception ex)
            {
                _unitOfWork.AddressRepository.Remove(address);
                await _unitOfWork.SaveAsync();
                return StatusCode(500, $"Помилка: {ex.Message}");
            }
        }

        [HttpPost("populate-slugs")]
        public async Task<IActionResult> PopulateExistingSlugs()
        {
            if (_unitOfWork.ShelterRepository == null)
            {
                return Problem("Shelter repository is not available.", statusCode: 500);
            }

            int updatedCount = 0;
            var sheltersToUpdate = new List<Shelter>();

            try
            {
                var allSlugsQuery = _unitOfWork.ShelterRepository.GetAllAsync(
                     filter: s => !string.IsNullOrEmpty(s.Slug)
                );
                var existingSlugsSet = (await allSlugsQuery)
                                         .Select(s => s.Slug)
                                         .ToHashSet();

                sheltersToUpdate = (await _unitOfWork.ShelterRepository.GetAllAsync(
                     filter: s => string.IsNullOrEmpty(s.Slug)
                )).ToList();

                if (!sheltersToUpdate.Any())
                {
                    return Ok("No shelters found needing slug population.");
                }

                foreach (var shelter in sheltersToUpdate)
                {
                    if (string.IsNullOrWhiteSpace(shelter.Name))
                    {
                        continue;
                    }

                    string baseSlug = UrlSlugger.GenerateSlug(shelter.Name);
                    string finalSlug = baseSlug;
                    int counter = 1;

                    while (existingSlugsSet.Contains(finalSlug))
                    {
                        finalSlug = $"{baseSlug}-{counter}";
                        counter++;
                    }

                    if (finalSlug != null)
                    {
                        shelter.Slug = finalSlug;
                        shelter.UpdatedAtUtc = DateTime.UtcNow;
                        existingSlugsSet.Add(finalSlug);
                        updatedCount++;
                    }
                }

                await _unitOfWork.SaveAsync();

                return Ok($"Successfully populated slugs for {updatedCount} shelters out of {sheltersToUpdate.Count} processed.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred during slug population: {ex.Message}");
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
            var shelter = await _unitOfWork.ShelterRepository.GetByIdAsync(id, includeProperties: "Address");
            if (shelter == null)
            {
                return NotFound("Shelter not found.");
            }
            if (shelter.Address != null)
            {
                _unitOfWork.AddressRepository.Remove(shelter.Address);
            }
            var usersShelters = await _unitOfWork.UsersShelterRepository.GetAllAsync(us => us.ShelterId == id);
            _unitOfWork.UsersShelterRepository.RemoveRange(usersShelters);

            var shelterFeedbacks = await _unitOfWork.ShelterFeedbackRepository.GetAllAsync(sf => sf.ShelterId == id);
            _unitOfWork.ShelterFeedbackRepository.RemoveRange(shelterFeedbacks);

            var animals = await _unitOfWork.AnimalRepository.GetAllAsync(a => a.ShelterId == id);
            foreach (var animal in animals)
            {
                var animalPhotos = await _unitOfWork.AnimalPhotoRepository.GetAllAsync(ap => ap.AnimalId == animal.Id);
                _unitOfWork.AnimalPhotoRepository.RemoveRange(animalPhotos);

                var usersAnimals = await _unitOfWork.UsersAnimalRepository.GetAllAsync(ua => ua.AnimalId == animal.Id);
                _unitOfWork.UsersAnimalRepository.RemoveRange(usersAnimals);

                var adoptionRequests = await _unitOfWork.AdoptionRequestRepository.GetAllAsync(ar => ar.AnimalId == animal.Id);
                _unitOfWork.AdoptionRequestRepository.RemoveRange(adoptionRequests);
            }
            _unitOfWork.AnimalRepository.RemoveRange(animals);

            _unitOfWork.ShelterRepository.Remove(shelter);

            await _unitOfWork.SaveAsync();

            return NoContent();
        }
    }
}
