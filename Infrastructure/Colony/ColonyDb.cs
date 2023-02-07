using Application.Colony;
using Domain.Dtos;
using Domain.Dtos.Workflows;
using Domain.Entities;
using Domain.Entities.Workflows;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Colony
{
    internal class ColonyDb : IColonyDb
    {
        private readonly CrudDbTable<ColonyDbContext, BeeEntity, string, BeeDto> _crudBees;
        private readonly CrudDbTable<ColonyDbContext, BeehiveEntity, string, BeehiveDto> _crudBeehives;
        private readonly CrudDbTable<ColonyDbContext, RemoteTaskEntity, Guid, RemoteTaskDto> _crudTasks;
        private readonly CrudDbTable<ColonyDbContext, JobEntity, Guid, JobDto> _crudJobs;
        private readonly CrudDbTable<ColonyDbContext, WorkflowEntity, Guid, WorkflowDto> _crudWorkflows;
        public ColonyDb(IDbContextFactory<ColonyDbContext> factory)
        {
            _crudBees = new CrudDbTable<ColonyDbContext, BeeEntity, string, BeeDto>(
                factory, c => c.Bees, p => p.Address, d => d.ToEntity(), (d, e) => e.Update(d), p => p.Address, e => e.ToDto());
            _crudBeehives = new CrudDbTable<ColonyDbContext, BeehiveEntity, string, BeehiveDto>(
                factory, c => c.Beehives, p => p.Name, d => d.ToEntity(), (d, e) => e.Update(d), p => p.Name, e => e.ToDto());
            _crudTasks = new CrudDbTable<ColonyDbContext, RemoteTaskEntity, Guid, RemoteTaskDto>(
                factory, c => c.Tasks, p => p.Id, d => d.ToEntity(), (d, e) => e.Update(d), p => p.Id, e => e.ToDto());
            _crudJobs = new CrudDbTable<ColonyDbContext, JobEntity, Guid, JobDto>(
                factory, c => c.Jobs, p => p.Id, d => d.ToEntity(), (d, e) => e.Update(d), p => p.Id, e => e.ToDto());
            _crudWorkflows = new CrudDbTable<ColonyDbContext, WorkflowEntity, Guid, WorkflowDto>(
                factory, c => c.Workflows, p => p.Id, d => d.ToEntity(), (d, e) => e.Update(d), p => p.Id, e => e.ToDto());
        }

        #region Bee

        public IEnumerable<BeeDto> FetchBees() => _crudBees.Fetch();

        public void CreateBee(BeeDto bee) => _crudBees.Create(bee);

        public void DeleteBee(string address) => _crudBees.Delete(address);

        #endregion

        #region Colonies

        public IEnumerable<BeehiveDto> FetchBeehives() => _crudBeehives.Fetch();

        public void CreateBeehive(BeehiveDto beehive) => _crudBeehives.Create(beehive);

        public void UpdateBeehive(BeehiveDto beehive) => _crudBeehives.Update(beehive);

        public void DeleteBeehive(string name) => _crudBeehives.Delete(name);

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

        #region Worflows

        public IEnumerable<WorkflowDto> FetchWorkflows() => _crudWorkflows.Fetch();

        public WorkflowDto FetchWorkflow(Guid id) => _crudWorkflows.Fetch().FirstOrDefault(p => p.Id == id);

        public void CreateWorkflow(WorkflowDto worflow) => _crudWorkflows.Create(worflow);

        public void UpdateWorkflow(WorkflowDto workflow) => _crudWorkflows.Update(workflow);

        public void DeleteWorkflow(Guid id) => _crudWorkflows.Delete(id);

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

        private void Create(TContext context, TDto dto)
        {
            context.Add(_toEntity(dto));
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
        #region Bees

        public static BeeEntity ToEntity(this BeeDto dto) => new BeeEntity { Address = dto.Address };
        public static void Update(this BeeEntity entity, BeeDto dto) { }
        public static BeeDto ToDto(this BeeEntity entity) => new BeeDto { Address = entity.Address };

        #endregion

        #region Colonies
        
        private const char SEP = ';';
        public static BeehiveEntity ToEntity(this BeehiveDto dto)
        {
            var entity = new BeehiveEntity { Name = dto.Name };
            entity.Update(dto);
            return entity;
        }
        public static void Update(this BeehiveEntity entity, BeehiveDto dto)
        {
            entity.MaxParallelTasks = dto.MaxParallelTasks;
            entity.Bees = dto.Bees != null ? string.Join(SEP, dto.Bees) : null;
        }
        public static BeehiveDto ToDto(this BeehiveEntity entity)
        {
            return new BeehiveDto
            {
                Name = entity.Name,
                MaxParallelTasks = entity.MaxParallelTasks,
                Bees = entity.Bees != null ? entity.Bees.Split(SEP) : null
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
            entity.Beehive = dto.Beehive;
            entity.BeeAddress = dto.BeeAddress;
            entity.Order = dto.Order;
            entity.Command = dto.Parameters.Command;
            entity.Arguments = dto.Parameters.Arguments;
            entity.NbCores = dto.Parameters.NbCores;
            entity.BeeTaskId = dto.BeeState.Id;
        }
        public static RemoteTaskDto ToDto(this RemoteTaskEntity entity)
        {
            return new RemoteTaskDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Status = entity.Status,
                Beehive = entity.Beehive,
                BeeAddress = entity.BeeAddress,
                Order = entity.Order,
                Parameters = new TaskParameters
                {
                    Command = entity.Command,
                    Arguments = entity.Arguments,
                    NbCores = entity.NbCores
                },
                BeeState = new TaskDto
                {
                    Id = entity.BeeTaskId,
                    Status = TaskStatus.Pending
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

        #region Workflows

        public static WorkflowEntity ToEntity(this WorkflowDto dto)
        {
            var entity = new WorkflowEntity { Id = dto.Id };
            entity.Update(dto);
            return entity;
        }
        public static void Update(this WorkflowEntity entity, WorkflowDto dto)
        {
            entity.Name = dto.Name;
            entity.Beehive = dto.Beehive;
            entity.RootJobId = dto.RootJobId;
        }
        public static WorkflowDto ToDto(this WorkflowEntity entity)
        {
            return new WorkflowDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Beehive = entity.Beehive,
                RootJobId = entity.RootJobId,
            };
        }

        #endregion
    }
}
