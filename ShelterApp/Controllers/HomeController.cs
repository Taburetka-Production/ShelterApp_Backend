using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ShelterApp.Controllers
{
    [Route("/")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        // GET: /
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { Message = "Welcome to the ASP.NET Core Web API!", Timestamp = DateTime.UtcNow });
        }

        // GET: /info
        [HttpGet("info")]
        public IActionResult GetInfo()
        {
            return Ok(new
            {
                Application = "YourAppName",
                Version = "1.0.0",
                Author = "YourName",
                ServerTime = DateTime.UtcNow
            });
        }

        // POST: /
        [HttpPost]
        public IActionResult Post([FromBody] string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return BadRequest("Message cannot be empty.");
            }

            return Ok(new { ReceivedMessage = message, ProcessedAt = DateTime.UtcNow });
        }
    }
}