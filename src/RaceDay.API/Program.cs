using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDay.API.Data;
using RaceDay.API.Security;
using RaceDay.API.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Database Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<RaceDayDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(connectionString) && !connectionString.Contains("YOUR_SERVER"))
    {
        options.UseSqlServer(connectionString);
    }
    else
    {
        // Safe local fallback for zero-friction local setup and automated CI/CD testing
        options.UseInMemoryDatabase("RaceDayDb");
    }
});

// 2. Register Application Core Services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IBlobStorageService, AzureBlobStorageService>();

// 3. Register Controllers & JSON Serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// 4. Configure CORS for MVC Web Front-End
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 5. Configure Swagger / OpenAPI Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay RESTful Web API",
        Version = "v1",
        Description = "Enterprise RESTful API powering the RaceDay athletic event management platform (PROG6212 POE). Built with ASP.NET Core, EF Core, and Azure Blob Storage.",
        Contact = new OpenApiContact
        {
            Name = "Thabiso Nkosi",
            Email = "thabisonkosi241@gmail.com"
        }
    });

    // Support Session Token authentication via Swagger UI
    c.AddSecurityDefinition("SessionToken", new OpenApiSecurityScheme
    {
        Description = "Session Token authentication. Enter your token returned from /api/auth/login or /api/auth/register. Example: 'X-Session-Token: abc123def456'",
        Name = "X-Session-Token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "SessionToken"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// 6. Automatic Database Seeding on Startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await DbInitializer.SeedAsync(context, hasher);
}

// 7. Configure HTTP Request Pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "RaceDay API Documentation & Swagger UI";
});

app.UseCors("AllowAll");
app.UseStaticFiles();

// Custom Session Authentication Middleware
app.UseMiddleware<SessionAuthMiddleware>();

app.UseRouting();
app.MapControllers();

// Health check endpoint
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/api/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow, service = "RaceDay.API" }));

app.Run();
