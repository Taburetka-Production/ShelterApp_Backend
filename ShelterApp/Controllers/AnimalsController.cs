using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ShelterApp
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnimalsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AnimalsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("GetAllAnimals")]
        public async Task<ActionResult<IEnumerable<Animal>>> GetAnimals()
        {
            if (_context.Animals == null)
            {
                return NotFound();
            }

            var animals = await _context.Animals
                .Include(a => a.Shelter)
                .ThenInclude(s => s.Address)
                .ToListAsync();

            return Ok(animals);
        }

        [HttpGet("GetAnimalById")]
        public async Task<ActionResult<Animal>> GetAnimal(Guid id)
        {
            if (_context.Animals == null)
            {
                return NotFound();
            }

            var animal = await _context.Animals
                .Include(a => a.Shelter)
                .ThenInclude(s => s.Address)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (animal == null)
            {
                return NotFound();
            }

            return Ok(animal);
        }

        [HttpPut("ChangeAnimalInfo")]
        [Authorize(Roles = "ShelterAdmin,SuperAdmin")]
        public async Task<IActionResult> UpdateAnimal(Guid id, [FromBody] UpdateAnimalDto updatedAnimalDto)
        {
            var animal = await _context.Animals.FindAsync(id);

            if (animal == null)
            {
                return NotFound("Animal not found.");
            }

            animal.Name = updatedAnimalDto.Name ?? animal.Name;
            animal.Species = updatedAnimalDto.Species ?? animal.Species;
            animal.Breed = updatedAnimalDto.Breed ?? animal.Breed;
            animal.Age = updatedAnimalDto.Age ?? animal.Age;
            animal.Status = updatedAnimalDto.Status ?? animal.Status;
            animal.PhotoURL = updatedAnimalDto.PhotoURL ?? animal.PhotoURL;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict("Error occurred while updating the animal.");
            }

            return NoContent();
        }

        [HttpPost("AddAnimal")]
        [Authorize(Roles = "ShelterAdmin,SuperAdmin")]
        public async Task<IActionResult> CreateAnimal([FromBody] CreateAnimalDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var shelterExists = await _context.Shelters.AnyAsync(s => s.Id == dto.ShelterId);
            if (!shelterExists)
            {
                return NotFound("Shelter not found.");
            }

            var animal = new Animal
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Species = dto.Species,
                Breed = dto.Breed,
                Age = dto.Age,
                Status = dto.Status,
                PhotoURL = dto.PhotoURL,
                ShelterId = dto.ShelterId,
                CreatedAtUtc = DateTime.UtcNow,
                UserLastModified = null
            };

            _context.Animals.Add(animal);
            await _context.SaveChangesAsync();

            var animalWithShelter = await _context.Animals
                .Include(a => a.Shelter)
                .ThenInclude(s => s.Address)
                .FirstOrDefaultAsync(a => a.Id == animal.Id);

            return CreatedAtAction(nameof(GetAnimal), new { id = animal.Id }, animal);
        }
    }
}
