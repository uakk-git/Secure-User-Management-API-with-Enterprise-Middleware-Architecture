using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UserManagementAPI.Authentication;
using UserManagementAPI.Data;
using UserManagementAPI.Models;
using UserManagementAPI.Services;
using UserManagementAPI.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// CONFIGURATION SETUP
// ============================================================

// Load JWT settings from configuration
var jwtSettings = builder.Configuration.GetSection("Jwt");

// ============================================================
// SERVICE REGISTRATION
// ============================================================

// Add logging configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add Entity Framework Core with In-Memory Database
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseInMemoryDatabase("UserManagementDb")
    .EnableSensitiveDataLogging());

// Add Application Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();

// Add Controllers
builder.Services.AddControllers();

// Add CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", corsBuilder =>
    {
        corsBuilder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
    });
});

// Add Swagger Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "User Management API",
        Version = "v2.0",
        Description = "User Management API with comprehensive middleware for logging, error handling, and authentication"
    });

    // Add JWT Bearer security definition for Swagger UI
    var jwtSecurityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Name = "JWT Authentication",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        Description = "Enter the JWT token"
    };

    options.AddSecurityDefinition("Bearer", jwtSecurityScheme);

    var securityRequirement = new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, new[] { "Bearer" } }
    };

    options.AddSecurityRequirement(securityRequirement);
});

// Add JWT Authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"] ?? "default-secret-key-change-in-production")),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "UserManagementAPI",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "UserManagementAPI-Users",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

var app = builder.Build();

// ============================================================
// MIDDLEWARE PIPELINE CONFIGURATION (ORDER IS CRITICAL)
// ============================================================

// 1. Exception Handling Middleware - FIRST (catches all exceptions)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 2. JWT Authentication Middleware - SECOND (validates tokens)
app.UseMiddleware<JwtAuthenticationMiddleware>();

// 3. Request/Response Logging Middleware - THIRD (logs all requests/responses)
app.UseMiddleware<RequestResponseLoggingMiddleware>();

// ============================================================
// STANDARD MIDDLEWARE
// ============================================================

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "User Management API v2.0");
        options.RoutePrefix = string.Empty;
    });
}

// Use HTTPS redirection
app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowAll");

// Use Authentication
app.UseAuthentication();

// Use Authorization
app.UseAuthorization();

// Map Controllers
app.MapControllers();

// Add health check endpoint (public, no auth required)
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow })
    .WithName("HealthCheck")
    .WithOpenApi();

app.Run();