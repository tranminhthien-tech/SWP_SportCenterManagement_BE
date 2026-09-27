using System.Security.Claims;

namespace SWP_SportCenter.Service.Jwt;

public interface IJwtService
{
    public string GenerateAccessToken(IEnumerable<Claim> claims);
    
    ClaimsPrincipal ValidateToken(string token);
}