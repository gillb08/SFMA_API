using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using SFMA_API.Api.Filters;
using SFMA_API.Data.Context;
using SFMA_API.Data.Extensions;
using SFMA_API.Data.SeedData;
using SFMA_API.Models.Configuration.MappingConfiguration;
using SFMA_API.Models.Entities;
using SFMA_API.Services.Exceptions;
using SFMA_API.Services.Extensions;
using SFMA_API.Services.Handlers;
using SFMA_API.Services.Infrastructure;
using System;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var root = Directory.GetCurrentDirectory();
DotEnv.Load(Path.Combine(root, ".env"));
DotEnv.Load(Path.Combine(root, "SFMA_API.Api", ".env"));
DotEnv.Load(Path.Combine(AppContext.BaseDirectory, ".env"));

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Add services
builder.Services.Configure<JWTConfiguration>(builder.Configuration.GetSection("JwtConfig"));
builder.Services.Configure<SFMA_API.Models.Configuration.SchoolSettings>(builder.Configuration.GetSection("SchoolSettings"));

static string ResolveConnectionString(IConfiguration config)
{
    var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? Environment.GetEnvironmentVariable("DATABASE_URL")
        ?? Environment.GetEnvironmentVariable("DefaultConnection")
        ?? config.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(raw) || raw.Contains("<NEON_HOST>"))
    {
        var fallback = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? Environment.GetEnvironmentVariable("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(fallback))
            raw = fallback;
    }

    if (!string.IsNullOrWhiteSpace(raw))
    {
        if (raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(raw);
            var userInfo = uri.UserInfo.Split(':');
            var user = userInfo[0];
            var pass = userInfo.Length > 1 ? userInfo[1] : "";
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var db = uri.AbsolutePath.TrimStart('/');
            return $"Host={host};Port={port};Database={db};Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true;";
        }
        return raw;
    }

    return "Host=localhost;Port=5432;Database=SFMA_DB;Username=postgres;Password=postgres;SSL Mode=Prefer;";
}

var connectionString = ResolveConnectionString(builder.Configuration);

builder.Services.AddDbContext<SFMA_APIDbContext>(options =>
{
    options.UseNpgsql(connectionString, b => b.MigrationsAssembly("SFMA_API.Data"));
    options.ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.NavigationBaseIncludeIgnored));
});

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(config =>
{
    var alphabet = "abcdefghijklmnopqrstuvwxyz";
    config.User.AllowedUserNameCharacters = $"{alphabet}{alphabet.ToUpper()}0123456789/-@._+";
    config.User.RequireUniqueEmail = true;
    config.Password.RequiredLength = 8;
    config.Password.RequireDigit = true;
    config.Password.RequiredUniqueChars = 0;
    config.Password.RequireNonAlphanumeric = true;
    config.Password.RequireUppercase = true;
    config.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<SFMA_APIDbContext>()
.AddDefaultTokenProviders()
.AddImpersonationTokenProvider()
.AddRefreshTokenProvider();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(jwt =>
{
    var secret = builder.Configuration["JwtConfig:Secret"] ?? "SuperSecretKeyForSFMAStFaithModelAcademyPortal2026JWTSigningMustBeLongEnough123456!";
    var key = Encoding.ASCII.GetBytes(secret);

    jwt.SaveToken = true;
    jwt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JwtConfig:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JwtConfig:Audience"],
        ValidateLifetime = true,
        RequireExpirationTime = true
    };
});

builder.Services.AddAuthorization(cfg =>
{
    cfg.AddPolicy("Authorization", policy => policy.Requirements.Add(new AuthorizationRequirement()));
});

builder.Services.BindConfigurations(builder.Configuration);

// Add AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddMaps(typeof(SfmaMappingProfile).Assembly);
});

builder.Services.AddTransient(typeof(PagedResponseConfiguration.PagedListToPagedResponseConverter<,>));

// Register Domain Services
builder.Services.AddMemoryCache();
builder.Services.RegisterServices();
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers(setupAction =>
{
    setupAction.Filters.Add<ValidateModelAttribute>();
    setupAction.ReturnHttpNotAcceptable = true;
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
})
.AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(o => o.AddPolicy("AllowConfiguredOrigins", b =>
{
    b.WithOrigins(
        "https://www.stfaithmodelacademy.com",
        "http://localhost:3000"
    )
     .AllowAnyMethod()
     .AllowAnyHeader();
}));

builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "St. Faith Model Academy API",
        Version = "v1",
        Description = "Production REST API for St. Faith Model Academy Portal (SFMA)"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\""
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
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "St. Faith Model Academy API v1");
});

app.UseCors("AllowConfiguredOrigins");

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.ConfigureException(builder.Environment);

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Ensure seed data is populated
await SeedApplicationData.EnsurePopulated(app);

app.Run();
