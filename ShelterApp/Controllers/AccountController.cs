namespace ShelterApp
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using ShelterApp.DTOs;
    using System.Security.Claims;
    using System.Threading.Tasks;

    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private const string DefaultUserRole = "User";

        public AccountController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegistrationDto registrationDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Перевірка унікальності Email
            var existingUser = await _userManager.FindByEmailAsync(registrationDto.Email);
            if (existingUser != null)
                return BadRequest("Email вже зареєстрований.");

            // Створення користувача з усіма обов’язковими полями
            var user = new User
            {
                UserName = registrationDto.UserName, // Використовуємо Email як UserName
                Email = registrationDto.Email,
                Name = registrationDto.Name,
                Surname = registrationDto.Surname,
                Age = registrationDto.Age,
                AvatarUrl = registrationDto.AvatarUrl,
                PhoneNumber = registrationDto.PhoneNumber
            };

            // Створення користувача з паролем
            var result = await _userManager.CreateAsync(user, registrationDto.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, "User");

            return Ok(new { message = "Користувача успішно зареєстровано!" });
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
            var roles = await _userManager.GetRolesAsync(user);

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
                user.PhoneNumber,
                Roles = roles
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

