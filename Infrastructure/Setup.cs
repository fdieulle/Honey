using Infrastructure.Bee;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Application;
using Application.Bee;
using Infrastructure.Dojo;
using Application.Dojo;
using Application.Honey;

namespace Infrastructure
{
    public static class Setup
    {
        public static void ConfigureBee(this IServiceCollection services, IConfiguration configuration)
        {
            #region Database

            var workingFolder = configuration["WorkingFolder"] ?? ".";
            var dataFolder = Path.Combine(workingFolder, "data").CreateFolder();
            var dbPath = Path.Combine(dataFolder, "Bee.db");

            services.AddEntityFrameworkSqlite()
                .AddDbContextFactory<BeeDbContext>(options => options.UseSqlite($"Data Source=\"{dbPath}\""));
            services.AddSingleton<IBeeDb, BeeDb>();

            #endregion

            services.AddSingleton<IBeeResourcesProvider, BeeResourcesProvider>();
            services.AddSingleton<Application.Bee.Bee>();
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

            services.AddSingleton<ITimer>(new ThreadedTimer(3000));
            services.AddSingleton<IBeeFactory, BeeProxyFactory>();
            var taskTracker = new TaskTracker();
            services.AddSingleton<ITaskTracker>(taskTracker);
            services.AddSingleton(taskTracker);
            services.AddSingleton<Application.Dojo.Dojo>();
            services.AddSingleton<QueueProvider>();
            services.AddSingleton<Colony>();
            services.AddSingleton<Poller>();

            services.AddSingleton<WorkflowRepository>();
        }
    }
}
