using System;

namespace Application.Dojo
{
    public class Poller : IDisposable
    {
        private readonly ITimer _timer;
        private readonly Dojo _dojo;
        private readonly QueueProvider _queues;
        private readonly TaskTracker _tasksTracker;

        public Poller(ITimer timer, Dojo dojo, QueueProvider queues, TaskTracker tasksTracker)
        {
            _timer = timer;
            _dojo = dojo;
            _queues = queues;
            _tasksTracker = tasksTracker;
        }
        public void Start() => _timer.Updated += Refresh;

        private void Refresh()
        {
            _dojo.Refresh();
            _queues.Refresh();
            _tasksTracker.Refresh();
        }

        public void Dispose() => _timer.Updated -= Refresh;
    }
}
