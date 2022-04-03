using Application.Ninja;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Ninja
{
    internal class NinjaDb : INinjaDb
    {
        private readonly IDbContextFactory<NinjaDbContext> _factory;

        public NinjaDb(IDbContextFactory<NinjaDbContext> factory)
        {
            _factory = factory;
        }        

        public void CreateTask(RunningTask task)
        {
            using (var context = _factory.CreateDbContext())
                CreateTask(context, task);
        }

        private void CreateTask(NinjaDbContext context, RunningTask task)
        {
            var model = new TaskEntity
            {
                Id = task.Id,
                Command = task.Command,
                Arguments = task.Arguments,
                Status = task.Status,
                Pid = task.Pid,
                StartTime = task.StartTime,
                EndTime = task.EndTime,
            };
            context.Add(model);
            context.SaveChanges();
        }

        public void UpdateTask(RunningTask task)
        {
            using (var context = _factory.CreateDbContext())
            {
                var model = context.Tasks.FirstOrDefault(p => p.Id == task.Id);
                if (model == null) CreateTask(context, task);
                else
                {
                    model.Pid = task.Pid;
                    model.StartTime = task.StartTime;
                    model.Status = task.Status;
                    model.EndTime = task.EndTime;
                    context.SaveChanges();
                }
            }
        }

        public void DeleteTask(Guid taskId)
        {
            using (var context = _factory.CreateDbContext())
            {
                var model = context.Tasks.FirstOrDefault(p => p.Id == taskId);
                if (model == null) return;

                context.Tasks.Remove(model);
                context.SaveChanges();
            }
        }

        public IEnumerable<RunningTask> FetchTasks()
        {
            var result = new List<RunningTask>();
            using (var context = _factory.CreateDbContext())
            {
                foreach (var task in context.Tasks)
                    result.Add(new RunningTask(task));
            }
            return result;
        }
    }
}
