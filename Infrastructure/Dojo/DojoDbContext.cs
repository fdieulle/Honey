using Domain.Entities;
using Domain.Entities.Pipelines;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Dojo
{
    internal class DojoDbContext : DbContext
    {
        public DbSet<NinjaEntity> Ninjas { get; set; }

        public DbSet<QueueEntity> Queues { get; set; }

        public DbSet<RemoteTaskEntity> Tasks { get; set; }

        public DbSet<JobEntity> Jobs { get; set; }

        public DbSet<PipelineEntity> Pipelines { get; set; }

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
            mb.Entity<RemoteTaskEntity>()
                .HasKey(p => p.Id);

            mb.Entity<JobEntity>()
                .HasKey(p => p.Id);
            mb.Entity<SingleTaskJobEntity>()
                .HasBaseType<JobEntity>();
            mb.Entity<ParallelJobsEntity>()
                .HasBaseType<JobEntity>();
            mb.Entity<LinkedJobsEntity>()
                .HasBaseType<JobEntity>();
            mb.Entity<PipelineEntity>()
                .HasKey(p => p.Id);
        }
    }
}
