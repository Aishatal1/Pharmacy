using Pharmacy.Dtos;
using Pharmacy.Dtos.Validators;
using Pharmacy.Data;
using Pharmacy.Models;
using Pharmacy.Endpoints;
using Pharmacy.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Add services
        builder.Services.AddValidation();
        var connString = "Data Source = Pharma.db";
        builder.Services.AddSqlite<PharmaContext>(connString);

        // Add JWT Authentication
        var key = Encoding.UTF8.GetBytes("YourSuperSecretKeyHereAtLeast32CharactersLong!");
        
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "PharmacyAPI",
                ValidAudience = "PharmacyClient",
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };
        });

        builder.Services.AddAuthorization();
        
        // Add OpenAPI (built-in to .NET 10)
        builder.Services.AddOpenApi();
        
        // Register FluentValidation
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();
        builder.Services.AddFluentValidationAutoValidation();
        builder.Services.AddFluentValidationClientsideAdapters();

        var app = builder.Build();

        // Configure pipeline
        if (app.Environment.IsDevelopment())
        {
            // Built-in OpenAPI endpoint
            app.MapOpenApi();
            
            // Use Swagger UI with the built-in OpenAPI
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/{documentName}.json", "Pharmacy API");
            });
        }

        // app.UseHttpsRedirection(); // Comment out for development
        app.UseAuthentication();
        app.UseAuthorization();

        // Add middleware
        app.UseMiddleware<ActivityLoggingMiddleware>();
        app.UseMiddleware<ValidationMiddleware>();

        // Map all endpoints
        app.MapAuthEndpoints();
        app.MapSalesEndpoints();
        app.MapBillEndpoints();
        app.MapActivityLogEndpoints();

        app.Run();
    }
}