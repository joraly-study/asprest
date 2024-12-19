using asp_rest.Data;
using asp_rest.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace asp_rest.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly ProductContext _context;

    public UsersController(ProductContext context)
    {
        _context = context;
    }

    // GET: api/users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        return await _context.Users.ToListAsync();
    }
    // GET: api/users/roles
    [HttpGet("roles")]
    public async Task<ActionResult<IEnumerable<UserRoleCount>>> GetUserRoleCounts()
    {
        var roles = await _context.Users
            .GroupBy(u => u.Role)
            .Select(g => new UserRoleCount
            {
                Role = g.Key,
                UserCount = g.Count()
            })
            .ToListAsync();

        return Ok(roles);
    }
}
public class UserRoleCount
{
    public string Role { get; set; }
    public int UserCount { get; set; }
}