using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TVS_App.Infrastructure.Models;

namespace TVS_App.Infrastructure.Security;

public class JwtService
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<User> _userManager;

    public JwtService(IConfiguration configuration, UserManager<User> userManager)
    {
        _configuration = configuration;
        _userManager = userManager;
    }

    public async Task<string> Generate(User user)
    {
        var handler = new JwtSecurityTokenHandler();

        var secret = _configuration["Jwt:SecretKey"];

        if (string.IsNullOrWhiteSpace(secret))
            throw new Exception("O SecretKey da API não pode estar vazia");

        var key = Encoding.ASCII.GetBytes(secret);

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = await GenerateClaims(user),
            SigningCredentials = credentials,
            Expires = DateTime.UtcNow.AddHours(8),

        };

        var token = handler.CreateToken(tokenDescriptor);

        return handler.WriteToken(token);

    }

    private async Task<ClaimsIdentity> GenerateClaims(User user)
    {
        var ci = new ClaimsIdentity();
        
        ci.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
        ci.AddClaim(new Claim(ClaimTypes.Email, user.Email ?? string.Empty));
        
        var roles = await _userManager.GetRolesAsync(user);

        foreach (var role in roles)
        {
            ci.AddClaim(new Claim(ClaimTypes.Role, role));
        }
        
        return ci;
    }

    public void SetTokensInsideCookie(string token, HttpContext context)
    {
        context.Response.Cookies.Append("accessToken", token, new CookieOptions
        {
            Expires = DateTime.UtcNow.AddHours(2),
            HttpOnly = true,
            IsEssential = true
        });
    }
}