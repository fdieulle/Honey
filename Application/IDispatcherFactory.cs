using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Application
{
    public interface IDispatcher
    {
        void Dispatch(Action action);
    }

    public interface IDispatcherFactory
    {
        IDispatcher CreateDispathcer(string name = null);

        IDispatcher CreateSequencer(string name = null);

        IDisposable Schedule(int dueTimeMs, int intervalMs, Action action);
    }

    public class DispatcherFactory : IDispatcherFactory
    {
        private readonly ConcurrentDictionary<string, IDispatcher> _dispatchers = new ConcurrentDictionary<string, IDispatcher>();
        private readonly ConcurrentDictionary<string, IDispatcher> _sequencers = new ConcurrentDictionary<string, IDispatcher>();

        public IDispatcher CreateDispathcer(string name = null) 
            => name == null 
                ? new Dispatcher(null) 
                : _dispatchers.GetOrAdd(name, n => new Dispatcher(n));

        public IDispatcher CreateSequencer(string name = null) 
            => name == null
                ? new Dispatcher(null)
                : _sequencers.GetOrAdd(name, n => new Sequencer(n));

        public IDisposable Schedule(int dueTimeMs, int intervalMs, Action action)
        {
            throw new NotImplementedException();
        }

        private class Dispatcher : IDispatcher
        {
            private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            private readonly string _name;

            public Dispatcher(string name) => _name = name;

            public void Dispatch(Action action) => Task.Run(() =>
            {
                try { action(); }
                catch (Exception e) { Logger.Error($"Dispatcher #{_name}, Unexpected exception", e); }
            });
        }

        private class Sequencer : IDispatcher, IDisposable
        {
            private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            private readonly string _name;
            private readonly ActionBlock<Action> _block;

            public Sequencer(string name)
            {
                _name = name;
                _block = new ActionBlock<Action>(OnDispatch);
            }

            public void Dispatch(Action action)
                => _block.Post(action);

            private void OnDispatch(Action action)
            {
                try { action(); }
                catch (Exception e) { Logger.Error($"Sequencer #{_name}, Unexpected exception", e); }
            }

            public void Dispose()
            {
                _block.Complete();
                _block.Completion.Wait();
            }
        }
    }

    public class SynchronousDispatcherFactory : IDispatcherFactory
    {
        private readonly Dictionary<string, IDispatcher> _dispatchers = new Dictionary<string, IDispatcher>();
        private readonly Dictionary<string, IDispatcher> _sequencers = new Dictionary<string, IDispatcher>();

        public IDispatcher CreateDispathcer(string name = null)
            => name == null
                ? new SynchronousDispatcher()
                : _dispatchers.GetOrAdd(name, n => new SynchronousDispatcher());

        public IDispatcher CreateSequencer(string name = null)
            => name == null
                ? new SynchronousDispatcher()
                : _sequencers.GetOrAdd(name, n => new SynchronousDispatcher());

        public IDisposable Schedule(int dueTimeMs, int intervalMs, Action action)
        {
            throw new NotImplementedException();
        }

        private class SynchronousDispatcher : IDispatcher
        {
            public void Dispatch(Action action) => action();
        }
    }
}
