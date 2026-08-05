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

        // JWT Authentication
        var secretKey = "YourSuperSecretKeyHereAtLeast32CharactersLong!";
        var issuer = "http://localhost:5103";
        var audience = "http://localhost:5103";

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(secretKey)
                    ),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            });

        builder.Services.AddAuthorization();
        
        // Add Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        // Register FluentValidation
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();
        builder.Services.AddFluentValidationAutoValidation();
        builder.Services.AddFluentValidationClientsideAdapters();

        var app = builder.Build();

        //pipeline configuration
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pharmacy API V1");
                c.RoutePrefix = "swagger";
            });
        }

        app.UseAuthentication();
        app.UseAuthorization();

        //middleware
        app.UseMiddleware<ActivityLoggingMiddleware>();
        app.UseMiddleware<ValidationMiddleware>();

        //endpoints
        app.MapAuthEndpoints();
        app.MapSalesEndpoints();
        app.MapBillEndpoints();
        app.MapActivityLogEndpoints();
        app.MapCustomerEndpoints();
        app.MapProductEndpoints();
        app.MapInvoiceEndpoints();
        app.MapTransactionEndpoints();
        app.Run();
    }
}