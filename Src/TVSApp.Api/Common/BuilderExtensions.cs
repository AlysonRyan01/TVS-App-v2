using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;
using TVS_App.Application.Handlers;
using TVS_App.Application.Interfaces;
using TVS_App.Domain.Repositories.Customers;
using TVS_App.Domain.Repositories.Notifications;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Infrastructure.Configurations;
using TVS_App.Infrastructure.Data;
using TVS_App.Infrastructure.Models;
using TVS_App.Infrastructure.Repositories.Customers;
using TVS_App.Infrastructure.Repositories.Notifications;
using TVS_App.Infrastructure.Repositories.ServiceOrders;
using TVS_App.Infrastructure.Security;
using TVS_App.Infrastructure.Services;
using TVS_App.Infrastructure.Services.Whatsapp;

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
        builder.Services.AddScoped<ICustomerCommandRepository, CustomerCommandRepository>();
        builder.Services.AddScoped<ICustomerQueryRepository, CustomerQueryRepository>();
        builder.Services.AddScoped<IServiceOrderCommandRepository, ServiceOrderCommandRepository>();
        builder.Services.AddScoped<IServiceOrderQueryRepository, ServiceOrderQueryRepository>();
        builder.Services.AddScoped<INotificationCommandRepository, NotificationCommandRepository>();
        builder.Services.AddScoped<INotificationQueryRepository, NotificationQueryRepository>();
        builder.Services.AddTransient<IGenerateServiceOrderPdf, GenerateServiceOrderPdfService>();
        builder.Services.AddScoped<CustomerHandler>();
        builder.Services.AddScoped<ServiceOrderHandler>();
        builder.Services.AddScoped<NotificationHandler>();
        builder.Services.AddHttpClient<IWhatsappService, WhatsappService>();
        builder.Services.Configure<EvolutionApiSettings>(builder.Configuration.GetSection("EvolutionApiSettings"));
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
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddEndpointsApiExplorer(); 

            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new()
                {
                    Title = "TVS APP - API",
                    Description = "API para gerenciamento de ordens de serviço - empresa TVS Eletrônica",
                    Version = "v1",
                    Contact = new OpenApiContact { Name = "Alyson Ryan Ullirsch", Email = "alysonullirsch8@gmail.com" }
                });
                
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Insira o token JWT no formato: Bearer {seu_token}"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });
        }
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

    public static void AddQuestPdfConfiguration(this WebApplicationBuilder builder)
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static void AddSignalR(this WebApplicationBuilder builder)
    {
        builder.Services.AddSignalR();
    }
}