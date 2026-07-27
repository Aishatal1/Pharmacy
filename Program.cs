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
        
        builder.Services.AddValidation();
        var connString = "Data Source = Pharma.db";
        builder.Services.AddSqlite<PharmaContext>(connString);

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
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();
        builder.Services.AddFluentValidationAutoValidation();
        builder.Services.AddFluentValidationClientsideAdapters();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();

        //middleware
        app.UseMiddleware<ActivityLoggingMiddleware>();
        app.UseMiddleware<ValidationMiddleware>();

        app.MapAuthEndpoints();
        app.MapSalesEndpoints();
        app.MapBillEndpoints();
        app.MapActivityLogEndpoints();

        app.Run();
    }
}