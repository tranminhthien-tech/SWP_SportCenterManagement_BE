using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace SWP_SportCenter.Service.Jwt;

public class JwtService: IJwtService
{
    private readonly Jwtoption _jwtOption = new();

    public JwtService(IConfiguration configuration)
    {
        configuration.GetSection(nameof(Jwtoption)).Bind(_jwtOption);
    }
    public string GenerateAccessToken(IEnumerable<Claim> claims)
    {
        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOption.SecretKey));
        //tạo 1 key ể mã hoóa token, sử dụng secretKey từ JwtOptión
        var signingCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);
        // Tạo 1 đối tượng SigningCredentials để xác định thuật toán mã hóa và key sử dụng để ký token
        var tokenOptions = new JwtSecurityToken(
            issuer: _jwtOption.Issuer,// cái token này đc kí - đc tạo ra bới ai
            audience: _jwtOption.Audience,// cái token này dành cho ai, tổ chức nào
            claims: claims,// Những thông tin mà bạn muốn lưu tữ trong token
            // Thường là những thông tin v ng dùng như Id, email, vai trò, ..
            //Nằm trong payload
            expires: DateTime.UtcNow.AddMinutes(_jwtOption.ExpireMinutes), // token sẽ hết hạn trong bao lâu
            signingCredentials: signingCredentials
        );
        
        var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenOptions);
        //Sau đó gọi JwtSecurityTokenHandler
        // để tạo ra token dưới dạng chuỗi (string) ừ các thông tin đã cung cấp trên
        return tokenString;
    }

    public ClaimsPrincipal ValidateToken(string token)
    {
        throw new NotImplementedException();
    }
}