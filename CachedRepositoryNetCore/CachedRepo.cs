﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    [Obsolete("Bir sonraki versiyonda senkron çalışan bütün repo base'leri kaldırılacaktır. Async yapıya geçmeniz gerekmektedir.")]
    public abstract class CachedRepo<T> : CachedRepoBase<List<T>>
        where T : class
        //where TGetResult : class
    {
        protected CachedRepo(IAppCache lazyCache) : base(lazyCache)
        {

        }


        #region Get Cached Entities (private)

        private List<T> CachedEntities
        {
            get => GetFromCache(GetCacheKey());
            set => SetCache(GetCacheKey(), value);
        }

        #endregion

        /// <summary>
        /// Cache'deki btün entityleri verir
        /// </summary>
        /// <returns></returns>
        [DebuggerStepThrough]
        public virtual List<T> GetCachedEntities(bool dontFetchData = false)
        {
            //Bu _ ve Clone metodları LOCK'tan kurtarmak için eklendi. Datayı aldıktan sonra ,CLONE ederken LOCK'dan ötürü beklemeye gerek yok!
            return _GetCachedEntities(dontFetchData);
        }

        private List<T> _GetCachedEntities(bool dontFetchData)
        {
            if (dontFetchData)
                return CachedEntities;

            if (CachedEntities?.Count > 0)
                return CachedEntities;

            List<T> returnedData;
            try
            {
                returnedData = GetDataToBeCached()?.ToList();
                LastCachedItemDate = DateTime.Now;
            }
            catch (Exception e)
            {
                throw new Exception($"{GetType().Name} repo'sundan data çekilirken hata oluştu", e);
            }

            if (returnedData == null || returnedData.Count == 0)
                return CachedEntities;

            CachedEntities = returnedData;
            return returnedData;
        }


        /// <summary>
        /// Cache'deki değerleri verilen parametre ile değiştirir
        /// </summary>
        /// <param name="value"></param>
        public virtual void SetCachedEntities(List<T> value)
        {
            CachedEntities = value;
        }

        /// <summary>
        /// Repo'nun bütün cache'ini boşaltır, bütün objeler silinir.
        /// </summary>
        public override void ReleaseCache()
        {
            CachedEntities = null;
        }

        public int GetCachedCount() => CachedEntities?.Count ?? 0;

        protected abstract IEnumerable<T> GetDataToBeCached();
    }
}
