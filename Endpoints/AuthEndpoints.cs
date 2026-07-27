using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Data;
using Pharmacy.Models;
using Pharmacy.Dtos;

namespace Pharmacy.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        // Register a new user
        app.MapPost("/auth/register", async (
            CreateUserDto createDto,
            PharmaContext context) =>
        {
            // Check if username exists
            if (await context.Users.AnyAsync(u => u.Username == createDto.Username))
            {
                return Results.Conflict($"Username '{createDto.Username}' already exists");
            }

            // Check if role is valid
            var validRoles = new[] { "Admin", "Cashier", "Manager" };
            if (!validRoles.Contains(createDto.Role))
            {
                return Results.BadRequest($"Role must be one of: {string.Join(", ", validRoles)}");
            }

            var user = new User
            {
                Username = createDto.Username,
                PasswordHash = createDto.Password, // TODO: Use hashing!
                FullName = createDto.FullName,
                Role = createDto.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            return Results.Ok(new 
            { 
                Message = "User created successfully", 
                UserId = user.Id,
                Username = user.Username,
                Role = user.Role
            });
        })
        .WithName("Register");

        // Login - Returns JWT Token
        app.MapPost("/auth/login", async (
            LoginDto loginDto,
            PharmaContext context) =>
        {
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

            if (user == null)
            {
                return Results.Unauthorized();
            }

            // TODO: Use proper password hashing!
            if (user.PasswordHash != loginDto.Password)
            {
                return Results.Unauthorized();
            }

            if (!user.IsActive)
            {
                return Results.Unauthorized();
            }

            // Generate JWT token
            var token = GenerateJwtToken(user);

            var userDto = new UserDto(
                user.Id,
                user.Username,
                user.FullName,
                user.Role,
                user.IsActive,
                user.CreatedAt
            );

            return Results.Ok(new LoginResponseDto(token, userDto));
        })
        .WithName("Login");
    }

    private static string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("FullName", user.FullName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSuperSecretKeyHereAtLeast32CharactersLong!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "PharmacyAPI",
            audience: "PharmacyClient",
            claims: claims,
            expires: DateTime.Now.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}