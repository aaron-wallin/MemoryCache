using Microsoft.VisualStudio.TestTools.UnitTesting;
using MemoryCache.Eviction;
using System.Threading;

namespace MemoryCache.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void ManualEvictionTest()
        {
            var cache = new DataCache<int, string>(2, 5);
            // not assigning an eviction processor

            Assert.IsTrue(cache.ItemsPerSet == 5);
            Assert.IsTrue(cache.NumberOfSetsInCache == 2);
            Assert.IsTrue(cache.CacheSets.Length == 2);

            for (int i = 1; i <= 10; i++)
            {
                cache.TryAdd(i, "string " + i);
            }

            Assert.IsTrue(cache.CacheSets[0].Count == 5);
            Assert.IsTrue(cache.CacheSets[1].Count == 5);

            string evictedValue;
            Assert.IsTrue(cache.TryEvict(1, out evictedValue));

            Assert.IsTrue(evictedValue == "string 1");
            Assert.IsTrue(cache.CacheSets[0].Count == 5);
            Assert.IsTrue(cache.CacheSets[1].Count == 4);

            Assert.IsTrue(cache.TryEvict(2, out evictedValue));

            Assert.IsTrue(evictedValue == "string 2");
            Assert.IsTrue(cache.CacheSets[0].Count == 4);
            Assert.IsTrue(cache.CacheSets[1].Count == 4);
        }
        
        [TestMethod]
        public void ExpirationEviction_BasicEvictionTest()
        {
            var cache = new DataCache<int, string>(2, 10000);
            cache.SetEvictionProcessor(typeof(ExpirationEvictionProcessor<int, string>), new object[]{ 10000 });
            
            Assert.IsTrue(cache.ItemsPerSet == 10000);
            Assert.IsTrue(cache.NumberOfSetsInCache == 2);
            Assert.IsTrue(cache.CacheSets.Length == 2);

            for (int i = 1; i <= 10; i++)
            {
                cache.TryAdd(i, "string " + i);
            }

            Assert.IsTrue(cache.CacheSets[0].Count == 5, "[0] Actual count " + cache.CacheSets[0].Count);
            Assert.IsTrue(cache.CacheSets[1].Count == 5, "[0] Actual count " + cache.CacheSets[1].Count);

            Thread.Sleep(11000);

            cache.TryAdd(11, "string 11");
            cache.TryAdd(12, "string 12");

            Assert.IsTrue(cache.CacheSets[0].Count == 1, "[0] Actual count " + cache.CacheSets[0].Count);
            Assert.IsTrue(cache.CacheSets[1].Count == 1, "[1] Actual count " + cache.CacheSets[1].Count);
        }

        [TestMethod]
        public void MruEviction_BasicEvictionTest()
        {
            var cache = new DataCache<int, string>(1, 5);
            cache.SetEvictionProcessor(typeof(MruEvictionProcessor<int, string>), null);

            Assert.IsTrue(cache.ItemsPerSet == 5);
            Assert.IsTrue(cache.NumberOfSetsInCache == 1);
            Assert.IsTrue(cache.CacheSets.Length == 1);

            for (int i = 1; i <= 5; i++)
            {
                cache.TryAdd(i, "string " + i);
            }

            Assert.IsTrue(cache.CacheSets[0].Count == 5);

            cache.TryAdd(6, "string 6");

            Assert.IsTrue(cache.CacheSets[0].Count == 5);

            Assert.IsTrue(cache.GetValue(1) == "string 1");
            Assert.IsTrue(cache.GetValue(2) == "string 2");
            Assert.IsTrue(cache.GetValue(3) == "string 3");
            Assert.IsTrue(cache.GetValue(4) == "string 4");
            Assert.IsTrue(cache.GetValue(5) == null);
            Assert.IsTrue(cache.GetValue(6) == "string 6");
        }

        [TestMethod]
        public void LruEviction_BasicEvictionTest()
        {
            var cache = new DataCache<int, string>(1, 5);
            cache.SetEvictionProcessor(typeof(LruEvictionProcessor<int, string>), null);

            Assert.IsTrue(cache.ItemsPerSet == 5);
            Assert.IsTrue(cache.NumberOfSetsInCache == 1);
            Assert.IsTrue(cache.CacheSets.Length == 1);

            for (int i = 1; i <= 5; i++)
            {
                cache.TryAdd(i, "string " + i);
            }

            Assert.IsTrue(cache.CacheSets[0].Count == 5);

            cache.TryAdd(6, "string 6");

            Assert.IsTrue(cache.CacheSets[0].Count == 5);

            Assert.IsTrue(cache.GetValue(1) == null);
            Assert.IsTrue(cache.GetValue(2) == "string 2");
            Assert.IsTrue(cache.GetValue(3) == "string 3");
            Assert.IsTrue(cache.GetValue(4) == "string 4");
            Assert.IsTrue(cache.GetValue(5) == "string 5");
            Assert.IsTrue(cache.GetValue(6) == "string 6");
        }

        [TestMethod]
        public void LruEviction_AdvancedEvictionTest()
        {
            var cache = new DataCache<int, string>(1, 5);
            cache.SetEvictionProcessor(typeof(LruEvictionProcessor<int, string>), null);

            Assert.IsTrue(cache.ItemsPerSet == 5);
            Assert.IsTrue(cache.NumberOfSetsInCache == 1);
            Assert.IsTrue(cache.CacheSets.Length == 1);

            for (int i = 1; i <= 5; i++)
            {
                cache.TryAdd(i, "string " + i);
            }

            Assert.IsTrue(cache.CacheSets[0].Count == 5);

            // getting item 1 moves it up the stack and makes item 2 the least recently used.
            cache.GetValue(1); 

            cache.TryAdd(6, "string 6");

            Assert.IsTrue(cache.CacheSets[0].Count == 5);
                        
            Assert.IsTrue(cache.GetValue(1) == "string 1");
            Assert.IsTrue(cache.GetValue(2) == null);
            Assert.IsTrue(cache.GetValue(3) == "string 3");
            Assert.IsTrue(cache.GetValue(4) == "string 4");
            Assert.IsTrue(cache.GetValue(5) == "string 5");
            Assert.IsTrue(cache.GetValue(6) == "string 6");
        }
    }
}
