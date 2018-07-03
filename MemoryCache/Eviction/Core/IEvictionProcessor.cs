using System;

namespace MemoryCache.Eviction
{
    public class InitiateEvictionEventArgs<TKey> : EventArgs
    {
        public TKey KeyToEvict { get; set; }
    }

    public interface IEvictionProcessor<TKey, TValue>
    {   
        void Track(TrackingAction trackingAction, TKey key, TValue value);
        TKey GetNextKeyToEvict();

        event EventHandler<InitiateEvictionEventArgs<TKey>> InitiateEviction;
    }
}
