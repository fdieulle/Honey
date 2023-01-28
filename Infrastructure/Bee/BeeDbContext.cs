using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Infrastructure.Bee
{
    internal class BeeDbContext : DbContext
    {
        public DbSet<TaskEntity> Tasks { get; set; }

        public BeeDbContext(DbContextOptions<BeeDbContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            mb.Entity<TaskEntity>()
                .HasKey(p => p.Id);
        }
    }
}
