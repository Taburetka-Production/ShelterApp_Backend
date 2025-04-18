using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Data;
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
            var shelters = await _unitOfWork.ShelterRepository.GetAllAsync(includeProperties: "Animals,Address");

            var shelterDtos = shelters.Select(s => new ShelterSummaryDto
            {
                Id = s.Id,
                Name = s.Name,
                Rating = s.Rating,
                AnimalsCount = s.AnimalsCount,
                ImageUrl = s.ImageUrl,
                Slug = s.Slug,
                City = s.Address?.City,
                Region = s.Address?.Region,
                Description = s.Description
            });

            return Ok(shelterDtos);
        }

        [HttpGet("{slug}")]
        public async Task<ActionResult<ShelterDetailDto>> GetShelterBySlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return BadRequest("Slug cannot be empty.");
            }

            string includeProps = "Address,Animals.Photos,ShelterFeedbacks,ShelterFeedbacks.User";

            var shelterEntity = await _unitOfWork.ShelterRepository.GetFirstOrDefaultAsync(
                filter: s => s.Slug == slug.ToLowerInvariant(),
                includeProperties: includeProps,
                tracked: false
            );

            if (shelterEntity == null)
            {
                return NotFound($"Shelter with slug '{slug}' not found.");
            }

            var shelterDto = new ShelterDetailDto
            {
                Id = shelterEntity.Id,
                Name = shelterEntity.Name,
                Rating = shelterEntity.Rating,
                ReviewsCount = shelterEntity.ReviewsCount,
                AnimalsCount = shelterEntity.AnimalsCount,
                Description = shelterEntity.Description,
                ImageUrl = shelterEntity.ImageUrl,
                Slug = shelterEntity.Slug,
                Address = shelterEntity.Address == null ? null : new AddressDto
                {
                    Country = shelterEntity.Address.Country,
                    Region = shelterEntity.Address.Region,
                    District = shelterEntity.Address.District,
                    City = shelterEntity.Address.City,
                    Street = shelterEntity.Address.Street,
                    Apartments = shelterEntity.Address.Apartments,
                    lng = shelterEntity.Address.lng,
                    lat = shelterEntity.Address.lat
                },
                Animals = shelterEntity.Animals?.Select(a => new AnimalSummaryDto
                {
                    Name = a.Name,
                    Species = a.Species,
                    Status = a.Status,
                    Slug = a.Slug,
                    PrimaryPhotoUrl = a.Photos?.FirstOrDefault()?.PhotoURL
                }).ToList() ?? new List<AnimalSummaryDto>(),
                Feedbacks = shelterEntity.ShelterFeedbacks?.Select(f => new ShelterFeedbackDto
                {
                    Comment = f.Comment,
                    Rating = f.Rating,
                    CreatedAtUtc = f.CreatedAtUtc,
                    User = f.User == null ? null : new UserSummaryDto
                    {
                        Name = f.User.Name,
                        Surname = f.User.Surname,
                        AvatarUrl = f.User.AvatarUrl
                    }
                }).ToList() ?? new List<ShelterFeedbackDto>()
            };

            return Ok(shelterDto);
        }

        [HttpPost("{slug}/toggle-save")]
        [Authorize]
        public async Task<IActionResult> ToggleSaveShelterBySlug(string slug)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var shelter = await _unitOfWork.ShelterRepository.GetFirstOrDefaultAsync(s => s.Slug == slug.ToLowerInvariant());
            if (shelter == null)
            {
                return NotFound("Shelter not found.");
            }

            var existingUserShelter = await _unitOfWork.UsersShelterRepository.GetFirstOrDefaultAsync(
                filter: us => us.ShelterId == shelter.Id && us.UserId == userId);

            bool isNowSaved;

            if (existingUserShelter != null)
            {
                _unitOfWork.UsersShelterRepository.Remove(existingUserShelter);
                isNowSaved = false;
            }
            else
            {
                var newUserShelter = new UsersShelter
                {
                    Id = Guid.NewGuid(),
                    ShelterId = shelter.Id,
                    UserId = userId,
                    CreatedAtUtc = DateTime.UtcNow
                };
                await _unitOfWork.UsersShelterRepository.AddAsync(newUserShelter);
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

        [HttpPost("{slug}/feedback")]
        [Authorize]
        public async Task<IActionResult> AddShelterFeedbackBySlug(string slug, [FromBody] CreateShelterFeedbackDto feedbackDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var shelter = await _unitOfWork.ShelterRepository.GetFirstOrDefaultAsync(s => s.Slug == slug.ToLowerInvariant());
            if (shelter == null)
            {
                return NotFound("Shelter not found.");
            }

            var existingFeedback = await _unitOfWork.ShelterFeedbackRepository.ExistsAsync(
                sf => sf.ShelterId == shelter.Id && sf.UserId == userId
            );
            if (existingFeedback)
            {
                return Conflict("User has already submitted feedback for this shelter.");
            }

            var newFeedback = new ShelterFeedback
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ShelterId = shelter.Id,
                Comment = feedbackDto.Comment,
                Rating = feedbackDto.Rating,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _unitOfWork.ShelterFeedbackRepository.AddAsync(newFeedback);

            double currentTotalRating = shelter.Rating * shelter.ReviewsCount;
            int newReviewsCount = shelter.ReviewsCount + 1;
            double newAverageRating = (currentTotalRating + newFeedback.Rating) / newReviewsCount;

            shelter.Rating = newAverageRating;
            shelter.ReviewsCount = newReviewsCount;
            shelter.UpdatedAtUtc = DateTime.UtcNow;
            shelter.UserLastModified = Guid.Parse(userId);

            try
            {
                await _unitOfWork.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while saving the feedback.");
            }


            var createdFeedback = await _unitOfWork.ShelterFeedbackRepository.GetFirstOrDefaultAsync(
                filter: sf => sf.Id == newFeedback.Id,
                includeProperties: "User"
            );

            var responseDto = new ShelterFeedbackDto
            {
                Comment = createdFeedback.Comment,
                Rating = createdFeedback.Rating,
                CreatedAtUtc = createdFeedback.CreatedAtUtc,
                User = createdFeedback.User == null ? null : new UserSummaryDto
                {
                    Name = createdFeedback.User.Name,
                    Surname = createdFeedback.User.Surname,
                    AvatarUrl = createdFeedback.User.AvatarUrl
                }
            };

            return CreatedAtAction(nameof(GetShelterBySlug), new { slug = shelter.Slug }, responseDto);
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
