using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;

namespace CachedRepository.NetCore
{
    public abstract class CachedRepoBase
    {
        public static Func<DateTime> DefaultExpireDate = () => DateTime.Now.RoundUp(TimeSpan.FromDays(31));

        public static DateTime? LastCachedItemDate { get; protected set; } = null;

        /// <summary>
        /// aynı günün 23:59:00 da expire olması
        /// </summary>
        public static Func<DateTime> DefaultExpireDate1Day = () => DateTime.Now.AddMonths(1).RoundUp(TimeSpan.FromDays(1));

        public static Func<DateTime> DefaultExpireDate5Min = () => DateTime.Now.RoundUp(TimeSpan.FromMinutes(5));

        public static Func<DateTime> DefaultExpireDate10Min = () => DateTime.Now.RoundUp(TimeSpan.FromMinutes(10));

        public static Func<DateTime> DefaultExpireDate1Hour = () => DateTime.Now.RoundUp(TimeSpan.FromHours(1));

        public static Func<int, DateTime> DefaultExpireDateXHours = (hours) => DateTime.Now.AddHours(hours).RoundUp(TimeSpan.FromHours(1));

        protected CachedRepoBase(IAppCache lazyCache)
        {
            _LazyCache = lazyCache;
        }

        protected static readonly SemaphoreSlim locker = new SemaphoreSlim(1, 1);
        protected IAppCache _LazyCache;

        protected CacheItemPriority DefaultCacheItemPriority = CacheItemPriority.Normal;

        protected MemoryCacheEntryOptions CacheItemPolicyDefault => new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = GetCacheExpireDate(),
            //Priority = (System.Runtime.Caching.CacheItemPriority) CacheItemPriority.AboveNormal,
            Priority = DefaultCacheItemPriority,
            PostEvictionCallbacks = {
                new PostEvictionCallbackRegistration
                {
                    EvictionCallback = delegate(object key, object value, EvictionReason reason, object state)
                    {
                        Debug.WriteLine($"[CachedDataSourceBase] Cache ({key}: +{value}) Removed: {reason} State: {state}");
                    }
                }
            }
        };

        protected virtual DateTime GetCacheExpireDate()
        {
            var expiration = DefaultExpireDate();
            return expiration;
        }

        protected void DebugLog(string msg)
        {
            Debug.WriteLine($"[CACHEDREPO-{GetType().Name}] {msg}");
        }

        public abstract void ReleaseCache();
    }

    public abstract class CachedRepoBase<T> : CachedRepoBase
        where T : class
    {
        protected CachedRepoBase(IAppCache lazyCache) : base(lazyCache)
        {

        }

        protected virtual string GetCacheKey()
        {
            return "CachedRepoBase-" + GetType().FullName;
        }

        protected async Task<T> GetFromCacheAsync(String key)
        {
            var cachedItem = await _LazyCache.GetAsync<T>(key);
            return cachedItem;
        }

        /// <summary>
        /// Runtime Cache'e yazmakla görevlidir. Default davranışı 15 gün cache'de kalacak ve NotRemovable olacak şekilde cache'lemektir.
        /// Davranışını değiştirmek için extend edilmelidir.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        protected void SetCache(String key, T value)
        {
            if (value == null)
                _LazyCache.Remove(key);
            else
            {
                //var cacheDependency = GetCacheDependency();
                DebugLog($"Set new data, Repo expire date: {CacheItemPolicyDefault.AbsoluteExpiration}");
                _LazyCache.Add(key, value, CacheItemPolicyDefault);
                //HttpRuntime.Cache.Add(key, value, cacheDependency, expiration, Cache.NoSlidingExpiration,
                //DefaultCacheItemPriority, CacheItemRemovedCallback);
            }
        }

    }
}
