using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using asp_rest.Data;
using asp_rest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace asp_rest.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly JwtSettings _options;
        private readonly ILogger<AuthController> _logger;
        private readonly ProductContext _context;

        public AuthController(ILogger<AuthController> logger, IOptions<JwtSettings> optAccess, ProductContext context)
        {
            _logger = logger;
            _options = optAccess.Value;
            _context = context;
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User
                {
                    Username = model.Username,
                    PasswordHash = HashPassword(model.Password),
                    Role = model.Role // Use the role from the model
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var token = Authenticate(user, 30); // 30 minutes token
                return Ok(new { Token = token, Message = "User registered successfully" });
            }

            return BadRequest(ModelState);
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == model.Username);

                if (user != null && user.PasswordHash == HashPassword(model.Password))
                {
                    var token = Authenticate(user, 30); // 30 minutes token
                    return Ok(new { Token = token });
                }

                return Unauthorized(new { Message = "Invalid username or password" });
            }

            return BadRequest(ModelState);
        }

        [HttpGet]
        [Route("CurrentUser")]
        public async Task<IActionResult> CurrentUser()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            if (identity != null)
            {
                var username = identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == username);

                if (user != null)
                {
                    return Ok(new
                    {
                        Username = user.Username,
                        Role = user.Role
                    });
                }
            }

            return Unauthorized();
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("Authenticate")]
        public string Authenticate(User user, int minutes)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("level", "123") // Если это необходимо
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));

            var jwt = new JwtSecurityToken
            (
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(minutes)),
                notBefore: DateTime.UtcNow,
                signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
            );
            var resp = new JwtSecurityTokenHandler().WriteToken(jwt);
            return resp;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var hash = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
                return hash;
            }
        }
    }

    public class RegisterModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }

    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
