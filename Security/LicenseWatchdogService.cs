using Microsoft.Extensions.Hosting;

public class LicenseWatchdogService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); 
        // wait until app fully starts

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!LicenseManager.Validate(out string msg))
                {
                    Console.WriteLine("LICENSE VIOLATION: " + msg);

                    // graceful shutdown
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("License check error: " + ex.Message);
                Environment.Exit(0);
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
