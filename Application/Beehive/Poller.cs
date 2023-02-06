using System;

namespace Application.Beehive
{
    public class Poller : IDisposable
    {
        private readonly ITimer _timer;
        private readonly BeeKeeper _beeKeeper;
        private readonly ColonyProvider _colonies;
        private readonly TaskTracker _tasksTracker;

        public Poller(ITimer timer, BeeKeeper beeKeeper, ColonyProvider colonies, TaskTracker tasksTracker)
        {
            _timer = timer;
            _beeKeeper = beeKeeper;
            _colonies = colonies;
            _tasksTracker = tasksTracker;
        }
        public void Start() => _timer.Updated += Refresh;

        private void Refresh()
        {
            _beeKeeper.Refresh();
            _colonies.Refresh();
            _tasksTracker.Refresh();
        }

        public void Dispose() => _timer.Updated -= Refresh;
    }
}
