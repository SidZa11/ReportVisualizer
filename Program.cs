using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReportVisualizer.App_Start;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();

// Initialize database configuration
// DatabaseConfigInitializer.ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();

app.UseAuthorization();

// Database initialization is now handled through the ConfigureServices method
// No need for additional initialization here

try
{
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Application terminated unexpectedly: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}