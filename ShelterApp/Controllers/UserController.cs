using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ShelterApp
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Superadmin")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<User> _userManager;

        public UsersController(UserManager<User> userManager)
        {
            _userManager = userManager;
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

        [HttpDelete("{id}")]
        
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Superadmin"))
            {
                return BadRequest("Cannot delete admin users.");
            }

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded ? NoContent() : BadRequest(result.Errors);
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
        public async Task<IActionResult> RevokeAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound("User not found");

            if (!await _userManager.IsInRoleAsync(user, "ShelterAdmin"))
            {
                return BadRequest("User is not an admin");
            }

            var result = await _userManager.RemoveFromRoleAsync(user, "ShelterAdmin");
            return result.Succeeded ? Ok() : BadRequest(result.Errors);
        }
    }
}