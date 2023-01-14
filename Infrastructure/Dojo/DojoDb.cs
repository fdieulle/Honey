using Application.Dojo;
using Domain.Dtos;
using Domain.Dtos.Pipelines;
using Domain.Entities;
using Domain.Entities.Pipelines;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Dojo
{
    internal class DojoDb : IDojoDb
    {
        private readonly CrudDbTable<DojoDbContext, NinjaEntity, string, NinjaDto> _crudNinjas;
        private readonly CrudDbTable<DojoDbContext, QueueEntity, string, QueueDto> _crudQueues;
        private readonly CrudDbTable<DojoDbContext, RemoteTaskEntity, Guid, RemoteTaskDto> _crudTasks;
        private readonly CrudDbTable<DojoDbContext, JobEntity, Guid, JobDto> _crudJobs;
        private readonly CrudDbTable<DojoDbContext, PipelineEntity, Guid, PipelineDto> _crudPipelines;
        public DojoDb(IDbContextFactory<DojoDbContext> factory)
        {
            _crudNinjas = new CrudDbTable<DojoDbContext, NinjaEntity, string, NinjaDto>(
                factory, c => c.Ninjas, p => p.Address, d => d.ToEntity(), (d, e) => e.Update(d), p => p.Address, e => e.ToDto());
            _crudQueues = new CrudDbTable<DojoDbContext, QueueEntity, string, QueueDto>(
                factory, c => c.Queues, p => p.Name, d => d.ToEntity(), (d, e) => e.Update(d), p => p.Name, e => e.ToDto());
            _crudTasks = new CrudDbTable<DojoDbContext, RemoteTaskEntity, Guid, RemoteTaskDto>(
                factory, c => c.Tasks, p => p.Id, d => d.ToEntity(), (d, e) => e.Update(d), p => p.Id, e => e.ToDto());
            _crudJobs = new CrudDbTable<DojoDbContext, JobEntity, Guid, JobDto>(
                factory, c => c.Jobs, p => p.Id, d => d.ToEntity(), (d, e) => e.Update(d), p => p.Id, e => e.ToDto());
            _crudPipelines = new CrudDbTable<DojoDbContext, PipelineEntity, Guid, PipelineDto>(
                factory, c => c.Pipelines, p => p.Id, d => d.ToEntity(), (d, e) => e.Update(d), p => p.Id, e => e.ToDto());
        }

        #region Ninja

        public IEnumerable<NinjaDto> FetchNinjas() => _crudNinjas.Fetch();

        public void CreateNinja(NinjaDto ninja) => _crudNinjas.Create(ninja);

        public void DeleteNinja(string address) => _crudNinjas.Delete(address);

        #endregion

        #region Queues

        public IEnumerable<QueueDto> FetchQueues() => _crudQueues.Fetch();

        public void CreateQueue(QueueDto queue) => _crudQueues.Create(queue);

        public void UpdateQueue(QueueDto queue) => _crudQueues.Update(queue);

        public void DeleteQueue(string name) => _crudQueues.Delete(name);

        #endregion

        #region Remote tasks

        public IEnumerable<RemoteTaskDto> FetchTasks() => _crudTasks.Fetch();

        public void CreateTask(RemoteTaskDto task) => _crudTasks.Create(task);

        public void UpdateTask(RemoteTaskDto task) => _crudTasks.Update(task);

        public void DeleteTask(Guid id) => _crudTasks.Delete(id);

        #endregion

        #region Jobs

        public IEnumerable<JobDto> FetchJobs() => _crudJobs.Fetch();

        public JobDto FetchJob(Guid id) => _crudJobs.Fetch().FirstOrDefault(p => p.Id == id);

        public void CreateJob(JobDto job) => _crudJobs.Create(job);

        public void UpdateJob(JobDto job) => _crudJobs.Update(job);

        public void DeleteJob(Guid id) => _crudJobs.Delete(id);

        #endregion

        #region Pipelines

        public IEnumerable<PipelineDto> FetchPipelines() => _crudPipelines.Fetch();

        public PipelineDto FetchPipeline(Guid id) => _crudPipelines.Fetch().FirstOrDefault(p => p.Id == id);

        public void CreatePipeline(PipelineDto pipeline) => _crudPipelines.Create(pipeline);

        public void UpdatePipeline(PipelineDto pipeline) => _crudPipelines.Update(pipeline);

        public void DeletePipeline(Guid id) => _crudJobs.Delete(id);

        #endregion
    }

    // Maybe overkill
    public class CrudDbTable<TContext, TEntity, TKey, TDto>
        where TContext : DbContext
        where TEntity : class
        where TKey : IComparable<TKey>
    {
        private readonly IDbContextFactory<TContext> _factory;
        private readonly Func<TContext, DbSet<TEntity>> _getTable;
        private readonly Func<TEntity, TKey> _getKey;
        private readonly Func<TDto, TEntity> _toEntity;
        private readonly Action<TDto, TEntity> _updateEntity;
        private readonly Func<TDto, TKey> _getKeyFromDto;
        private readonly Func<TEntity, TDto> _toDto;

        public CrudDbTable(
            IDbContextFactory<TContext> factory,
            Func<TContext, DbSet<TEntity>> getTable,
            Func<TEntity, TKey> getKey,
            Func<TDto, TEntity> toEntity,
            Action<TDto, TEntity> updateEntity,
            Func<TDto, TKey> getKeyFromDto,
            Func<TEntity, TDto> toDto)
        {
            _factory = factory;
            _getTable = getTable;
            _getKey = getKey;
            _toEntity = toEntity;
            _updateEntity = updateEntity;
            _getKeyFromDto = getKeyFromDto;
            _toDto = toDto;
        }

        public IEnumerable<TDto> Fetch()
        {
            var result = new List<TDto>();
            using (var context = _factory.CreateDbContext())
                foreach (var entity in _getTable(context).AsEnumerable())
                    result.Add(_toDto(entity));
            return result;
        }

        public void Create(TDto dto)
        {
            using (var context = _factory.CreateDbContext())
                Create(context, dto);
        }

        private void Create(TContext context, TDto queue)
        {
            context.Add(_toEntity(queue));
            context.SaveChanges();
        }

        public void Update(TDto dto)
        {
            var key = _getKeyFromDto(dto);
            using (var context = _factory.CreateDbContext())
            {
                var entity = _getTable(context).AsEnumerable().FirstOrDefault(p => _getKey(p).CompareTo(key) == 0);

                if (entity == null) Create(context, dto);
                else
                {
                    _updateEntity(dto, entity);
                    context.SaveChanges();
                }
            }
        }

        public void Delete(TKey key)
        {
            using (var context = _factory.CreateDbContext())
            {
                var table = _getTable(context);
                var entity = table.AsEnumerable().FirstOrDefault(p => _getKey(p).CompareTo(key) == 0);
                if (entity == null) return;

                table.Remove(entity);
                context.SaveChanges();
            }
        }
    }

    public static class EntityDtoMapper
    {
        #region Ninjas

        public static NinjaEntity ToEntity(this NinjaDto dto) => new NinjaEntity { Address = dto.Address };
        public static void Update(this NinjaEntity entity, NinjaDto dto) { }
        public static NinjaDto ToDto(this NinjaEntity entity) => new NinjaDto { Address = entity.Address };

        #endregion

        #region Queues
        
        private const char SEP = ';';
        public static QueueEntity ToEntity(this QueueDto dto)
        {
            var entity = new QueueEntity { Name = dto.Name };
            entity.Update(dto);
            return entity;
        }
        public static void Update(this QueueEntity entity, QueueDto dto)
        {
            entity.MaxParallelTasks = dto.MaxParallelTasks;
            entity.Ninjas = dto.Ninjas != null ? string.Join(SEP, dto.Ninjas) : null;
        }
        public static QueueDto ToDto(this QueueEntity entity)
        {
            return new QueueDto
            {
                Name = entity.Name,
                MaxParallelTasks = entity.MaxParallelTasks,
                Ninjas = entity.Ninjas != null ? entity.Ninjas.Split(SEP) : null
            };
        }

        #endregion

        #region RemoteTasks

        public static RemoteTaskEntity ToEntity(this RemoteTaskDto dto)
        {
            var entity = new RemoteTaskEntity { Id = dto.Id };
            entity.Update(dto);
            return entity;
        }
        public static void Update(this RemoteTaskEntity entity, RemoteTaskDto dto)
        {
            entity.Name = dto.Name;
            entity.Status = dto.Status;
            entity.QueueName = dto.QueueName;
            entity.NinjaAddress = dto.NinjaAddress;
            entity.Order = dto.Order;
            entity.Command = dto.StartTask.Command;
            entity.Arguments = dto.StartTask.Arguments;
            entity.NbCores = dto.StartTask.NbCores;
            entity.NinjaTaskId = dto.NinjaState.Id;
        }
        public static RemoteTaskDto ToDto(this RemoteTaskEntity entity)
        {
            return new RemoteTaskDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Status = entity.Status,
                QueueName = entity.QueueName,
                NinjaAddress = entity.NinjaAddress,
                Order = entity.Order,
                StartTask = new TaskParameters
                {
                    Command = entity.Command,
                    Arguments = entity.Arguments,
                    NbCores = entity.NbCores
                },
                NinjaState = new TaskDto
                {
                    Id = entity.Id,
                    Status = Domain.Dtos.TaskStatus.Pending
                }
            };
        }

        #endregion

        #region Jobs

        public static JobEntity ToEntity(this JobDto dto)
        {
            if (dto is SingleTaskJobDto sj)
                return sj.ToEntity();
            else if (dto is ManyJobsDto mj)
                return mj.ToEntity();
            else
                throw new InvalidOperationException($"Job dto: {dto?.GetType()} is not supported");
        }
        
        public static void Update(this JobEntity entity, JobDto dto) 
        {
            if (entity is SingleTaskJobEntity sje && dto is SingleTaskJobDto sjd)
                sje.Update(sjd);
            else if (entity is ManyJobsEntity mje && dto is ManyJobsDto mjd)
                mje.Update(mjd);
            else
                throw new InvalidOperationException($"Job entity: {entity?.GetType()} with dto: {dto?.GetType()} is not supported");
        }
        public static JobDto ToDto(this JobEntity entity)
        {
            if (entity is SingleTaskJobEntity sj)
                return sj.ToDto();
            else if (entity is ManyJobsEntity mj)
                return mj.ToDto();
            else
                throw new InvalidOperationException($"Job entity: {entity?.GetType()} is not supported");
        }

        public static SingleTaskJobEntity ToEntity(this SingleTaskJobDto dto)
        {
            var entity = new SingleTaskJobEntity { Id = dto.Id };
            entity.Update(dto);
            return entity;
        }

        public static void Update(this SingleTaskJobEntity entity, SingleTaskJobDto dto)
        {
            entity.Name = dto.Name;
            entity.Status = dto.Status;
            entity.TaskId = dto.TaskId;
            entity.Command = dto.Parameters.Command;
            entity.Arguments = dto.Parameters.Arguments;
            entity.NbCores = dto.Parameters.NbCores;
        }

        public static SingleTaskJobDto ToDto(this SingleTaskJobEntity entity)
        {
            return new SingleTaskJobDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Status = entity.Status,
                TaskId = entity.TaskId,
                Parameters = new TaskParameters
                {
                    Command = entity.Command,
                    Arguments = entity.Arguments,
                    NbCores = entity.NbCores
                }
            };
        }

        public static ManyJobsEntity ToEntity(this ManyJobsDto dto)
        {
            var entity = new ManyJobsEntity { Id = dto.Id };
            entity.Update(dto);
            return entity;
        }

        public static void Update(this ManyJobsEntity entity, ManyJobsDto dto)
        {
            entity.Name = dto.Name;
            entity.Status = dto.Status;
            entity.Behavior = dto.Behavior;
            entity.JobIds = string.Join(SEP, dto.JobIds);
        }

        public static ManyJobsDto ToDto(this ManyJobsEntity entity)
        {
            var dto = new ManyJobsDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Status = entity.Status,
                Behavior = entity.Behavior
            };

            dto.JobIds = !string.IsNullOrEmpty(entity.JobIds)
                ? entity.JobIds.Split(SEP).Select(p => Guid.Parse(p)).ToArray()
                : Array.Empty<Guid>();
            return dto;
        }

        #endregion

        #region Pipelines

        public static PipelineEntity ToEntity(this PipelineDto dto)
        {
            var entity = new PipelineEntity { Id = dto.Id };
            entity.Update(dto);
            return entity;
        }
        public static void Update(this PipelineEntity entity, PipelineDto dto)
        {
            entity.Name = dto.Name;
            entity.QueueName = dto.QueueName;
            entity.RootJobId = dto.RootJobId;
        }
        public static PipelineDto ToDto(this PipelineEntity entity)
        {
            return new PipelineDto
            {
                Id = entity.Id,
                Name = entity.Name,
                QueueName = entity.QueueName,
                RootJobId = entity.RootJobId,
            };
        }

        #endregion
    }
}
