using EmployeeAdminPortal.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

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
        Console.WriteLine("🔗 Configured for Azure SQL Server");
    }
    else
    {
        // Use MySQL for local development
        var localConnection = builder.Configuration.GetConnectionString("LocalConnection") ?? connectionString ?? "";
        options.UseMySql(localConnection, MySqlServerVersion.LatestSupportedServerVersion,
            mySqlOptions => mySqlOptions.EnableRetryOnFailure());
        Console.WriteLine("🔗 Configured for Local MySQL");
    }
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Test database connection on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        Console.WriteLine("Testing database connection...");
        await context.Database.CanConnectAsync();
        Console.WriteLine("✅ Database connection successful!");
        Console.WriteLine($"Connected to: {context.Database.GetDbConnection().Database}");
        Console.WriteLine($"Provider: {context.Database.ProviderName}");
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ Database connection failed!");
        Console.WriteLine($"Error: {ex.Message}");
    }
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();