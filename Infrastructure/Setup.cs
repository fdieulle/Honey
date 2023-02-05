using Infrastructure.Bee;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Application;
using Application.Bee;
using Infrastructure.Beehive;
using Application.Beehive;
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

        public static void ConfigureBeehive(this IServiceCollection services, IConfiguration configuration)
        {
            #region Database

            var workingFolder = configuration["WorkingFolder"] ?? ".";
            var dataFolder = Path.Combine(workingFolder, "data").CreateFolder();
            var dbPath = Path.Combine(dataFolder, "Beehive.db");

            services.AddEntityFrameworkSqlite()
                .AddDbContextFactory<BeehiveDbContext>(options => options.UseSqlite($"Data Source=\"{dbPath}\""));
            services.AddSingleton<IBeehiveDb, BeehiveDb>();

            #endregion

            services.AddSingleton<ITimer>(new ThreadedTimer(3000));
            services.AddSingleton<IBeeFactory, BeeProxyFactory>();
            var taskTracker = new TaskTracker();
            services.AddSingleton<ITaskTracker>(taskTracker);
            services.AddSingleton(taskTracker);
            services.AddSingleton<Application.Beehive.BeeKeeper>();
            services.AddSingleton<QueueProvider>();
            services.AddSingleton<Beehive>();
            services.AddSingleton<Poller>();

            services.AddSingleton<WorkflowRepository>();
        }
    }
}
