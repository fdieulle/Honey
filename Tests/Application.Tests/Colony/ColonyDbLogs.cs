﻿using Application.Colony;
using Domain.Dtos;
using Domain.Dtos.Workflows;
using System;
using System.Collections.Generic;
using Xunit;

namespace Application.Tests.Colony
{
    public class ColonyDbLogs : IColonyDb
    {
        private readonly Table<string, BeeDto> _beeTable = new Table<string, BeeDto>(p => p.Address);
        private readonly Table<string, BeehiveDto> _beehiveTable = new Table<string, BeehiveDto>(p => p.Name);
        private readonly Table<Guid, RemoteTaskDto> _taskTable = new Table<Guid, RemoteTaskDto>(p => p.Id);
        private readonly Table<Guid, JobDto> _jobTable = new Table<Guid, JobDto>(p => p.Id);
        private readonly Table<Guid, WorkflowDto> _workflowTable = new Table<Guid, WorkflowDto>(p => p.Id);
        public void CreateBee(BeeDto bee) => _beeTable.Create(bee);

        public void CreateBeehive(BeehiveDto beehive) => _beehiveTable.Create(beehive);

        public void CreateTask(RemoteTaskDto task) => _taskTable.Create(task);

        public void DeleteBee(string address) => _beeTable.Delete(address);

        public void DeleteBeehive(string name) => _beehiveTable.Delete(name);

        public void DeleteTask(Guid id) => _taskTable.Delete(id);

        public IEnumerable<BeeDto> FetchBees() => _beeTable.FetchAll();

        public IEnumerable<BeehiveDto> FetchBeehives() => _beehiveTable.FetchAll();

        public IEnumerable<RemoteTaskDto> FetchTasks() => _taskTable.FetchAll();

        public void UpdateBeehive(BeehiveDto beehive) => _beehiveTable.Update(beehive);

        public void UpdateTask(RemoteTaskDto task) => _taskTable.Update(task);

        public IEnumerable<JobDto> FetchJobs() => _jobTable.FetchAll();

        public JobDto FetchJob(Guid id) => _jobTable.Fetch(id);

        public void CreateJob(JobDto job) => _jobTable.Create(job);

        public void UpdateJob(JobDto job) => _jobTable.Update(job);

        public void DeleteJob(Guid id) => _jobTable.Delete(id);

        public IEnumerable<WorkflowDto> FetchWorkflows() => _workflowTable.FetchAll();

        public WorkflowDto FetchWorkflow(Guid id) => _workflowTable.Fetch(id);

        public void CreateWorkflow(WorkflowDto workflow) => _workflowTable.FetchAll();

        public void UpdateWorkflow(WorkflowDto workflow) => _workflowTable.Update(workflow);

        public void DeleteWorkflow(Guid id) => _workflowTable.Delete(id);

        public ITableChecker<string, BeeDto> BeeTable => _beeTable;
        public ITableChecker<string, BeehiveDto> BeehiveTable => _beehiveTable;
        public ITableChecker<Guid, RemoteTaskDto> TaskTable => _taskTable;
        public ITableChecker<Guid, JobDto> JobTable => _jobTable;
        public ITableChecker<Guid, WorkflowDto> WorkflowTable => _workflowTable;

        public void ClearLogs()
        {
            _beeTable.ClearLogs();
            _beehiveTable.ClearLogs();
            _taskTable.ClearLogs();
            _jobTable.ClearLogs();
            _workflowTable.ClearLogs();
        }

        public interface ITableChecker<TKey, T>
        {
            T NextCreate();
            void EmptyCreates();

            T NextUpdate();
            void EmptyUpdates();
            TKey NextDelete();
            void EmptyDeletes();

            void EmptyLogs();
        }
        private class Table<TKey, T> : ITableChecker<TKey, T>
        {
            private readonly Dictionary<TKey, T> _table = new Dictionary<TKey, T>();
            private readonly Queue<T> _creates = new Queue<T>();
            private readonly Queue<T> _updates = new Queue<T>();
            private readonly Queue<TKey> _deletes = new Queue<TKey>();
            private readonly Func<T, TKey> _getKey;

            public Table(Func<T, TKey> getKey)
            {
                _getKey = getKey;
            }

            public IEnumerable<T> FetchAll() => _table.Values;

            public T Fetch(TKey key) => _table[key];

            public void Create(T item)
            {
                var clone = item.DeepCopy();
                _table.Add(_getKey(clone), clone);
                _creates.Enqueue(clone);
            }

            public T NextCreate() => _creates.Dequeue();

            public void EmptyCreates() => Assert.Empty(_creates);

            public void Update(T item)
            {
                var clone = item.DeepCopy();
                var key = _getKey(clone);
                if (!_table.ContainsKey(key))
                    throw new InvalidOperationException($"Key {key} has not been created first");
                _table[key] = clone;
                _updates.Enqueue(clone);
            }

            public T NextUpdate() => _updates.Count > 0 ? _updates.Dequeue() : default;

            public void EmptyUpdates() => Assert.Empty(_updates);

            public void Delete(TKey key)
            {
                var clone = key.DeepCopy();
                _table.Remove(clone);
                _deletes.Enqueue(clone);
            }

            public TKey NextDelete() => _deletes.Dequeue();

            public void EmptyDeletes() => Assert.Empty(_deletes);

            public void EmptyLogs()
            {
                EmptyCreates();
                EmptyUpdates();
                EmptyDeletes();
            }

            public void ClearLogs()
            {
                _creates.Clear();
                _updates.Clear();
                _deletes.Clear();
            }
        }
    }
}
