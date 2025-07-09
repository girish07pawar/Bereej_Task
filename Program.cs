using EmployeeAdminPortal.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin() // OR use .WithOrigins("http://192.168.56.1:8080") for stricter policy
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


// Configure Database Context based on environment
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    // Check if we're in Azure (production) or local development
    if (builder.Environment.IsProduction() || (!string.IsNullOrEmpty(connectionString) && connectionString.Contains("database.windows.net")))
    {
        // Use SQL Server for Azure
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });
        Console.WriteLine(" Configured for Azure SQL Server");
    }
    else
    {
        // Use MySQL for local development
        var localConnection = builder.Configuration.GetConnectionString("LocalConnection") ?? connectionString ?? "";
        options.UseMySql(localConnection, MySqlServerVersion.LatestSupportedServerVersion,
            mySqlOptions => mySqlOptions.EnableRetryOnFailure());
        Console.WriteLine(" Configured for Local MySQL");
    }
});

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Employee Admin Portal API",
        Version = "v1",
        Description = "API for managing employees - Test your endpoints here!"
    });
});

var app = builder.Build();

// Test database connection on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        Console.WriteLine("Testing database connection...");
        await context.Database.CanConnectAsync();
        Console.WriteLine(" Database connection successful!");
        Console.WriteLine($"Connected to: {context.Database.GetDbConnection().Database}");
        Console.WriteLine($"Provider: {context.Database.ProviderName}");
    }
    catch (Exception ex)
    {
        Console.WriteLine(" Database connection failed!");
        Console.WriteLine($"Error: {ex.Message}");
    }
}

// Configure pipeline - Enable Swagger in all environments for API testing
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee Admin Portal API v1");
    c.RoutePrefix = "swagger"; // This makes Swagger available at /swagger
    c.DocumentTitle = "Employee Admin Portal API";
});

// Add a root route that redirects to Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

// Add a simple API info endpoint
app.MapGet("/api", () => new
{
    message = "Welcome to Employee Admin Portal API! 🎉",
    description = "This is an API for managing employees. Use /swagger to test the endpoints.",
    version = "1.0.0",
    endpoints = new[]
    {
        "GET /api/employees - Get all employees",
        "POST /api/employees - Create a new employee",
        "GET /api/employees/highest-salary - Get employee with highest salary",
        "POST /api/employees/by-name - Get employee by name",
        "DELETE /api/employees?id={guid} - Delete an employee"
    },
    swaggerUrl = "/swagger"
});
app.UseCors("AllowAll");
app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();

app.Run();