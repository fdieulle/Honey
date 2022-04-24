using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Infrastructure.Ninja
{
    internal class NinjaDbContext : DbContext
    {
        public DbSet<TaskEntity> Tasks { get; set; }

        public NinjaDbContext(DbContextOptions<NinjaDbContext> options) : base(options)
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
