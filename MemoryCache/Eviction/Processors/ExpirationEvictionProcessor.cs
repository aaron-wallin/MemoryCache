using System;
using System.Linq;
using System.Collections.Concurrent;

namespace MemoryCache.Eviction
{
    public class ExpirationEvictionProcessor<TKey, TValue> : IEvictionProcessor<TKey, TValue>
    {
        public event EventHandler<InitiateEvictionEventArgs<TKey>> InitiateEviction;
        private ConcurrentDictionary<TKey, DateTime> _trackingList;
        private System.Timers.Timer _timer;
        private double _timerInterval = 3600000;
        private double _expirationMilliseconds = 3600000;

        public ExpirationEvictionProcessor(double evictionIntervalMilliseconds)
        {
            _timerInterval = evictionIntervalMilliseconds;
            _expirationMilliseconds = evictionIntervalMilliseconds;

            _trackingList = new ConcurrentDictionary<TKey, DateTime>();
            _timer = new System.Timers.Timer(_timerInterval);
            _timer.Elapsed += _timer_Elapsed;
            _timer.AutoReset = true;
            _timer.Start();
        }

        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var expireTime = DateTime.UtcNow;
            var itemsToExpire = _trackingList.Where(a => a.Value <= expireTime).ToList();

            foreach(var i in itemsToExpire)
            {
                OnEvictionInitiated(i.Key);
            }
        }

        public TKey GetNextKeyToEvict()
        {
            return default(TKey);
        }

        public void Track(TrackingAction trackingAction, TKey key, TValue value)
        {
            switch (trackingAction)
            {
                case TrackingAction.Added:
                case TrackingAction.Get:
                    TryRemoveKeyFromTrackingList(key);
                    AddOrUpdateValue(key, value);
                    break;
                case TrackingAction.Evicted:
                    CleanUpList(key, value);
                    break;
                default:
                    break;
            }
        }

        private void OnEvictionInitiated(TKey key)
        {
            InitiateEviction?.Invoke(this, new InitiateEvictionEventArgs<TKey>() { KeyToEvict = key });
        }

        private void AddOrUpdateValue(TKey key, TValue value)
        {
            if (!_trackingList.TryAdd(key, DateTime.UtcNow.AddMilliseconds(_expirationMilliseconds)))
            {
                _trackingList[key] = DateTime.UtcNow;
            }
        }

        private void CleanUpList(TKey key, TValue value)
        {
            TryRemoveKeyFromTrackingList(key);
        }

        private void TryRemoveKeyFromTrackingList(TKey key)
        {
            var count = _trackingList.Count(i => i.Equals(key));

            for (int i = 0; i < count; i++)
            {
                DateTime dateTime;
                _trackingList.TryRemove(key, out dateTime);
            }
        }
    }
}
