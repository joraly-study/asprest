using Microsoft.AspNetCore.Identity;

namespace asp_rest.Models;

public class User : IdentityUser<Guid>
{
}

public class Role : IdentityRole<Guid>
{
    
}