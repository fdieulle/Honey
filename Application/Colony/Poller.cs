using System;

namespace Application.Colony
{
    public class Poller : IDisposable
    {
        private readonly ITimer _timer;
        private readonly BeeKeeper _beeKeeper;
        private readonly BeehiveProvider _beehives;
        private readonly TaskTracker _tasksTracker;

        public Poller(ITimer timer, BeeKeeper beeKeeper, BeehiveProvider beehives, TaskTracker tasksTracker)
        {
            _timer = timer;
            _beeKeeper = beeKeeper;
            _beehives = beehives;
            _tasksTracker = tasksTracker;
        }
        public void Start() => _timer.Updated += Refresh;

        private void Refresh()
        {
            _beeKeeper.Refresh();
            _beehives.Refresh();
            _tasksTracker.Refresh();
        }

        public void Dispose() => _timer.Updated -= Refresh;
    }
}
