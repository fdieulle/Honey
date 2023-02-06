﻿using Domain.Entities;
using Domain.Entities.Workflows;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Beehive
{
    internal class BeehiveDbContext : DbContext
    {
        public DbSet<BeeEntity> Bees { get; set; }

        public DbSet<ColonyEntity> Colonies { get; set; }

        public DbSet<RemoteTaskEntity> Tasks { get; set; }

        public DbSet<JobEntity> Jobs { get; set; }

        public DbSet<WorkflowEntity> Workflows { get; set; }

        public BeehiveDbContext(DbContextOptions<BeehiveDbContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            mb.Entity<BeeEntity>()
                .HasKey(p => p.Address);
            mb.Entity<ColonyEntity>()
                .HasKey(p => p.Name);
            mb.Entity<RemoteTaskEntity>()
                .HasKey(p => p.Id);

            mb.Entity<JobEntity>()
                .HasKey(p => p.Id);
            mb.Entity<SingleTaskJobEntity>()
                .HasBaseType<JobEntity>();
            mb.Entity<ManyJobsEntity>()
                .HasBaseType<JobEntity>();
            mb.Entity<WorkflowEntity>()
                .HasKey(p => p.Id);
        }
    }
}
