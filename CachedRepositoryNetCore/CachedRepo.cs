using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LazyCache;

namespace CachedRepository.NetCore
{
    /// <summary>
    /// Runtime Cache'de kaydedilmek üzere, cached repo yazmak için kullanılabilr.
    /// Key'ler üzerinden ilgil entity'leri döner.
    /// </summary>
    /// <typeparam name="T">Repository'nin içerdiği entity tipi</typeparam>
    //public abstract class CachedRepo<T, TKey, TGetResult> : CachedRepoBase<List<T>>
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

            if (CachedEntities.AnyAndNotNull())
                return CachedEntities;

            IEnumerable<T> returnedData = null;
            try
            {
                returnedData = GetDataToBeCached();
                LastCachedItemDate = DateTime.Now;
            }
            catch (Exception e)
            {
                throw new Exception($"{GetType().Name} repo'sundan data çekilirken hata oluştu", e);
            }

            var data = returnedData.DefaultIfNull().ToList();
            if (!data.AnyAndNotNull())
                return CachedEntities.DefaultIfNull().ToList();

            CachedEntities = data;
            return data;
        }


        //protected virtual bool IsEntityMatchForKeys(T entity, TKey keysForFinding)
        //{
        //    throw new NotImplementedException(GetType() + " cachedRepo'nun IsEntityMatchForKeys metodu override edilemediği için, bu metod çağırılamaz.");
        //}

        //[DebuggerStepThrough]
        //public virtual T Get(bool hideExceptions, TKey keyForFinding)
        //{
        //    lock (LOCK)
        //    {
        //        var cachedEntities = GetCachedEntities();
        //        if (cachedEntities == null)
        //            throw new Exception(String.Format("{0} CachedEntities is null!", GetType()));

        //        var filtered = cachedEntities.Where(e => IsEntityMatchForKeys(e, keyForFinding)).ToArray();
        //        if (hideExceptions)
        //            return filtered.FirstOrDefault();

        //        var count = filtered.Length;
        //        if (count == 0)
        //            throw new Exception(String.Format("'{0}' key'leri için entity cachedDataSource'da bulunamadı", keyForFinding));
        //        if (count == 2)
        //            throw new Exception(String.Format("'{0}' key'leri için birden fazla entity cachedDataSource'da bulundu.", keyForFinding));
        //        return filtered.FirstOrDefault();
        //    }
        //}

        /*
         * REMOVE VE REMOVERAGNGE METODLARI CACHEDREPO ÖZELİNDE SAÇMA OLUYOR.
         * ÇÜNKÜ CACHE BOŞSA DATA SORUYOR; İÇİNDEKİ İTEM'I SİLERSEK DAVRANIŞSAL OLARAK BOZULACAKTIR SİSTEM
         *
         */
        //public virtual void Remove(Func<List<T>, T> findElementTobeRemoved)
        //{
        //    lock (LOCK)
        //    {
        //        if (!CachedEntities.AnyAndNotNull())
        //            return;
        //        var item = findElementTobeRemoved(CachedEntities);
        //        if (item != null)
        //            CachedEntities.Remove(item);
        //    }
        //}

        //public virtual void RemoveRange(Predicate<T> findElementTobeRemoved)
        //{
        //    lock (LOCK)
        //    {
        //        if (!CachedEntities.AnyAndNotNull())
        //            return;
        //        CachedEntities.RemoveAll(findElementTobeRemoved);
        //    }
        //}

        //protected abstract TGetResult ExtractValueFieldFromEntity(T entity);
        //public virtual TGetResult GetValue(bool hideExceptions, TKey keysForFinding)
        //{
        //    var entity = Get(hideExceptions, keysForFinding);
        //    return entity != null ? ExtractValueFieldFromEntity(entity) : null;
        //}

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
