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

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            mb.Entity<NinjaEntity>()
                .HasKey(p => p.Address);
            mb.Entity<QueueEntity>()
                .HasKey(p => p.Name);
            mb.Entity<QueuedTaskEntity>()
                .HasKey(p => p.Id);
        }
    }
}
