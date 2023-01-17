using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Dojo.Repositories
{
    public interface IRepository<TValue>
    {
        IDisposable Subscribe(IList<TValue> list);
    }

    public class Repository<TKey, TValue> : Repository<TKey, TValue, TValue>
    {
        public Repository(Func<TValue, TKey> getKey)
            : base(getKey, p => p, (s, v) => { }) { }
    }

    public class Repository<TKey, TValue, TSource> : Dictionary<TKey, TValue>, IRepository<TValue>
    {
        private readonly Dictionary<Guid, RepositoryView<TKey, TValue>> _views = new Dictionary<Guid, RepositoryView<TKey, TValue>>();
        private readonly Func<TSource, TKey> _getKey;
        private readonly Func<TSource, TValue> _create;
        private readonly Action<TSource, TValue> _update;

        public Repository(
            Func<TSource, TKey> getKey,
            Func<TSource, TValue> create,
            Action<TSource, TValue> update)
        {
            _getKey = getKey;
            _create = create;
            _update = update;
        }

        public void Reload(IEnumerable<TSource> sources)
        {
            var keys = new HashSet<TKey>(Keys);
            foreach (var source in sources)
            {
                var key = _getKey(source);
                if (!TryGetValue(key, out var value))
                    Add(key, value = Create(key, source));
                else _update(source, value);
                keys.Remove(key);
            }

            foreach (var key in keys)
            {
                Delete(key, this[key]);
                Remove(key);
            }
        }

        private TValue Create(TKey key, TSource source)
        {
            var value = _create(source);
            foreach (var pair in _views)
                pair.Value.Add(key, value);
            return value;
        }

        private void Delete(TKey key, TValue value)
        {

            foreach (var pair in _views)
                pair.Value.Remove(key, value);

            if (value is IDisposable disposable)
                disposable.Dispose();
        }

        public IDisposable Subscribe(IList<TValue> list)
        {
            var view = new RepositoryView<TKey, TValue>(list);
            _views.Add(view.Id, view);
            foreach (var pair in this)
                view.Add(pair.Key, pair.Value);
            return new Disposable(() => _views.Remove(view.Id));
        }
    }

    public class RepositoryView<TKey, TValue>
    {
        private readonly IList<TValue> _view;
        private readonly Dictionary<TKey, int> _indices = new Dictionary<TKey, int>();

        public Guid Id { get; } = Guid.NewGuid();

        public RepositoryView(IList<TValue> view)
        {
            _view = view;
        }

        public void Add(TKey key, TValue value)
        {
            _indices[key] = _view.Count;
            _view.Add(value);
        }

        public void Update(TKey key, TValue value)
        {
            _view[_indices[key]] = value;
        }

        public void Remove(TKey key, TValue value)
        {
            if (!_indices.TryGetValue(key, out var index))
                return;
            _view.RemoveAt(index);
            _indices.Remove(key);

            var keys = _indices.Where(p => p.Value > index).Select(p => p.Key).ToArray();
            foreach (var k in keys)
                _indices[k]--;
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
            _onDispose?.Invoke();
            _onDispose = null;
        }
    }
}
