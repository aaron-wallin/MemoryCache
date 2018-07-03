using System;
using System.Linq;
using MemoryCache.Eviction;

namespace MemoryCache
{
    public class DataCache<TKey, TValue>
    {
        #region Fields
        private CacheSet<TKey, TValue>[] _cacheSets;
        private int _numberOfSetsInCache;
        private int _itemsPerSet;
        private Type _evictionProcessor;
        private object[] _evictionProcessorConstructorArgs;
        #endregion

        #region Properties
        /// <summary>
        /// The cache sets contained in this cache.
        /// </summary>
        public CacheSet<TKey, TValue>[] CacheSets
        {
            get { return _cacheSets; }
        }

        /// <summary>
        /// The specified number of sets in this cache.
        /// </summary>
        public int NumberOfSetsInCache
        {
            get { return _numberOfSetsInCache; }
        }

        /// <summary>
        /// The specified number of items per set.
        /// </summary>
        public int ItemsPerSet
        {
            get { return _itemsPerSet; }
        }
        #endregion

        #region Constructors
        public DataCache()
        {
            Initialize(int.MaxValue, 1);
        }

        public DataCache(int setsInCache, int itemsPerSet)
        {
            Initialize(itemsPerSet, setsInCache);
        }
        #endregion

        #region Methods - Public
        /// <summary>
        /// Sets the process that contains the eviction logic for the cache.
        /// </summary>
        /// <param name="evictionProcessor">Type of eviction processor (must implement IEvictionProcessor&lt;TKey, TValue&gt;)</param>
        /// <param name="constructorArgs">Constructor arguments (if any) for the eviction processor</param>
        public void SetEvictionProcessor(Type evictionProcessor, object[] constructorArgs)
        {
            _evictionProcessor = evictionProcessor;
            _evictionProcessorConstructorArgs = constructorArgs;

            foreach (var cacheSet in _cacheSets)
            {
                if (cacheSet != null)
                {
                    cacheSet.SetEvictionProcessor(GenerateEvictionProcessor());
                }
            }
        }

        /// <summary>
        /// Attempts to add an entry to the cache.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <returns>Returns bool indicating if the add was successful.</returns>
        public bool TryAdd(TKey key, TValue value)
        {
            return FindSet(key).TryAdd(key, value);
        }

        /// <summary>
        /// Attempts to retrieve a value from the cache.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <returns>Returns bool indicating if the get was successful.</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            return FindSet(key).TryGetValue(key, out value);
        }

        /// <summary>
        /// Attempts to retrieve a value from the cache.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Returns the value from the cache item or the default.</returns>
        public TValue GetValue(TKey key)
        {
            TValue value;
            FindSet(key).TryGetValue(key, out value);
            return value;
        }

        /// <summary>
        /// Attempts to evict a value from the cache.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value of removed item</param>
        /// <returns>Returns bool indicating if the eviction was successful.</returns>
        public bool TryEvict(TKey key, out TValue value)
        {
            return FindSet(key).TryEvict(key, out value);
        }
        #endregion

        #region Methods - Private
        private void Initialize(int itemsPerSet, int numberOfSets)
        {
            _itemsPerSet = itemsPerSet;
            _numberOfSetsInCache = numberOfSets;

            InitializeCacheSets();
        }

        private IEvictionProcessor<TKey, TValue> GenerateEvictionProcessor()
        {
            if (_evictionProcessor == null) return null;

            if (!(_evictionProcessor.GetInterfaces().Contains(typeof(IEvictionProcessor<TKey, TValue>))))
            {
                throw new ArgumentException("Type must implement IEvictionProcessor<TKey, TValue>");
            }

            return Activator.CreateInstance(_evictionProcessor, _evictionProcessorConstructorArgs) as IEvictionProcessor<TKey, TValue>;
        }

        private void InitializeCacheSets()
        {
            _cacheSets = new CacheSet<TKey, TValue>[_numberOfSetsInCache];
        }

        private CacheSet<TKey, TValue> FindSet(TKey cacheItemKey)
        {
            int setForItem = _numberOfSetsInCache == 1 ? 0 : (cacheItemKey.GetHashCode() % _numberOfSetsInCache);

            if (_cacheSets[setForItem] == null)
            {
                _cacheSets[setForItem] = CreateCacheSet();
            }

            return _cacheSets[setForItem];
        }

        private CacheSet<TKey, TValue> CreateCacheSet()
        {
            var cacheSet = new CacheSet<TKey, TValue>(_itemsPerSet);
            cacheSet.SetEvictionProcessor(GenerateEvictionProcessor());
            return cacheSet;
        } 
        #endregion
    }
}
