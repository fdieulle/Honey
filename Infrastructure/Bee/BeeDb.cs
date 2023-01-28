using Application.Bee;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Bee
{
    internal class BeeDb : IBeeDb
    {
        private readonly IDbContextFactory<BeeDbContext> _factory;
        private readonly IBeeResourcesProvider _beeResourcesProvider;

        public BeeDb(IDbContextFactory<BeeDbContext> factory, IBeeResourcesProvider beeResourcesProvider)
        {
            _factory = factory;
            _beeResourcesProvider = beeResourcesProvider;
        }        

        public void CreateTask(RunningTask task)
        {
            using (var context = _factory.CreateDbContext())
                CreateTask(context, task);
        }

        private void CreateTask(BeeDbContext context, RunningTask task)
        {
            var dto = task.ToDto();
            var model = new TaskEntity
            {
                Id = task.Id,
                Command = task.Command,
                Arguments = task.Arguments,
                WorkingFolder = task.WorkingFolder,
                Status = task.Status,
                Pid = task.Pid,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                ProgressPercent = dto.ProgressPercent,
                ExpectedEndTime = dto.ExpectedEndTime,
                Message = dto.Message,
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
                    var dto = task.ToDto();
                    model.Pid = task.Pid;
                    model.StartTime = dto.StartTime;
                    model.Status = task.Status;
                    model.EndTime = dto.EndTime;
                    model.ProgressPercent = dto.ProgressPercent;
                    model.ExpectedEndTime = dto.ExpectedEndTime;
                    model.Message = dto.Message;
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
                    result.Add(new RunningTask(_beeResourcesProvider.GetBaseUri(), task));
            }
            return result;
        }
    }
}
