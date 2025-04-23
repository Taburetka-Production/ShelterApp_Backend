using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace ShelterApp
{
    public class TemporaryCreateShelterInputModel
    {
        [Required]
        public string UserId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        [Url]
        public string ImageUrl { get; set; }
        [Required]
        public string Country { get; set; }
        [Required]
        public string Region { get; set; }
        public string? District { get; set; }
        [Required]
        public string City { get; set; }
        [Required]
        public string Street { get; set; }
        public string? Apartments { get; set; }
        [Required]
        public double lng { get; set; }
        [Required]
        public double lat { get; set; }
    }

    public class TemporaryCreateFeedbackInputModel
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public Guid ShelterId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public double Rating { get; set; }

        [Required]
        [MaxLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
        public string Comment { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class SheltersController : ControllerBase
    {
        private IUnitOfWork _unitOfWork;
        public SheltersController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpPost("create-temporary")]
        [AllowAnonymous]
        public async Task<ActionResult<Shelter>> CreateShelterTemporary([FromBody] TemporaryCreateShelterInputModel dto)
        {
            if (_unitOfWork.ShelterRepository == null || _unitOfWork.AddressRepository == null)
            {
                return Problem("Required repository services are not available.", statusCode: StatusCodes.Status500InternalServerError);
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
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
                Address = address,
                UserId = dto.UserId,
                Slug = finalSlug,
                CreatedAtUtc = DateTime.UtcNow
            };

            try
            {
                await _unitOfWork.ShelterRepository.AddAsync(shelter);
                await _unitOfWork.SaveAsync();

                var createdShelter = await _unitOfWork.ShelterRepository.GetFirstOrDefaultAsync(
                    filter: s => s.Id == shelter.Id,
                    includeProperties: "Address"
                );

                if (createdShelter == null)
                {
                    return Problem("Failed to retrieve created shelter.", statusCode: StatusCodes.Status500InternalServerError);
                }

                return CreatedAtAction(nameof(GetShelterBySlug), new { slug = createdShelter.Slug }, createdShelter);
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Database error occurred: {dbEx.InnerException?.Message ?? dbEx.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpPost("feedback-temporary")]
        [AllowAnonymous]
        public async Task<IActionResult> AddShelterFeedbackTemporary([FromBody] TemporaryCreateFeedbackInputModel dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (_unitOfWork.ShelterRepository == null || _unitOfWork.ShelterFeedbackRepository == null)
            {
                return Problem("Required repository services are not available.", statusCode: StatusCodes.Status500InternalServerError);
            }

            var shelter = await _unitOfWork.ShelterRepository.GetFirstOrDefaultAsync(
                s => s.Id == dto.ShelterId,
                tracked: true
            );

            if (shelter == null)
            {
                return NotFound($"Shelter with ID {dto.ShelterId} not found.");
            }

            var newFeedback = new ShelterFeedback
            {
                Id = Guid.NewGuid(),
                UserId = dto.UserId,
                ShelterId = dto.ShelterId,
                Comment = dto.Comment,
                Rating = dto.Rating,
                CreatedAtUtc = DateTime.UtcNow
            };

            try
            {
                await _unitOfWork.ShelterFeedbackRepository.AddAsync(newFeedback);

                double currentTotalRating = shelter.Rating * shelter.ReviewsCount;
                int newReviewsCount = shelter.ReviewsCount + 1;
                double newAverageRating = (newReviewsCount > 0) ? ((currentTotalRating + newFeedback.Rating) / newReviewsCount) : newFeedback.Rating;

                shelter.Rating = newAverageRating;
                shelter.ReviewsCount = newReviewsCount;
                shelter.UpdatedAtUtc = DateTime.UtcNow;

                await _unitOfWork.SaveAsync();

                return Ok(newFeedback);
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Database error occurred: {dbEx.InnerException?.Message ?? dbEx.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<ActionResult> GetShelters()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            HashSet<Guid> savedShelterIds = new HashSet<Guid>();

            if (!string.IsNullOrEmpty(userId))
            {
                var userShelters = await _unitOfWork.UsersShelterRepository.GetAllAsync(
                    filter: us => us.UserId == userId
                );
                savedShelterIds = new HashSet<Guid>(userShelters.Select(us => us.ShelterId));
            }

            var shelters = await _unitOfWork.ShelterRepository.GetAllAsync(includeProperties: "Animals,Address");

            var shelterDtos = shelters.Select(s => new ShelterSummaryDto
            {
                Id = s.Id,
                Name = s.Name,
                Rating = s.Rating,
                ReviewsCount = s.ReviewsCount,
                AnimalsCount = s.AnimalsCount,
                ImageUrl = s.ImageUrl,
                Slug = s.Slug,
                City = s.Address?.City,
                Region = s.Address?.Region,
                Description = s.Description,
                IsSaved = !string.IsNullOrEmpty(userId) && savedShelterIds.Contains(s.Id),
                Address = s.Address == null ? null : new AddressDto
                {
                    Country = s.Address.Country,
                    Region = s.Address.Region,
                    District = s.Address.District,
                    City = s.Address.City,
                    Street = s.Address.Street,
                    Apartments = s.Address.Apartments,
                    lng = s.Address.lng,
                    lat = s.Address.lat
                }
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

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool isUserLoggedIn = !string.IsNullOrEmpty(userId);

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

            bool isShelterSavedByUser = false;
            bool hasUserSubmittedFeedback = false;
            HashSet<Guid> savedAnimalIds = new HashSet<Guid>();

            if (isUserLoggedIn)
            {
                isShelterSavedByUser = await _unitOfWork.UsersShelterRepository.ExistsAsync(
                    us => us.ShelterId == shelterEntity.Id && us.UserId == userId
                );

                hasUserSubmittedFeedback = await _unitOfWork.ShelterFeedbackRepository.ExistsAsync(
                    sf => sf.ShelterId == shelterEntity.Id && sf.UserId == userId
                );

                if (shelterEntity.Animals != null && shelterEntity.Animals.Any())
                {
                    var animalIdsInThisShelter = shelterEntity.Animals.Select(a => a.Id).ToList();
                    if (animalIdsInThisShelter.Any())
                    {
                        var userAnimals = await _unitOfWork.UsersAnimalRepository.GetAllAsync(
                             filter: ua => ua.UserId == userId && animalIdsInThisShelter.Contains(ua.AnimalId)
                        );
                        savedAnimalIds = new HashSet<Guid>(userAnimals.Select(ua => ua.AnimalId));
                    }
                }
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
                IsSaved = isShelterSavedByUser,
                HasSubmittedFeedback = hasUserSubmittedFeedback,
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
                    Slug = a.Slug,
                    PrimaryPhotoUrl = a.Photos?.FirstOrDefault()?.PhotoURL,
                    Description = a.Description,
                    IsSaved = isUserLoggedIn && savedAnimalIds.Contains(a.Id)
                }).ToList() ?? new List<AnimalSummaryDto>(),
                Feedbacks = shelterEntity.ShelterFeedbacks?.Select(f => new ShelterFeedbackDto
                {
                    Comment = f.Comment,
                    Rating = f.Rating,
                    CreatedAtUtc = f.CreatedAtUtc,
                    User = f.User == null ? null : new UserSummaryDto
                    {
                        Username = f.User.UserName,
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
                    Username = createdFeedback.User.UserName,
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

        [HttpPut("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateShelter(Guid id, [FromBody] UpdateShelterDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Користувач не автентифікований.");
            }

            var shelter = await _unitOfWork.ShelterRepository.GetFirstOrDefaultAsync(
                s => s.Id == id,
                includeProperties: "Address",
                tracked: true
            );

            if (shelter == null)
            {
                return NotFound("Shelter not found.");
            }

            if (dto.Name != null)
            {
                bool nameChanged = !shelter.Name.Equals(dto.Name.Trim(), StringComparison.OrdinalIgnoreCase);

                if (nameChanged)
                {
                    shelter.Name = dto.Name.Trim();

                    string baseSlug = UrlSlugger.GenerateSlug(shelter.Name);
                    string finalSlug = baseSlug;
                    int counter = 1;

                    while (await _unitOfWork.ShelterRepository.ExistsAsync(s => s.Slug == finalSlug && s.Id != id))
                    {
                        finalSlug = $"{baseSlug}-{counter}";
                        counter++;
                    }
                    shelter.Slug = finalSlug;
                }
            }

            shelter.Description = dto.Description ?? shelter.Description;
            shelter.ImageUrl = dto.ImageUrl ?? shelter.ImageUrl;

            if (dto.Address != null)
            {
                shelter.Address ??= new Address();

                shelter.Address.Country = dto.Address.Country ?? shelter.Address.Country;
                shelter.Address.Region = dto.Address.Region ?? shelter.Address.Region;
                shelter.Address.District = dto.Address.District ?? shelter.Address.District;
                shelter.Address.City = dto.Address.City ?? shelter.Address.City;
                shelter.Address.Street = dto.Address.Street ?? shelter.Address.Street;
                shelter.Address.Apartments = dto.Address.Apartments ?? shelter.Address.Apartments;
                shelter.Address.lng = dto.Address.lng;
                shelter.Address.lat = dto.Address.lat;

                _unitOfWork.AddressRepository.Update(shelter.Address);
            }

            shelter.UpdatedAtUtc = DateTime.UtcNow;
            shelter.UserLastModified = Guid.Parse(userId);

            try
            {
                _unitOfWork.ShelterRepository.Update(shelter);
                await _unitOfWork.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, "Помилка при оновленні притулку");
            }

            return Ok(await _unitOfWork.ShelterRepository.GetByIdAsync(id, includeProperties: "Address"));
        }

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
