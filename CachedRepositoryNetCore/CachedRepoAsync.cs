using System;
using System.Diagnostics;
using System.Threading.Tasks;
using LazyCache;

namespace CachedRepository.NetCore
{
    /// <summary>
    /// Runtime Cache'de kaydedilmek üzere, cached repo yazmak için kullanılabilr.
    /// Key'ler üzerinden ilgil entity'leri döner.
    /// </summary>
    /// <typeparam name="T">Repository'nin içerdiği entity tipi</typeparam>
    //public abstract class CachedRepo<T, TKey, TGetResult> : CachedRepoBase<List<T>>
    public abstract class CachedRepoAsync<T> : CachedRepoBase<T[]>
        where T : class
        //where TGetResult : class
    {
        protected CachedRepoAsync(IAppCache lazyCache) : base(lazyCache)
        {

        }


        /// <summary>
        /// Cache'deki btün entityleri verir
        /// </summary>
        /// <returns></returns>
        [DebuggerStepThrough]
        public virtual Task<T[]> GetAsync(bool dontFetchData = false)
        {
            //Bu _ ve Clone metodları LOCK'tan kurtarmak için eklendi. Datayı aldıktan sonra ,CLONE ederken LOCK'dan ötürü beklemeye gerek yok!
            return _GetCachedEntities(dontFetchData);
        }

        private async Task<T[]> _GetCachedEntities(bool dontFetchData)
        {
            var cachedEntities = await GetFromCacheAsync(GetCacheKey());
            if (dontFetchData)
                return cachedEntities;

            if (cachedEntities?.Length > 0)
                return cachedEntities;

            T[] returnedData;
            try
            {
                returnedData = await GetDataToBeCached();
                LastCachedItemDate = DateTime.Now;
            }
            catch (Exception e)
            {
                throw new Exception($"{GetType().Name} repo'sundan data çekilirken hata oluştu", e);
            }

            if (returnedData == null || returnedData.Length == 0)
                return cachedEntities;

            SetCache(GetCacheKey(), returnedData);
            return returnedData;
        }

        /// <summary>
        /// Cache'deki değerleri verilen parametre ile değiştirir
        /// </summary>
        /// <param name="value"></param>
        public virtual void Set(T[] value)
        {
            SetCache(GetCacheKey(), value);
        }

        /// <summary>
        /// Repo'nun bütün cache'ini boşaltır, bütün objeler silinir.
        /// </summary>
        public override void ReleaseCache()
        {
            SetCache(GetCacheKey(), null);
        }

        public async Task<int> GetCachedCount()
        {
            var cachedEntities = await GetFromCacheAsync(GetCacheKey());
            return cachedEntities?.Length ?? 0;
        }

        protected abstract Task<T[]> GetDataToBeCached();
    }
}
