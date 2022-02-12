using Microsoft.EntityFrameworkCore;
using Ninja.Services;
using System;
using System.Linq;

namespace Ninja.Model
{
    public static class ModelExtensions
    {
        public static void CreateJob(this IDbContextFactory<WorkerContext> factory, RunningJob job)
        {
            using (var context = factory.CreateDbContext())
            {
                var model = new JobModel
                {
                    Id = job.Id,
                    State = job.State,
                    Pid = job.Pid,
                    StartTime = job.StartTime,
                    EndTime = job.EndTime,
                };
                context.Add(model);
                context.SaveChanges();
            }
        }

        public static void UpdateJob(this IDbContextFactory<WorkerContext> factory, RunningJob job)
        {
            using (var context = factory.CreateDbContext())
            {
                var model = context.Jobs.FirstOrDefault(p => p.Id == job.Id);
                if (model == null) factory.CreateJob(job);
                else
                {
                    model.Pid = job.Pid;
                    model.StartTime = job.StartTime;
                    model.State = job.State;
                    model.EndTime = job.EndTime;
                    context.SaveChanges();
                }
            }
        }

        public static void DeleteJob(this IDbContextFactory<WorkerContext> factory, Guid jobId)
        {
            using (var context = factory.CreateDbContext())
            {
                var model = context.Jobs.FirstOrDefault(p => p.Id == jobId);
                if (model == null) return;

                context.Jobs.Remove(model);
                context.SaveChanges();
            }
        }
    }
}
