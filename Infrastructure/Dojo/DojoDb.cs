using Application.Dojo;
using Domain.Dtos;
using Domain.Entities;
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
        private readonly CrudDbTable<DojoDbContext, QueuedTaskEntity, Guid, QueuedTaskDto> _crudTasks;
        public DojoDb(IDbContextFactory<DojoDbContext> factory)
        {
            _crudNinjas = new CrudDbTable<DojoDbContext, NinjaEntity, string, NinjaDto>(
                factory, c => c.Ninjas, p => p.Address, ToEntity, UpdateEntity, p => p.Address, ToDto);
            _crudQueues = new CrudDbTable<DojoDbContext, QueueEntity, string, QueueDto>(
                factory, c => c.Queues, p => p.Name, ToEntity, UpdateEntity, p => p.Name, ToDto);
            _crudTasks = new CrudDbTable<DojoDbContext, QueuedTaskEntity, Guid, QueuedTaskDto>(
                factory, c => c.Tasks, p => p.Id, ToEntity, UpdateEntity, p => p.Id, ToDto);
        }

        public IEnumerable<NinjaDto> FetchNinjas() => _crudNinjas.Fetch();

        public void CreateNinja(NinjaDto ninja) => _crudNinjas.Create(ninja);

        public void DeleteNinja(string address) => _crudNinjas.Delete(address);

        public IEnumerable<QueueDto> FetchQueues() => _crudQueues.Fetch();

        public void CreateQueue(QueueDto queue) => _crudQueues.Create(queue);

        public void UpdateQueue(QueueDto queue) => _crudQueues.Update(queue);

        public void DeleteQueue(string name) => _crudQueues.Delete(name);

        public IEnumerable<QueuedTaskDto> FetchTasks() => _crudTasks.Fetch();

        public void CreateTask(QueuedTaskDto task) => _crudTasks.Create(task);

        public void UpdateTask(QueuedTaskDto task) => _crudTasks.Update(task);

        public void DeleteTask(Guid id) => _crudTasks.Delete(id);

        private static NinjaEntity ToEntity(NinjaDto dto) => new NinjaEntity { Address = dto.Address };
        private static void UpdateEntity(NinjaDto dto, NinjaEntity entity) { }
        private static NinjaDto ToDto(NinjaEntity entity) => new NinjaDto { Address = entity.Address };

        private const char SEP = ';';
        private static QueueEntity ToEntity(QueueDto dto)
        {
            var entity = new QueueEntity { Name = dto.Name };
            UpdateEntity(dto, entity);
            return entity;
        }
        private static void UpdateEntity(QueueDto dto, QueueEntity entity) 
        {
            entity.MaxParallelTasks = dto.MaxParallelTasks;
            entity.Ninjas = dto.Ninjas != null ? string.Join(SEP, dto.Ninjas) : null;
        }
        private static QueueDto ToDto(QueueEntity entity)
        {
            return new QueueDto { 
                Name = entity.Name,
                MaxParallelTasks = entity.MaxParallelTasks,
                Ninjas = entity.Ninjas != null ? entity.Ninjas.Split(SEP) : null
            };
        }

        private static QueuedTaskEntity ToEntity(QueuedTaskDto dto)
        {
            var entity = new QueuedTaskEntity { Id = dto.Id };
            UpdateEntity(dto, entity);
            return entity;
        }
        private static void UpdateEntity(QueuedTaskDto dto, QueuedTaskEntity entity)
        {
            entity.Name = dto.Name;
            entity.Status = dto.Status;
            entity.QueueName = dto.QueueName;
            entity.NinjaAddress = dto.NinjaAddress;
            entity.Order = dto.Order;
            entity.Command = dto.StartTask.Command;
            entity.Arguments = dto.StartTask.Arguments;
            entity.NbCores = dto.StartTask.NbCores;
        }
        private static QueuedTaskDto ToDto(QueuedTaskEntity entity)
        {
            return new QueuedTaskDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Status = entity.Status,
                QueueName = entity.QueueName,
                NinjaAddress = entity.NinjaAddress,
                Order = entity.Order,
                StartTask = new StartTaskDto
                {
                    Command = entity.Command,
                    Arguments = entity.Arguments,
                    NbCores = entity.NbCores
                }
            };
        }
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
}
