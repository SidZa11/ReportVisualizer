using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using ReportVisualizer.App_Start;
using ReportVisualizer.ReportViewer.ReportDataExtraction; // Added for RdlDataExtractor
using ReportVisualizer.ReportViewer.ReportDataExecution; // Added for SqlDatasetExecutor
using ReportVisualizer.ReportViewer; // Added for ReportRenderer

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5005);
});

builder.Host.UseWindowsService(
    options =>
    {
        options.ServiceName = "ReportVisualizer";
    }
);
// builder.Logging.ClearProviders();
// builder.Logging.AddEventLog(settings =>
// {
//     settings.LogName = "ReportVisualizer";
// });
// builder.Logging.AddFile("Logs/log-{Date}.txt");

// Add session services
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


// Add services to the container
builder.Services.AddRazorPages();

// Register custom services for dependency injection
builder.Services.AddScoped<RdlDataExtractor>();
builder.Services.AddScoped<SqlDatasetExecutor>();
builder.Services.AddScoped<ReportRenderer>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHostedService<LicenseWatchdogService>();


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

app.MapGet("/", context =>
{
    context.Response.Redirect("/ReportViewer");
    return Task.CompletedTask;
});
app.MapRazorPages();

app.UseAuthorization();

app.UseSession();

// Track the browser process we launch so we can close it on shutdown
System.Diagnostics.Process? __launchedBrowserProcess = null;

var __lifetime = app.Services.GetRequiredService<Microsoft.Extensions.Hosting.IHostApplicationLifetime>();
__lifetime.ApplicationStopping.Register(() =>
{
    try
    {
        if (__launchedBrowserProcess != null && !__launchedBrowserProcess.HasExited)
        {
            try { __launchedBrowserProcess.CloseMainWindow(); } catch { }
            try { __launchedBrowserProcess.WaitForExit(2000); } catch { }
            if (!__launchedBrowserProcess.HasExited)
            {
                try { __launchedBrowserProcess.Kill(true); } catch { }
            }
        }
    }
    catch { }
});

app.MapPost("/_app/close", (Microsoft.AspNetCore.Http.HttpContext ctx, Microsoft.Extensions.Hosting.IHostApplicationLifetime lifetime) =>
{
    var remote = ctx.Connection.RemoteIpAddress;
    if (remote != null && !System.Net.IPAddress.IsLoopback(remote))
    {
        return Results.Unauthorized();
    }

    _ = System.Threading.Tasks.Task.Run(() =>
    {
        System.Threading.Thread.Sleep(200);
        lifetime.StopApplication();
    });

    return Results.Ok(new { success = true });
});


// Database initialization is now handled through the ConfigureServices method
// No need for additional initialization here

try
{
    var url = "http://localhost:5005";

    if (!LicenseManager.Validate(out string msg))
    {
        Console.WriteLine(msg);
        Console.WriteLine($"License Error: {msg}");
        return;
    }

    await Task.Run(async () =>
    {
        await Task.Delay(1500);
        try
        {
            var edgeCandidates = new[]
            {
                System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
                System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe")
            };
            string edgeExe = null;
            foreach (var p in edgeCandidates)
            {
                if (System.IO.File.Exists(p))
                {
                    edgeExe = p;
                    break;
                }
            }
            if (edgeExe != null)
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = edgeExe,
                    Arguments = $"--kiosk \"{url}\" --edge-kiosk-type=fullscreen",
                    UseShellExecute = false
                };
                __launchedBrowserProcess = System.Diagnostics.Process.Start(psi);
                return;
            }

            var chromeCandidates = new[]
            {
                System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
                System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe")
            };
            string chromeExe = null;
            foreach (var p in chromeCandidates)
            {
                if (System.IO.File.Exists(p))
                {
                    chromeExe = p;
                    break;
                }
            }
            if (chromeExe != null)
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = chromeExe,
                    Arguments = $"--kiosk \"{url}\"",
                    UseShellExecute = false
                };
                __launchedBrowserProcess = System.Diagnostics.Process.Start(psi);
                return;
            }

            var fallbackPsi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            __launchedBrowserProcess = System.Diagnostics.Process.Start(fallbackPsi);
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
