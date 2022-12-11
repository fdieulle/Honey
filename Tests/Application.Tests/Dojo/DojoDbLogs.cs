﻿using Application.Dojo;
using Domain.Dtos;
using System;
using System.Collections.Generic;
using Xunit;

namespace Application.Tests.Dojo
{
    public class DojoDbLogs : IDojoDb
    {
        private readonly Table<string, NinjaDto> _ninjaTable = new Table<string, NinjaDto>(p => p.Address);
        private readonly Table<string, QueueDto> _queueTable = new Table<string, QueueDto>(p => p.Name);
        private readonly Table<Guid, QueuedTaskDto> _taskTable = new Table<Guid, QueuedTaskDto>(p => p.Id);
        public void CreateNinja(NinjaDto ninja) => _ninjaTable.Create(ninja);

        public void CreateQueue(QueueDto queue) => _queueTable.Create(queue);

        public void CreateTask(QueuedTaskDto task) => _taskTable.Create(task);

        public void DeleteNinja(string address) => _ninjaTable.Delete(address);

        public void DeleteQueue(string name) => _queueTable.Delete(name);

        public void DeleteTask(Guid id) => _taskTable.Delete(id);

        public IEnumerable<NinjaDto> FetchNinjas() => _ninjaTable.FetchAll();

        public IEnumerable<QueueDto> FetchQueues() => _queueTable.FetchAll();

        public IEnumerable<QueuedTaskDto> FetchTasks() => _taskTable.FetchAll();

        public void UpdateQueue(QueueDto queue) => _queueTable.Update(queue);

        public void UpdateTask(QueuedTaskDto task) => _taskTable.Update(task);

        public ITableChecker<string, NinjaDto> NinjaTable => _ninjaTable;
        public ITableChecker<string, QueueDto> QueueTable => _queueTable;
        public ITableChecker<Guid, QueuedTaskDto> TaskTable => _taskTable;

        public void ClearLogs()
        {
            _ninjaTable.ClearLogs();
            _queueTable.ClearLogs();
            _taskTable.ClearLogs();
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