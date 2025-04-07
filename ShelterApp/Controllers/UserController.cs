using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Data;
using ShelterApp.DTOs;
using System.Security.Claims;

namespace ShelterApp
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Superadmin")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private IUnitOfWork _unitOfWork;

        public UsersController(UserManager<User> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        [HttpGet("all-users")]

        public async Task<IActionResult> GetAllUsers()
        {
            var allUsers = _userManager.Users.ToList();
            var usersWithRoles = new List<object>();

            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);

                // Пропускаємо користувачів з роллю Superadmin
                if (roles.Contains("Superadmin"))
                {
                    continue;
                }

                usersWithRoles.Add(new
                {
                    user.Id,
                    user.AvatarUrl,
                    user.Name,
                    user.Surname,
                    user.Age,
                    user.Email,
                    Roles = roles
                });
            }

            return Ok(usersWithRoles);
        }

        [HttpPost("grant-admin/{id}")]

        public async Task<IActionResult> GrantAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, "ShelterAdmin"))
            {
                return BadRequest("User is already an admin.");
            }

            var result = await _userManager.AddToRoleAsync(user, "ShelterAdmin");
            return result.Succeeded ? Ok() : BadRequest(result.Errors);
        }

        [HttpPost("revoke-admin/{id}")]
        public async Task<IActionResult> RevokeAdmin(
    string id,
    [FromBody] TransferAdminDto transferDto // DTO з email нового адміна
)
        {
            // 1. Знайти поточного адміна за id
            var currentAdmin = await _userManager.FindByIdAsync(id);
            if (currentAdmin == null)
            {
                return NotFound("Поточний адмін не знайдений.");
            }

            // 2. Перевірити, чи є він адміном
            if (!await _userManager.IsInRoleAsync(currentAdmin, "ShelterAdmin"))
            {
                return BadRequest("Користувач не є адміном.");
            }

            // 3. Знайти нового адміна за email
            var newAdmin = await _userManager.FindByEmailAsync(transferDto.NewAdminEmail);
            if (newAdmin == null)
            {
                return NotFound("Новий адмін не знайдений.");
            }

            // 4. Перевірити, чи новий адмін вже не має цієї ролі
            if (await _userManager.IsInRoleAsync(newAdmin, "ShelterAdmin"))
            {
                return BadRequest("Новий користувач вже є адміном.");
            }

            // 5. Видалити роль у поточного адміна
            var removeResult = await _userManager.RemoveFromRoleAsync(currentAdmin, "ShelterAdmin");
            if (!removeResult.Succeeded)
            {
                return BadRequest(removeResult.Errors);
            }

            // 6. Додати роль новому адміну
            var addResult = await _userManager.AddToRoleAsync(newAdmin, "ShelterAdmin");
            if (!addResult.Succeeded)
            {
                return BadRequest(addResult.Errors);
            }

            // 7. Зберегти зміни та закрити транзакцію
            await _unitOfWork.SaveAsync();

            return Ok("Роль успішно передана.");
        }



        // Видалення звичайного користувача
        [HttpDelete("user/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Superadmin"))
            {
                return BadRequest("Цей користувач є адміністратором. Використовуйте /delete-admin.");
            }
            else 
            {
                // Видалення пов'язаних даних користувача (як у попередньому коді)
                var userAnimals = await _unitOfWork.UsersAnimalRepository.GetAllAsync(ua => ua.UserId == id);
                _unitOfWork.UsersAnimalRepository.RemoveRange(userAnimals);

                var userShelters = await _unitOfWork.UsersShelterRepository.GetAllAsync(us => us.UserId == id);
                _unitOfWork.UsersShelterRepository.RemoveRange(userShelters);

                var adoptionRequests = await _unitOfWork.AdoptionRequestRepository.GetAllAsync(ar => ar.UserId == id);
                _unitOfWork.AdoptionRequestRepository.RemoveRange(adoptionRequests);

                var shelterFeedbacks = await _unitOfWork.ShelterFeedbackRepository.GetAllAsync(sf => sf.UserId == id);
                _unitOfWork.ShelterFeedbackRepository.RemoveRange(shelterFeedbacks);

                await _unitOfWork.SaveAsync();

                
                if (!roles.Contains("ShelterAdmin"))
                {
                    var result = await _userManager.DeleteAsync(user);
                    return result.Succeeded ? NoContent() : BadRequest(result.Errors);
                }
                else
                {
                    // Знаходимо притулок, який належить адміністратору
                    var shelter = await _unitOfWork.ShelterRepository.GetAllAsync(s => s.UserId == id);
                    if (shelter != null && shelter.Any())
                    {
                        var shelterToDelete = shelter.First();

                        // Видаляємо притулок і всі його залежності (як у попередньому коді)
                        var animals = await _unitOfWork.AnimalRepository.GetAllAsync(a => a.ShelterId == shelterToDelete.Id);
                        foreach (var animal in animals)
                        {
                            var animalPhotos = await _unitOfWork.AnimalPhotoRepository.GetAllAsync(ap => ap.AnimalId == animal.Id);
                            _unitOfWork.AnimalPhotoRepository.RemoveRange(animalPhotos);

                            var adoptionRequests_animal = await _unitOfWork.AdoptionRequestRepository.GetAllAsync(ar => ar.AnimalId == animal.Id);
                            _unitOfWork.AdoptionRequestRepository.RemoveRange(adoptionRequests_animal);

                            var usersAnimals = await _unitOfWork.UsersAnimalRepository.GetAllAsync(ua => ua.AnimalId == animal.Id);
                            _unitOfWork.UsersAnimalRepository.RemoveRange(usersAnimals);
                        }
                        _unitOfWork.AnimalRepository.RemoveRange(animals);

                        var shelterFeedbacks_animal = await _unitOfWork.ShelterFeedbackRepository.GetAllAsync(sf => sf.ShelterId == shelterToDelete.Id);
                        _unitOfWork.ShelterFeedbackRepository.RemoveRange(shelterFeedbacks_animal);

                        var usersShelters = await _unitOfWork.UsersShelterRepository.GetAllAsync(us => us.ShelterId == shelterToDelete.Id);
                        _unitOfWork.UsersShelterRepository.RemoveRange(usersShelters);

                        _unitOfWork.ShelterRepository.Remove(shelterToDelete);
                    }

                    // Видаляємо самого адміністратора
                    await _unitOfWork.SaveAsync();
                    var result = await _userManager.DeleteAsync(user);
                    return result.Succeeded ? NoContent() : BadRequest(result.Errors);
                }
                    
            }
            
        }

        
    }
}