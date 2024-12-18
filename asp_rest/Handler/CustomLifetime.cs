using Microsoft.IdentityModel.Tokens;

namespace asp_rest.Handler;

public class CustomLifetime
{
    public static bool CustomLifetimeValidator(DateTime? notBefore, DateTime? expires, SecurityToken tokenToValidate,
        TokenValidationParameters @param)
    {
        if (expires != null)
        {
            return expires > DateTime.UtcNow;
        }

        return false;
    }
}