using Infrastructure.Ninja;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Application;
using Application.Ninja;
using Infrastructure.Dojo;
using Application.Dojo;

namespace Infrastructure
{
    public static class Setup
    {
        public static void ConfigureNinja(this IServiceCollection services, IConfiguration configuration)
        {
            #region Database

            var workingFolder = configuration["WorkingFolder"] ?? ".";
            var dataFolder = Path.Combine(workingFolder, "data").CreateFolder();
            var dbPath = Path.Combine(dataFolder, "Ninja.db");

            services.AddEntityFrameworkSqlite()
                .AddDbContextFactory<NinjaDbContext>(options => options.UseSqlite($"Data Source=\"{dbPath}\""));
            services.AddSingleton<INinjaDb, NinjaDb>();

            #endregion

            services.AddSingleton<INinjaResourcesProvider, NinjaResourcesProvider>();
            services.AddSingleton<Application.Ninja.Ninja>();
        }

        public static void ConfigureDojo(this IServiceCollection services, IConfiguration configuration)
        {
            #region Database

            var workingFolder = configuration["WorkingFolder"] ?? ".";
            var dataFolder = Path.Combine(workingFolder, "data").CreateFolder();
            var dbPath = Path.Combine(dataFolder, "Dojo.db");

            services.AddEntityFrameworkSqlite()
                .AddDbContextFactory<DojoDbContext>(options => options.UseSqlite($"Data Source=\"{dbPath}\""));
            services.AddSingleton<IDojoDb, DojoDb>();

            #endregion

            services.AddSingleton<INinjaContainer, NinjaContainer>();
            services.AddSingleton<Application.Dojo.Dojo>();
            services.AddSingleton<QueueProvider>();
            services.AddSingleton<Shogun>();
        }
    }
}
