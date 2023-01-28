using Domain.Dtos;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Application.Beehive
{
    public class TaskTracker : ITaskTracker
    {
        private readonly ConcurrentQueue<RemoteTaskDto> _queue = new ConcurrentQueue<RemoteTaskDto>();
        private readonly ConcurrentDictionary<Guid, List<Action<RemoteTaskDto>>> _listeners = new ConcurrentDictionary<Guid, List<Action<RemoteTaskDto>>>();
        public void Track(RemoteTaskDto task) => _queue.Enqueue(task);

        public void Refresh()
        {
            while (_queue.TryDequeue(out var task))
            {
                if (_listeners.TryGetValue(task.Id, out var subscribers))
                    foreach (var onUpdate in subscribers)
                        onUpdate(task);
            }
        }

        public IDisposable Subscribe(Guid taskId, Action<RemoteTaskDto> onUpdate)
        {
            var list = _listeners.GetOrAdd(taskId, new List<Action<RemoteTaskDto>>());
            list.Add(onUpdate);

            return new Disposable(() => list.Remove(onUpdate));
        }

        private class Disposable : IDisposable
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
}
