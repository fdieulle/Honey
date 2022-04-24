using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Dojo
{
    internal class DojoDbContext : DbContext
    {
        public DbSet<NinjaEntity> Ninjas { get; set; }

        public DbSet<QueueEntity> Queues { get; set; }

        public DbSet<QueuedTaskEntity> Tasks { get; set; }

        public DojoDbContext(DbContextOptions<DojoDbContext> options) : base(options)
        {
            Database.EnsureCreated();
        }
    }
}
