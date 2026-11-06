using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TVS_App.Infrastructure.Data;
using TVS_App.Infrastructure.Models;

namespace TVS_App.Infrastructure.Security;

public class JwtService
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDataContext _context;

    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromHours(1);
    public static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public JwtService(IConfiguration configuration, UserManager<User> userManager, ApplicationDataContext context)
    {
        _configuration = configuration;
        _userManager = userManager;
        _context = context;
    }

    public async Task<string> GenerateAccessToken(User user)
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
            Expires = DateTime.UtcNow.Add(AccessTokenLifetime),

        };

        var token = handler.CreateToken(tokenDescriptor);

        return handler.WriteToken(token);

    }

    public async Task<string> GenerateRefreshTokenAsync(User user, string? createdByIp = null)
    {
        var refreshToken = new RefreshToken
        {
            Token = GenerateSecureRandomToken(),
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime),
            CreatedByIp = createdByIp
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken.Token;
    }

    public async Task<string?> RefreshAccessTokenAsync(string refreshToken)
    {
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == refreshToken);

        if (storedToken is null || !storedToken.IsActive)
            return null;

        var user = await _userManager.FindByIdAsync(storedToken.UserId);
        if (user is null)
            return null;

        return await GenerateAccessToken(user);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == refreshToken);

        if (storedToken is null || storedToken.RevokedAt is not null)
            return;

        storedToken.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
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

    private static string GenerateSecureRandomToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
