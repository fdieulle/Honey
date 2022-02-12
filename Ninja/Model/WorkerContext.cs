using Microsoft.EntityFrameworkCore;

namespace Ninja.Model
{
    public class WorkerContext : DbContext
    {
        public DbSet<JobModel> Jobs { get; set; }


        public WorkerContext(DbContextOptions<WorkerContext> options) : base(options) 
        {
            Database.EnsureCreated();
        }
    }
}
