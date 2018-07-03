using System;
using System.Collections.Generic;
using System.Linq;

namespace MemoryCache.Eviction
{
    public class MruEvictionProcessor<TKey, TValue> : IEvictionProcessor<TKey, TValue>
    {
        private LinkedList<TKey> _trackingList;
        private object _stackLock = new object();
        public event EventHandler<InitiateEvictionEventArgs<TKey>> InitiateEviction;

        public MruEvictionProcessor()
        {
            _trackingList = new LinkedList<TKey>();
        }

        public TKey GetNextKeyToEvict()
        {
            lock (_stackLock)
            {
                return _trackingList.First();
            }
        }

        public void Track(TrackingAction trackingAction, TKey key, TValue value)
        {
            lock (_stackLock)
            {
                switch (trackingAction)
                {
                    case TrackingAction.Added:
                    case TrackingAction.Get:
                        TryRemoveKeyFromTrackingList(key);
                        _trackingList.AddFirst(key);
                        break;
                    case TrackingAction.Evicted:
                        CleanUpList(key, value);
                        break;
                    default:
                        break;
                }
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
                _trackingList.Remove(key);
            }
        }
    }
}
