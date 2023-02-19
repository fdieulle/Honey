using Infrastructure.Bee;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Application;
using Application.Bee;
using Infrastructure.Colony;
using Application.Colony;
using Application.Honey;
using log4net.Config;

namespace Infrastructure
{
    public static class Setup
    {
        public static void ConfigureBee(this IServiceCollection services, IConfiguration configuration)
        {
            XmlConfigurator.Configure(new FileInfo(configuration["Log4netConfiguration"]));

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

        public static void ConfigureColony(this IServiceCollection services, IConfiguration configuration)
        {
            XmlConfigurator.Configure(new FileInfo(configuration["Log4netConfiguration"]));

            #region Database

            var workingFolder = configuration["WorkingFolder"] ?? ".";
            var dataFolder = Path.Combine(workingFolder, "data").CreateFolder();
            var dbPath = Path.Combine(dataFolder, "Colony.db");

            services.AddEntityFrameworkSqlite()
                .AddDbContextFactory<ColonyDbContext>(options => options.UseSqlite($"Data Source=\"{dbPath}\""));
            services.AddSingleton<IColonyDb, ColonyDb>();

            #endregion

            services.AddSingleton<IDispatcherFactory>(new DispatcherFactory());
            services.AddSingleton<ITimer>(new ThreadedTimer(3000));
            services.AddSingleton<IBeeFactory, BeeProxyFactory>();
            var taskTracker = new TaskTracker();
            services.AddSingleton<ITaskTracker>(taskTracker);
            services.AddSingleton(taskTracker);
            services.AddSingleton<BeeKeeper>();
            services.AddSingleton<BeehiveProvider>();
            services.AddSingleton<Application.Colony.Colony>();
            services.AddSingleton<Poller>();

            services.AddSingleton<WorkflowRepository>();
        }
    }
}
