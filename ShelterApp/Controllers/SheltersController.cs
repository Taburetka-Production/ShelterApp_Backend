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

        [HttpPost("feedback-temporary-bulk")] // Renamed route slightly for clarity
        [AllowAnonymous]
        // Change parameter to accept a List/Array
        public async Task<IActionResult> AddShelterFeedbackTemporaryBulk([FromBody] List<TemporaryCreateFeedbackInputModel> dtos)
        {
            // Check if the input list itself is null or empty
            if (dtos == null || !dtos.Any())
            {
                return BadRequest("Input list cannot be null or empty.");
            }

            // Basic validation for each item in the list (DataAnnotations)
            if (!ModelState.IsValid)
            {
                // Note: ModelState validation on list items can be tricky.
                // You might need more robust manual validation if complex rules apply per item.
                return BadRequest(ModelState);
            }

            if (_unitOfWork.ShelterRepository == null || _unitOfWork.ShelterFeedbackRepository == null)
            {
                return Problem("Required repository services are not available.", statusCode: StatusCodes.Status500InternalServerError);
            }

            // To store created feedbacks if needed for response
            var createdFeedbacks = new List<ShelterFeedback>();
            // To keep track of shelters needing updates (avoids fetching multiple times if optimized)
            var sheltersToUpdate = new Dictionary<Guid, Shelter>();

            try
            {
                // Loop through each feedback item in the request body
                foreach (var dto in dtos)
                {
                    // --- Fetch Shelter (Tracked) ---
                    // Optimization: Check if we already fetched this shelter in this batch
                    if (!sheltersToUpdate.TryGetValue(dto.ShelterId, out var shelter))
                    {
                        shelter = await _unitOfWork.ShelterRepository.GetFirstOrDefaultAsync(
                           s => s.Id == dto.ShelterId,
                           tracked: true // Track entity for updates
                       );

                        if (shelter == null)
                        {
                            // Option 1: Stop processing and return error for the batch
                            // return NotFound($"Shelter with ID {dto.ShelterId} not found. Batch processing stopped.");

                            // Option 2: Log the error and skip this item (more resilient for seeding)
                            Console.WriteLine($"Warning: Shelter with ID {dto.ShelterId} not found. Skipping feedback item.");
                            // Log.Warning($"Shelter with ID {dto.ShelterId} not found. Skipping feedback item for User {dto.UserId}."); // Use a proper logger
                            continue; // Skip to the next dto in the list
                        }
                        // Add fetched shelter to our dictionary for potential reuse
                        sheltersToUpdate.Add(shelter.Id, shelter);
                    }


                    // --- Create Feedback Object ---
                    var newFeedback = new ShelterFeedback
                    {
                        Id = Guid.NewGuid(),
                        UserId = dto.UserId,
                        ShelterId = dto.ShelterId,
                        Comment = dto.Comment,
                        Rating = dto.Rating,
                        CreatedAtUtc = DateTime.UtcNow
                    };

                    // Add feedback to repository context (will be saved later)
                    await _unitOfWork.ShelterFeedbackRepository.AddAsync(newFeedback);
                    createdFeedbacks.Add(newFeedback); // Optional: Collect created objects

                    // --- Update Shelter Rating (using the tracked shelter object) ---
                    double currentTotalRating = shelter.Rating * shelter.ReviewsCount;
                    int newReviewsCount = shelter.ReviewsCount + 1;
                    double newAverageRating = (newReviewsCount > 0) ? ((currentTotalRating + newFeedback.Rating) / newReviewsCount) : newFeedback.Rating;

                    shelter.Rating = newAverageRating;
                    shelter.ReviewsCount = newReviewsCount;
                    shelter.UpdatedAtUtc = DateTime.UtcNow;
                    // No need to call _unitOfWork.ShelterRepository.Update(shelter); as it's tracked
                } // End foreach loop

                // --- Save All Changes ---
                // Only save if there were valid items processed
                if (createdFeedbacks.Any())
                {
                    await _unitOfWork.SaveAsync();
                }

                // Return success - maybe indicate how many were processed
                return Ok(new { Message = $"Successfully processed {createdFeedbacks.Count} out of {dtos.Count} feedback items.", ProcessedItems = createdFeedbacks.Count });
                // Or return the list of created objects: return Ok(createdFeedbacks);
            }
            catch (DbUpdateException dbEx)
            {
                // Log the detailed error
                Console.WriteLine($"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}");
                // Log.Error(dbEx, "Database error occurred during bulk feedback processing."); // Use a proper logger
                return StatusCode(StatusCodes.Status500InternalServerError, $"Database error occurred: {dbEx.InnerException?.Message ?? dbEx.Message}");
            }
            catch (Exception ex)
            {
                // Log the detailed error
                Console.WriteLine($"Unexpected error: {ex.Message}");
                // Log.Error(ex, "An unexpected error occurred during bulk feedback processing."); // Use a proper logger
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
                    Id = f.Id,
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

        [HttpDelete("feedback/{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteShelterFeedback(Guid id)
        {
            if (_unitOfWork.ShelterRepository == null || _unitOfWork.ShelterFeedbackRepository == null)
            {
                return Problem("Required repository services are not available.", statusCode: StatusCodes.Status500InternalServerError);
            }

            var feedbackToDelete = await _unitOfWork.ShelterFeedbackRepository.GetFirstOrDefaultAsync(
                filter: sf => sf.Id == id,
                includeProperties: "Shelter",
                tracked: true
            );

            if (feedbackToDelete == null)
            {
                return NotFound($"ShelterFeedback with ID {id} not found.");
            }

            var shelter = feedbackToDelete.Shelter;
            if (shelter == null)
            {
                Console.WriteLine($"Critical Error: Shelter associated with Feedback ID {id} could not be loaded. Data might be inconsistent.");
                return Problem($"Could not load Shelter associated with Feedback ID {id}. Cannot proceed.", statusCode: StatusCodes.Status500InternalServerError);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid? userGuid = null;
            if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out Guid parsedGuid))
            {
                userGuid = parsedGuid;
            }

            try
            {
                double ratingToRemove = feedbackToDelete.Rating;
                double currentTotalRating = shelter.Rating * shelter.ReviewsCount;

                int newReviewsCount = Math.Max(0, shelter.ReviewsCount - 1);
                double newAverageRating = 0.0;

                if (newReviewsCount > 0)
                {
                    double newTotalRating = Math.Max(0, currentTotalRating - ratingToRemove);
                    newAverageRating = newTotalRating / newReviewsCount;
                }

                shelter.Rating = newAverageRating;
                shelter.ReviewsCount = newReviewsCount;
                shelter.UpdatedAtUtc = DateTime.UtcNow;
                shelter.UserLastModified = userGuid;

                _unitOfWork.ShelterFeedbackRepository.Remove(feedbackToDelete);

                await _unitOfWork.SaveAsync();

                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, $"Database error occurred while deleting feedback: {dbEx.InnerException?.Message ?? dbEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An unexpected error occurred while deleting feedback: {ex.Message}");
            }
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
