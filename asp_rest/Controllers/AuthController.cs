using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using asp_rest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;

namespace asp_rest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var user = new User
            {
                UserName = model.Login,
                Email = model.Login,
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User registered successfully: {UserName}", user.UserName);
                return Ok(new { message = "User registered successfully" });
            }

            _logger.LogError("User registration failed: {Errors}", result.Errors);
            return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Login) || string.IsNullOrEmpty(model.Password))
            {
                _logger.LogWarning("Invalid login model");
                return BadRequest("Invalid login model");
            }

            var result = await _signInManager.PasswordSignInAsync(model.Login, model.Password, true, false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByNameAsync(model.Login);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {Login}", model.Login);
                    return Unauthorized("User not found");
                }

                var token = GenerateJwtToken(user);

                // Add user token
                await _userManager.SetAuthenticationTokenAsync(user, "Default", "Bearer", token);

                _logger.LogInformation("User logged in successfully: {UserName}", user.UserName);
                return Ok(new { token });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Account is locked out: {Login}", model.Login);
                return Unauthorized("Account is locked out");
            }

            if (result.IsNotAllowed)
            {
                _logger.LogWarning("Account is not allowed to sign in: {Login}", model.Login);
                return Unauthorized("Account is not allowed to sign in");
            }

            if (result.RequiresTwoFactor)
            {
                _logger.LogWarning("Two-factor authentication is required: {Login}", model.Login);
                return Unauthorized("Two-factor authentication is required");
            }

            _logger.LogWarning("Invalid login or password: {Login}", model.Login);
            return Unauthorized("Invalid login or password");
        }

        [HttpGet("current-user")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogError("User not found for current request.");
                return NotFound();
            }

            _logger.LogInformation("Current user retrieved: {UserName}", user.UserName);
            return Ok(new { user.UserName });
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class RegisterModel
    {
        public string Login { get; set; }
        public string Password { get; set; }
    }

    public class LoginModel
    {
        public string Login { get; set; }
        public string Password { get; set; }
    }
}
