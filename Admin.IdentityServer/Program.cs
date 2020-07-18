using Admin.IdentityServer.Configs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Web;
using System;
using System.Threading.Tasks;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Admin.IdentityServer
{
    public class Program
    {
        private static AppSettings _appSettings { get; } = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", false)
#if DEBUG
                .AddJsonFile("appsettings.Development.json", true)
#endif
                .Build().Get<AppSettings>();

        public static async Task<int> Main(string[] args)
        {
            var logger = LogManager.GetCurrentClassLogger();
            try
            {
                Console.WriteLine("Starting host...");
                var host = CreateHostBuilder(args).Build();
                Console.WriteLine($"{string.Join("\r\n", _appSettings.Urls)}\r\n");
                await host.RunAsync();
                //CreateHostBuilder(args).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Stopped program because of exception");
                return 1;
            }
            finally
            {
                LogManager.Shutdown();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder
                //.UseEnvironment(Environments.Production)
                .UseStartup<Startup>().UseUrls(_appSettings.Urls);
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Trace);
            })
            .UseNLog();
    }
}