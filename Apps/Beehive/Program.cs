using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Beehive
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService(options => options.ServiceName = "Colony")
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                        .UseUrls("http://*:5000;https://*:5001"); ;
                });
    }
}
