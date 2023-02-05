using System;

namespace Application.Beehive
{
    public class Poller : IDisposable
    {
        private readonly ITimer _timer;
        private readonly BeeKeeper _beeKeeper;
        private readonly QueueProvider _queues;
        private readonly TaskTracker _tasksTracker;

        public Poller(ITimer timer, BeeKeeper beeKeeper, QueueProvider queues, TaskTracker tasksTracker)
        {
            _timer = timer;
            _beeKeeper = beeKeeper;
            _queues = queues;
            _tasksTracker = tasksTracker;
        }
        public void Start() => _timer.Updated += Refresh;

        private void Refresh()
        {
            _beeKeeper.Refresh();
            _queues.Refresh();
            _tasksTracker.Refresh();
        }

        public void Dispose() => _timer.Updated -= Refresh;
    }
}
