using Microsoft.AspNetCore.Mvc;

namespace KtcWeb.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(
        ActiveDirectoryService adService,
        ILogger<AuthController> logger) : ControllerBase
    {
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            logger.LogInformation("Login attempt for user {Username}", request.Username);

            bool isAuth = adService.Authenticate(request.Username, request.Password);

            if (!isAuth)
                return Unauthorized("Identifiants invalides");

            var roles = adService.GetRoles(request.Username, request.Password);
            string token = adService.GenerateJwtToken(request.Username, roles);

            return Ok(new AuthResponse
            {
                Username   = request.Username,
                Roles      = roles,
                Token      = token,
                Expiration = DateTime.Now.AddHours(2)
            });
        }
    }
}
