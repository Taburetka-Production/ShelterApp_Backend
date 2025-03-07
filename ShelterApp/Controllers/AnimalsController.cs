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

        [HttpPut("{id}")]
        //[Authorize(Roles = "ShelterAdmin,SuperAdmin")]
        public async Task<IActionResult> UpdateAnimal(Guid id, [FromBody] UpdateAnimalDto updatedAnimalDto)
        {
            var animal = await _unitOfWork.AnimalRepository.GetByIdAsync(id);

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
                await _unitOfWork.SaveAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict("Error occurred while updating the animal.");
            }

            return NoContent();
        }

        [HttpPost]
        //[Authorize(Roles = "ShelterAdmin,SuperAdmin")]
        public async Task<IActionResult> CreateAnimal([FromBody] CreateAnimalDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var shelter = await _unitOfWork.ShelterRepository.GetByIdAsync(dto.ShelterId);
            if (shelter == null)
            {
                return NotFound("Shelter not found.");
            }

            //var shelterExists = await _context.Shelters.AnyAsync(s => s.Id == dto.ShelterId);
            //if (!shelterExists)
            //{
            //    return NotFound("Shelter not found.");
            //}

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

            await _unitOfWork.AnimalRepository.AddAsync(animal);
            await _unitOfWork.SaveAsync();

            var animalWithShelert = await _unitOfWork.AnimalRepository.GetByIdAsync(animal.Id, includeProperties: "Shelter,Shelter.Address");

            //var animalWithShelter = await _context.Animals
            //    .Include(a => a.Shelter)
            //    .ThenInclude(s => s.Address)
            //    .FirstOrDefaultAsync(a => a.Id == animal.Id);

            return CreatedAtAction(nameof(GetAnimal), new { id = animal.Id }, animal);
        }
    }
}
