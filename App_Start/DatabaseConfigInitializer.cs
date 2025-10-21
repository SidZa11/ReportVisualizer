using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReportVisualizer.Utilities;

namespace ReportVisualizer.App_Start
{
    /// <summary>
    /// Initializes database configuration on application startup
    /// </summary>
    public static class DatabaseConfigInitializer
    {
        /// <summary>
        /// Configures services for database initialization
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration</param>
        public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Initialize configuration manager
            ConfigurationManager.Initialize(configuration);
            
            // Register database services
            services.AddSingleton<GlobalConnectionHandler>();
            
            Logger.Log("Database services configured");
        }

        /// <summary>
        /// Initializes the database connection
        /// </summary>
        /// <param name="app">Application builder</param>
        public static void Initialize(IApplicationBuilder app)
        {
            try
            {
                // Initialize global connection handler
                GlobalConnectionHandler.Initialize();
                
                Logger.Log("Database connection initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error initializing database connection: {ex.Message}");
                // Allow application to continue, connection will be retried when needed
            }
        }
    }
}