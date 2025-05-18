using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TVS_App.Application.Handlers;
using TVS_App.Application.Interfaces;
using TVS_App.Application.Repositories;
using TVS_App.Infrastructure.Data;
using TVS_App.Infrastructure.Models;
using TVS_App.Infrastructure.Repositories;
using TVS_App.Infrastructure.Security;
using TVS_App.Infrastructure.Services;

namespace TVS_App.Api.Common;

public static class BuilderExtensions
{
    public static void AddSqlServer(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<ApplicationDataContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
        );
    }

    public static void AddAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(x =>
            {
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(builder.Configuration["Jwt:SecretKey"]!)),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
                x.RequireHttpsMetadata = false;
            });

        builder.Services.AddAuthorization();
    }

    public static void AddDependencies(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
        builder.Services.AddScoped<IServiceOrderRepository, ServiceOrderRepository>();
        builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
        builder.Services.AddTransient<IGenerateServiceOrderPdf, GenerateServiceOrderPdfService>();
        builder.Services.AddScoped<CustomerHandler>();
        builder.Services.AddScoped<ServiceOrderHandler>();
        builder.Services.AddScoped<NotificationHandler>();
    }

    public static void AddJwtService(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<JwtService>();
    }

    public static void AddIdentity(this WebApplicationBuilder builder)
    {
        builder.Services.AddIdentity<User, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDataContext>()
            .AddDefaultTokenProviders();
    }

    public static void ConfigureJsonSerializer(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
    }

    public static void AddSwagger(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer(); 

        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "Sua API", Version = "v1" });

            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "Insira o token JWT no formato: Bearer {seu_token}"
            });

            c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] {}
                }
            });
        });
    }
    
    public static void AddCorsConfiguration(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        
        var policyName = configuration["Cors:PolicyName"];
        var origins = configuration["Cors:Origins"]?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(policyName!, policy =>
            {
                policy
                    .WithOrigins(origins!)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });
    }
}