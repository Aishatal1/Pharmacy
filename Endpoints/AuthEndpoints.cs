using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Data;
using Pharmacy.Models;
using Pharmacy.Dtos;
using BCrypt.Net;  

namespace Pharmacy.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        // Register
        app.MapPost("/auth/register", async (
            CreateUserDto createDto,
            PharmaContext context) =>
        {
            if (await context.Users.AnyAsync(u => u.Username == createDto.Username))
            {
                return Results.Conflict($"Username '{createDto.Username}' already exists");
            }

            var validRoles = new[] { "Admin", "Cashier", "Manager" };
            if (!validRoles.Contains(createDto.Role))
            {
                return Results.BadRequest($"Role must be one of: {string.Join(", ", validRoles)}");
            }

            var user = new User
            {
                Username = createDto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(createDto.Password),
                FullName = createDto.FullName,
                Role = createDto.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            return Results.Ok(new { message = "User created successfully", userId = user.Id });
        });

        // LOGIN
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

if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))            {
                return Results.Unauthorized();
            }

            if (!user.IsActive)
            {
                return Results.Unauthorized();
            }

            // Generate token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("YourSuperSecretKeyHereAtLeast32CharactersLong!");
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("FullName", user.FullName)
                }),
                Expires = DateTime.UtcNow.AddHours(8),
                Issuer = "http://localhost:5103",
                Audience = "http://localhost:5103",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // Simple response
            var response = new
            {
                token = tokenString,
                user = new
                {
                    user.Id,
                    user.Username,
                    user.FullName,
                    user.Role,
                    user.IsActive,
                    user.CreatedAt
                }
            };

            return Results.Json(response);
        });

        // Logout
        app.MapPost("/auth/logout", async (
            ClaimsPrincipal user,
            PharmaContext context) =>
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);  
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Results.Unauthorized();
            }     
            var currentUser = await context.Users.FindAsync(userId);
            if (currentUser == null)
            {
                return Results.Unauthorized();
            } 
            
            return Results.Json(new { message = "Logout successful", user = currentUser.Username });
        });
    }
}
