using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Sockets;
using ProjectApprovalSystem.Core.Interfaces;
using ProjectApprovalSystem.Core.Services;
using ProjectApprovalSystem.Data;
using ProjectApprovalSystem.Data.Context;
using ProjectApprovalSystem.Web.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// If no URL is provided by environment/launch profile, auto-select an available localhost port.
if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    var selectedPort = FindAvailablePort(5001, 15);
    builder.WebHost.UseUrls($"http://localhost:{selectedPort}");
}

// Configure Serilog logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/pas-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(8);
});

// Add Entity Framework Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<PasDbContext>(options =>
    options.UseSqlServer(connectionString,
        sqlServerOptions => sqlServerOptions.MigrationsAssembly("ProjectApprovalSystem.Data"))
);

// Register Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Services
builder.Services.AddScoped<IBlindMatchService, BlindMatchService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProposalService, ProposalService>();
builder.Services.AddScoped<IResearchAreaService, ResearchAreaService>();
builder.Services.AddScoped<ISupervisorExpertiseService, SupervisorExpertiseService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Project Approval System (PAS) API", 
        Version = "v1",
        Description = "API for blind matching project approval system"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapControllers();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PasDbContext>();
    try
    {
        // Apply migrations
        dbContext.Database.Migrate();
        await DemoDataSeeder.SeedAsync(dbContext);
        Log.Information("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while migrating the database");
        throw;
    }
}

app.Run();

int FindAvailablePort(int startPort, int maxAttempts)
{
    for (var offset = 0; offset < maxAttempts; offset++)
    {
        var candidatePort = startPort + offset;
        TcpListener? listener = null;

        try
        {
            listener = new TcpListener(IPAddress.Loopback, candidatePort);
            listener.Start();
            return candidatePort;
        }
        catch (SocketException)
        {
            // Port is in use, continue to next candidate.
        }
        finally
        {
            listener?.Stop();
        }
    }

    throw new InvalidOperationException("No available localhost port found for the web host.");
}

public partial class Program { }
