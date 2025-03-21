using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShelterApp;
using ShelterApp.Data;
using System;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class AdoptionRequestsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdoptionRequestsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateAdoptionRequest([FromBody] AdoptionRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Перевірка, чи існує тварина
        var animal = await _unitOfWork.AnimalRepository.GetByIdAsync(request.AnimalId);
        if (animal == null)
        {
            return NotFound("Animal not found");
        }

        // Додаємо дату заявки та статус
        request.RequestDate = DateTime.UtcNow;
        request.Status = "Pending";

        await _unitOfWork.AdoptionRequestRepository.AddAsync(request);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(GetAdoptionRequest), new { id = request.Id }, request);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAdoptionRequest(Guid id)
    {
        var request = await _unitOfWork.AdoptionRequestRepository.GetByIdAsync(
            id,
            includeProperties: "User,Animal"
        );

        if (request == null)
        {
            return NotFound();
        }

        return Ok(request);
    }
}