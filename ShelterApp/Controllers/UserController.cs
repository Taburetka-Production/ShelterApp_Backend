using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ShelterApp
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<User> _userManager;

        public UsersController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetUserInfo()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User identifier not found in claims.");
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            var userInfo = new
            {
                user.UserName,
                user.Name,
                user.Surname,
                user.Email,
                user.AvatarUrl,
                user.Age,
                user.PhoneNumber
            };

            return Ok(userInfo);
        }

        [HttpPut("info")]
        public async Task<IActionResult> UpdateUserInfo([FromBody] UpdateUserDto updatedUserDto)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized("User not found");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            user.UserName = updatedUserDto.UserName ?? user.UserName;
            user.Name = updatedUserDto.Name ?? user.Name;
            user.Surname = updatedUserDto.Surname ?? user.Surname;
            user.Age = updatedUserDto.Age ?? user.Age;
            user.PhoneNumber = updatedUserDto.PhoneNumber ?? user.PhoneNumber;
            user.AvatarUrl = updatedUserDto.AvatarUrl ?? user.AvatarUrl;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return NoContent();

        }
    }
}