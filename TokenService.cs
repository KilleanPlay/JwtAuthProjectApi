using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public class TokenService
{
    private readonly IConfiguration _config;
    public TokenService(IConfiguration config) => _config = config;

    public string GenerateToken(string username, string role, string? email = null, string? phone = null)
    {
        var claims = new List<Claim>
        {
            // Standart .NET claim'leri (Authorize bunlar� kullan�r)
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),

            // JWT "sub" gibi yayg�n alanlar (opsiyonel)
            new Claim(JwtRegisteredClaimNames.Sub, username),

            // K�sa anahtarl� custom claim'ler (jwt.io'da net g�r�ns�n diye)
            new Claim("role", role)
        };

        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email));  // standart
            claims.Add(new Claim("email", email));           // k�sa isimli
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            claims.Add(new Claim(ClaimTypes.MobilePhone, phone)); // standart
            claims.Add(new Claim("phone", phone));                // k�sa isimli
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expireMinutes = int.TryParse(_config["Jwt:ExpireMinutes"], out var m) ? m : 60;

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(expireMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
