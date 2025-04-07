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

            string baseSlug = UrlSlugger.GenerateSlug(shelter.Name);
            string finalSlug = baseSlug;
            int counter = 1;

            while (await _unitOfWork.ShelterRepository.ExistsAsync(s => s.Slug == finalSlug))
            {
                finalSlug = $"{baseSlug}-{counter}";
                counter++;
            }
            shelter.Slug = finalSlug;

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

        [HttpPost("populate-slugs")] // Choose a distinct route
        public async Task<IActionResult> PopulateExistingSlugs()
        {
            if (_unitOfWork.ShelterRepository == null)
            {
                return Problem("Shelter repository is not available.", statusCode: 500);
            }

            int updatedCount = 0;
            var sheltersToUpdate = new List<Shelter>(); // To hold entities needing update

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
    }
}
