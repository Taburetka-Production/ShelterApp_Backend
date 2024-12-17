using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ShelterApp
{
    [ApiController]
    [Route("[controller]")]
    public class SheltersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SheltersController(ApplicationDbContext context)
        {
            _context = context;
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<Shelter>>> GetShelters()
        {
            if (_context.Shelters == null)
            {
                return NotFound();
            }

            var shelters = await _context.Shelters
                .Include(s => s.Address)
                .ToListAsync();

            return Ok(shelters);
        }

    }
}
