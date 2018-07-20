using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using NLog;
using NLog.Web;

namespace AppBuilder {
    public class Program {
        public static void Main(string[] args) {
            var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            try {
                CreateWebHostBuilder(args).Build().Run();
            } catch (Exception ex) {
                //NLog: catch setup errors
                logger.Error(ex, "Stopped program because of exception");
            } finally {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                LogManager.Shutdown();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) {
            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseNLog().UseUrls("http://*:5001");
        }
    }
}