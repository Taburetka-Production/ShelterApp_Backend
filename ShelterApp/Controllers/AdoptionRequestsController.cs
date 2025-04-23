using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShelterApp;
using ShelterApp.Data;
using System;
using System.Security.Claims;
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
    [Authorize(Roles = "ShelterAdmin")]
    [HttpGet("by-shelter/{shelterSlug}")]
    public async Task<IActionResult> GetAdoptionRequestsByShelter(string shelterSlug)
    {
        // 1. Знайти шелтер за слагом
        var shelter = await _unitOfWork.ShelterRepository.GetFirstOrDefaultAsync(
            filter: s => s.Slug == shelterSlug,
            includeProperties: "Animals"
        );

        if (shelter == null)
        {
            return NotFound("Shelter not found");
        }

        // 2. Отримати всі adoption requests для тварин цього шелтера
        var animalIds = shelter.Animals.Select(a => a.Id).ToList();

        var requests = await _unitOfWork.AdoptionRequestRepository.GetAllAsync(
            filter: r => animalIds.Contains(r.AnimalId),
            includeProperties: "User,Animal"
        );

        // 3. Відобразити потрібні поля
        var result = requests.Select(request => new
        {
            UserName = request.User.Name,
            UserSurname = request.User.Surname,
            UserEmail = request.User.Email,
            UserPhone = request.User.PhoneNumber,
            AnimalName = request.Animal.Name,
            AnimalSlug = request.Animal.Slug
        });

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> CreateAdoptionRequest([FromBody] string animalSlug)
    {
        // Отримання ID поточного користувача
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _unitOfWork.UserRepository.GetByIdAsync(currentUserId);
        if (user == null) return Unauthorized("User not found");

        // Пошук тварини за слагом
        var animal = await _unitOfWork.AnimalRepository.GetBySlugAsync(animalSlug);
        if (animal == null) return NotFound("Animal not found");

        // Автоматичне заповнення даних
        var request = new AdoptionRequest
        {
            UserId = currentUserId,
            AnimalId = animal.Id, // Використовуємо знайдений animalId
            RequestDate = DateTime.UtcNow,
            Status = "In processing"
        };

        // Оновлення статусу тварини
        animal.Status = "Reserve";
        _unitOfWork.AnimalRepository.Update(animal);

        await _unitOfWork.AdoptionRequestRepository.AddAsync(request);
        await _unitOfWork.SaveAsync();

        return Ok(request);
    }
}