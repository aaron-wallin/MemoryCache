using System.Collections.Concurrent;
using MemoryCache.Eviction;

namespace MemoryCache
{
    public class CacheSet<TKey, TValue>
    {
        #region Fields
        private readonly ConcurrentDictionary<TKey, TValue> _cacheSet;
        private readonly int _capacity;
        private IEvictionProcessor<TKey, TValue> _evictionProcessor;
        #endregion

        #region Properties
        /// <summary>
        /// The number of items currently in this cache set.
        /// </summary>
        public int Count
        {
            get { return _cacheSet.Count; }
        }

        /// <summary>
        /// The specified capacity of this cache set.
        /// </summary>
        public int Capacity
        {
            get { return _capacity; }
        }
        #endregion

        #region Constructors
        public CacheSet(int capacity)
        {
            _capacity = capacity;
            _cacheSet = new ConcurrentDictionary<TKey, TValue>();
        }
        #endregion

        /// <summary>
        /// Sets the process that contains the eviction logic for the cache set.
        /// </summary>
        /// <param name="evictionProcessor">An eviction processor instance (must implement IEvictionProcessor&lt;TKey, TValue&gt;)</param>
        public void SetEvictionProcessor(IEvictionProcessor<TKey, TValue> evictionProcessor)
        {
            if (evictionProcessor == null) return;

            _evictionProcessor = evictionProcessor;
            _evictionProcessor.InitiateEviction += (s, e) => 
            {
                TValue value;
                TryEvict(e.KeyToEvict, out value);
            };
        }

        #region Methods - Public
        /// <summary>
        /// Attempts to add an entry to the cache set.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <returns>Returns bool indicating if the add was successful.</returns>
        public bool TryAdd(TKey key, TValue value)
        {
            var success = _cacheSet.TryAdd(key, value);

            if (success)
            {
                ProcessEviction();
                Track(TrackingAction.Added, key, value);
            }

            return success;
        }

        /// <summary>
        /// Attempts to retrieve a value from the cache set.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <returns>Returns bool indicating if the get was successful.</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            var success = _cacheSet.TryGetValue(key, out value);

            if (success)
            {
                Track(TrackingAction.Get, key, value);
            }

            return success;
        }

        /// <summary>
        /// Attempts to evict a value from the cache set.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value of removed item</param>
        /// <returns>Returns bool indicating if the eviction was successful.</returns>
        public bool TryEvict(TKey key, out TValue value)
        {
            var success = _cacheSet.TryRemove(key, out value);

            if (success && _evictionProcessor != null)
            {
                _evictionProcessor.Track(TrackingAction.Evicted, key, value);
            }

            return success;
        }
        #endregion

        #region Methods - Private
        private void ProcessEviction()
        {
            if (_cacheSet.Count <= _capacity) return;
            if (_evictionProcessor == null) return;

            TValue value;
            TryEvict(_evictionProcessor.GetNextKeyToEvict(), out value);
        }

        private void Track(Eviction.TrackingAction trackingAction, TKey key, TValue value)
        {
            if (_evictionProcessor == null) return;
            _evictionProcessor.Track(trackingAction, key, value);
        } 
        #endregion
    }
}
