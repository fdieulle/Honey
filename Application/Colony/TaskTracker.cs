using Domain.Dtos;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Application.Colony
{
    public class TaskTracker : ITaskTracker
    {
        private readonly ConcurrentQueue<RemoteTaskDto> _queue = new ConcurrentQueue<RemoteTaskDto>();
        private Action<IEnumerable<RemoteTaskDto>>[] _listeners = Array.Empty<Action<IEnumerable<RemoteTaskDto>>>();
        private readonly object _mutex = new object();
        public void Track(RemoteTaskDto task) => _queue.Enqueue(task);

        public void Refresh()
        {
            if (_queue.Count == 0) return;

            var updatedTasks = new List<RemoteTaskDto>(_queue.Count);
            while (_queue.TryDequeue(out var task))
                updatedTasks.Add(task);

            var listeners = _listeners;
            foreach (var listener in listeners)
                listener(updatedTasks);
        }

        public IDisposable Subscribe(Action<IEnumerable<RemoteTaskDto>> onUpdate)
        {
            lock (_mutex)
            {
                AddSafe(ref _listeners, onUpdate);
            }

            return new Disposable(() => Unsubscribe(onUpdate));
        }

        private void Unsubscribe(Action<IEnumerable<RemoteTaskDto>> onUpdate)
        {
            lock (_mutex)
            {
                RemoveSafe(ref _listeners, onUpdate);
            }
        }

        public IWorkflowTaskTracker CreateScope(IDispatcher dispatcher) => new WorkflowTaskTracker(this, dispatcher);

        public static void AddSafe<T>(ref T[] array, T item)
        {
            var copy = new T[array.Length + 1];
            for (var i = 0; i < array.Length; i++)
                copy[i] = array[i];
            copy[array.Length] = item;
            array = copy;
        }

        public static bool RemoveSafe<T>(ref T[] array, T item)
        {
            if (array.Length == 0) return false;

            var copy = new T[array.Length - 1];
            var found = false;
            for (var i = 0; i < array.Length; i++)
            {
                if (ReferenceEquals(array[i], item))
                    copy[i] = array[i];
                else found = true;
            }

            array = copy;
            return found;
        }
    }

    public class WorkflowTaskTracker : IWorkflowTaskTracker
    {
        private readonly Dictionary<Guid, List<Action<RemoteTaskDto>>> _listeners = new Dictionary<Guid, List<Action<RemoteTaskDto>>>();
        private readonly Dictionary<Guid, RemoteTaskDto> _cache = new Dictionary<Guid, RemoteTaskDto>();
        private readonly IDispatcher _dispatcher;
        private readonly IDisposable _subscription;

        public WorkflowTaskTracker(ITaskTracker taskTracker, IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _subscription = taskTracker.Subscribe(OnUpdate);
        }

        private void OnUpdate(IEnumerable<RemoteTaskDto> tasks)
        {
            _dispatcher.Dispatch(() =>
            {
                foreach(var task in tasks)
                {
                    if (!_listeners.TryGetValue(task.Id, out var subscribers))
                        continue;

                    foreach (var onUpdate in subscribers)
                        onUpdate(task);

                    _cache[task.Id] = task;
                }
            });
            
        }

        public IDisposable Subscribe(Guid taskId, Action<RemoteTaskDto> onUpdate)
        {
            _dispatcher.Dispatch(() =>
            {
                if (!_listeners.TryGetValue(taskId, out var listeners))
                    _listeners.Add(taskId, listeners = new List<Action<RemoteTaskDto>>());

                listeners.Add(onUpdate);

                if (_cache.TryGetValue(taskId, out var task))
                    onUpdate(task);
            });

            return new Disposable(() => Unsubscribe(taskId, onUpdate));
        }

        private void Unsubscribe(Guid taskId, Action<RemoteTaskDto> onUpdate)
        {
            _dispatcher.Dispatch(() =>
            {
                if (!_listeners.TryGetValue(taskId, out var listeners))
                    return;

                listeners.Remove(onUpdate);

                if (listeners.Count == 0)
                {
                    _listeners.Remove(taskId);
                    _cache.Remove(taskId);
                }
            });
        }

        public void Dispose()
        {
            _subscription?.Dispose();
        }
    }

    public class Disposable : IDisposable
    {
        private Action _onDispose;

        public Disposable(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            if (_onDispose == null) return;
            var closure = _onDispose;
            _onDispose = null;
            closure();
        }
    }
}
