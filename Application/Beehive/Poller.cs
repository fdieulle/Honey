using System;

namespace Application.Beehive
{
    public class Poller : IDisposable
    {
        private readonly ITimer _timer;
        private readonly Beehive _beehive;
        private readonly QueueProvider _queues;
        private readonly TaskTracker _tasksTracker;

        public Poller(ITimer timer, Beehive beehive, QueueProvider queues, TaskTracker tasksTracker)
        {
            _timer = timer;
            _beehive = beehive;
            _queues = queues;
            _tasksTracker = tasksTracker;
        }
        public void Start() => _timer.Updated += Refresh;

        private void Refresh()
        {
            _beehive.Refresh();
            _queues.Refresh();
            _tasksTracker.Refresh();
        }

        public void Dispose() => _timer.Updated -= Refresh;
    }
}
