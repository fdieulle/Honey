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
            var entity = new TaskEntity
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
            context.Add(entity);
            context.SaveChanges();
        }

        public void UpdateTask(RunningTask task)
        {
            using (var context = _factory.CreateDbContext())
            {
                var entity = context.Tasks.FirstOrDefault(p => p.Id == task.Id);
                if (entity == null) CreateTask(context, task);
                else
                {
                    var dto = task.ToDto();
                    entity.Pid = task.Pid;
                    entity.StartTime = dto.StartTime;
                    entity.Status = task.Status;
                    entity.EndTime = dto.EndTime;
                    entity.ProgressPercent = dto.ProgressPercent;
                    entity.ExpectedEndTime = dto.ExpectedEndTime;
                    entity.Message = dto.Message;
                    context.SaveChanges();
                }
            }
        }

        public void DeleteTask(Guid taskId)
        {
            using (var context = _factory.CreateDbContext())
            {
                var entity = context.Tasks.FirstOrDefault(p => p.Id == taskId);
                if (entity == null) return;

                context.Tasks.Remove(entity);
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
