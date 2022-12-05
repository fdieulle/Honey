using Application;
using System;
using System.Threading;

namespace Infrastructure
{
    internal class ThreadedTimer : ITimer, IDisposable
    {
        private readonly Timer _timer;
        private readonly int _intervalMs;
        private bool _isDisposed;

        public ThreadedTimer(int intervalMs)
        {
            _timer = new Timer(OnTimerTick, null, intervalMs, Timeout.Infinite);
            _intervalMs = intervalMs;
        }

        public event Action Updated;

        private void OnTimerTick(object state)
        {
            Updated?.Invoke();

            if (!_isDisposed)
                _timer.Change(_intervalMs, Timeout.Infinite);
        }

        public void Dispose()
        {
            _isDisposed = true;
            _timer.Dispose();
        }
    }
}
