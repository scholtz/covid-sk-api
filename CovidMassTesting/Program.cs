using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using NLog.Web;
using System;
using System.Resources;

[assembly: NeutralResourcesLanguage("en")]
[assembly: System.Reflection.AssemblyVersionAttribute("1.0.*")]
[assembly: System.Reflection.AssemblyCompanyAttribute("Scholtz & Company, jsa")]

namespace CovidMassTesting
{
    /// <summary>
    /// App
    /// </summary>
    public static class Program
    {

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            var logger = NLog.Web.NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            try
            {
                logger.Debug($"init main function {string.Join(';', args)}");
                Startup.Args = args;
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in init: " + ex.Message + ex.InnerException?.Message);
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
        }
        /// <summary>
        /// Creates web
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .UseNLog();
        }
    }
}
