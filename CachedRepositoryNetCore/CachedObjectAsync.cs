using System;
using System.Threading.Tasks;
using LazyCache;

namespace CachedRepository.NetCore
{
    /// <summary>
    /// Runtime Cache'de kaydedilmek üzere, cached repo yazmak için kullanılabilr.
    /// Key'ler üzerinden ilgil entity'leri döner.
    /// CachedRepo'dan farklı olarak, bir koleksiyon değil, direkt olarak bir obje'yi cache'de tutar/yönetir
    /// </summary>
    /// <typeparam name="T">Repository'nin içerdiği entity tipi</typeparam>
    public abstract class CachedObjectAsync<T> : CachedRepoBase<T>
    {
        /// <summary>
        /// If you set this flag to false, I will stop retrying to retrieve value since I will cache the default value that you have provided in abstract method.
        /// </summary>
        public bool ShouldTryAgainForDefaultValuesFromDataSource = true;

        protected CachedObjectAsync(IAppCache lazyCache) : base(lazyCache)
        {

        }

        public virtual async Task<T> GetAsync()
        {
            var result = await _LazyCache.GetOrAddAsync(GetCacheKey(), async (entry) =>
            {
                try
                {
                    entry.Priority = DefaultCacheItemPriority;
                    var cached = await GetDataToBeCached();
                    if (cached.IsDefault() && ShouldTryAgainForDefaultValuesFromDataSource)
                        return default(T);
                    LastCachedItemDate = DateTime.Now;
                    return cached;
                }
                catch (Exception e)
                {
                    throw new Exception($"{GetType().Name} repo'sundan data çekilirken hata oluştu", e);
                }
            });
            return result;
        }

        public virtual void Set(T value)
        {
            SetCache(GetCacheKey(), value);
        }

        public override void ReleaseCache()
        {
            SetCache(GetCacheKey(), default(T));
        }

        protected abstract Task<T> GetDataToBeCached();
    }
}
