using System;

namespace Application.Dojo
{
    public class Poller : IDisposable
    {
        private readonly ITimer _timer;
        private readonly Dojo _dojo;
        private readonly QueueProvider _queues;

        public Poller(ITimer timer, Dojo dojo, QueueProvider queues)
        {
            _timer = timer;
            _dojo = dojo;
            _queues = queues;
        }
        public void Start() => _timer.Updated += Refresh;

        private void Refresh()
        {
            _dojo.Refresh();
            _queues.Refresh();
        }

        public void Dispose() => _timer.Updated -= Refresh;
    }
}
