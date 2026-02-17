using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReportVisualizer.App_Start;
using ReportVisualizer.ReportViewer.ReportDataExtraction; // Added for RdlDataExtractor
using ReportVisualizer.ReportViewer.ReportDataExecution; // Added for SqlDatasetExecutor
using ReportVisualizer.ReportViewer; // Added for ReportRenderer

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5005);
});

// Add services to the container
builder.Services.AddRazorPages();

// Register custom services for dependency injection
builder.Services.AddScoped<RdlDataExtractor>();
builder.Services.AddScoped<SqlDatasetExecutor>();
builder.Services.AddScoped<ReportRenderer>();

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
    var url = "http://localhost:5005";

        await Task.Run(async () =>
        {
            await Task.Delay(1500);
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }
        });

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Application terminated unexpectedly: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}