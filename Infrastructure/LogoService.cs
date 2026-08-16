using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;

namespace ReportVisualizer.Infrastructure
{
    public class LogoOptions
    {
        public bool Client { get; set; }
        public bool App { get; set; }
        public bool ReplaceApp { get; set; }
        public bool ReplaceClient { get; set; }
    }

    public class LogoResolutionResult
    {
        public bool HasLogo { get; set; }
        public string RelativeUrl { get; set; }
        public string AbsoluteFilePath { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
    }

    public class LogoService
    {
        private readonly IConfiguration _configuration;
        private readonly LogoOptions _options;
        private readonly string _logoFolder;

        private static readonly string[] SupportedImageExts = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".ico", ".webp", ".svg" };

        public LogoService(IConfiguration configuration)
        {
            _configuration = configuration;
            _options = new LogoOptions();
            configuration.GetSection("logo").Bind(_options);
            _logoFolder = Path.Combine(Directory.GetCurrentDirectory(), "logo");
        }

        public LogoOptions Options => _options;
        public string LogoFolder => _logoFolder;

        public bool EnsureLogoFolder()
        {
            try
            {
                if (!Directory.Exists(_logoFolder))
                {
                    Directory.CreateDirectory(_logoFolder);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ensuring logo folder exists: {ex.Message}");
                return false;
            }
        }

        private string FindLogoFile(string baseName)
        {
            if (!Directory.Exists(_logoFolder)) return null;
            foreach (var ext in SupportedImageExts)
            {
                var candidate = Path.Combine(_logoFolder, baseName + ext);
                if (File.Exists(candidate)) return candidate;
            }
            return null;
        }

        public LogoResolutionResult ResolveClientLogo()
        {
            var result = new LogoResolutionResult();
            if (!_options.Client) return result;

            string baseName = "client";
            if (_options.ReplaceClient)
            {
                var appFile = FindLogoFile("app");
                if (appFile != null) baseName = "app";
            }

            var filePath = FindLogoFile(baseName);
            if (filePath == null) return result;

            result.HasLogo = true;
            result.AbsoluteFilePath = filePath;
            result.FileName = Path.GetFileName(filePath);
            result.RelativeUrl = "/logo/" + Uri.EscapeDataString(result.FileName);
            result.ContentType = InferContentType(filePath);
            return result;
        }

        public LogoResolutionResult ResolveAppLogo()
        {
            var result = new LogoResolutionResult();
            if (!_options.App) return result;

            string baseName = "app";
            if (_options.ReplaceApp)
            {
                var clientFile = FindLogoFile("client");
                if (clientFile != null) baseName = "client";
            }

            var filePath = FindLogoFile(baseName);
            if (filePath == null) return result;

            result.HasLogo = true;
            result.AbsoluteFilePath = filePath;
            result.FileName = Path.GetFileName(filePath);
            result.RelativeUrl = "/logo/" + Uri.EscapeDataString(result.FileName);
            result.ContentType = InferContentType(filePath);
            return result;
        }

        public static string InferContentType(string filePath)
        {
            var ext = (Path.GetExtension(filePath) ?? "").ToLowerInvariant();
            return ext switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".ico" => "image/x-icon",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };
        }
    }

    public static class LogoServiceExtensions
    {
        public static IServiceCollection AddLogoService(this IServiceCollection services)
        {
            services.AddScoped<LogoService>();
            return services;
        }

        public static IApplicationBuilder UseLogoStaticFiles(this IApplicationBuilder app)
        {
            var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
            var logoDir = Path.Combine(Directory.GetCurrentDirectory(), "logo");
            if (!Directory.Exists(logoDir))
            {
                try { Directory.CreateDirectory(logoDir); } catch { }
            }
            if (Directory.Exists(logoDir))
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(logoDir),
                    RequestPath = "/logo",
                    ServeUnknownFileTypes = false
                });
            }
            return app;
        }
    }
}
