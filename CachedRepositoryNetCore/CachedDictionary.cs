using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LazyCache;

namespace CachedRepository
{
    #region Overloads of CachedDictionary

    /// <inheritdoc cref="CachedDictionary{TEntity,TKey}"/>
    public abstract class CachedDictionary<TEntity, TKey1, TKey2, TKey3, TKey4> : CachedDictionary<TEntity, Tuple<TKey1, TKey2, TKey3, TKey4>>
    //where TEntity : class
    {
        protected CachedDictionary(IAppCache  lazyCache) : base(lazyCache)
        {

        }

        internal override bool IsKeyCached(Tuple<TKey1, TKey2, TKey3, TKey4> key)
        {
            var cachedEntities = GetCachedEntities();
            return cachedEntities.Any(t => t.Key.Equals(key) || (t.Key.Item1.Equals(key.Item1) && t.Key.Item2.Equals(key.Item2) && t.Key.Item3.Equals(key.Item3) && t.Key.Item4.Equals(key.Item4)));
        }
    }

    /// <inheritdoc cref="CachedDictionary{TEntity,TKey}"/>
    public abstract class CachedDictionary<TEntity, TKey1, TKey2, TKey3> : CachedDictionary<TEntity, Tuple<TKey1, TKey2, TKey3>>
    //where TEntity : class
    {
        protected CachedDictionary(IAppCache lazyCache) : base(lazyCache)
        {

        }

        internal override bool IsKeyCached(Tuple<TKey1, TKey2, TKey3> key)
        {
            var cachedEntities = GetCachedEntities();
            return cachedEntities.Any(t => t.Key.Equals(key) || (t.Key.Item1.Equals(key.Item1) && t.Key.Item2.Equals(key.Item2) && t.Key.Item3.Equals(key.Item3)));
        }
    }

    /// <inheritdoc cref="CachedDictionary{TEntity,TKey}"/>
    public abstract class CachedDictionary<TEntity, TKey1, TKey2> : CachedDictionary<TEntity, Tuple<TKey1, TKey2>>
    //where TEntity : class
    {
        protected CachedDictionary(IAppCache lazyCache) : base(lazyCache)
        {

        }

        internal override bool IsKeyCached(Tuple<TKey1, TKey2> key)
        {
            var cachedEntities = GetCachedEntities();
            return cachedEntities.Any(t => t.Key.Equals(key) || (t.Key.Item1.Equals(key.Item1) && t.Key.Item2.Equals(key.Item2)));
        }
    }

    /// <inheritdoc cref="CachedDictionary{TEntity,TKey}"/>
    public abstract class CachedDictionary<TEntity> : CachedDictionary<TEntity, int>
    //where TEntity : class
    {
        protected CachedDictionary(IAppCache lazyCache) : base(lazyCache)
        {

        }

    }

    #endregion

    /// <summary>
    /// Runtime Cache'de kaydedilmek üzere, cached dictionary yazmak için kullanılabilr.
    /// Key'ler üzerinden ilgil entity'leri döner.
    /// </summary>
    /// <typeparam name="TEntity">Repository'nin içerdiği entity tipi</typeparam>
    /// <typeparam name="TKey">Bu entity'lerin Get edilirken key olarak kullanılcak alanların tipi. string yada int gibi bir tip olabilir.</typeparam>
    public abstract class CachedDictionary<TEntity, TKey> : CachedRepoBase<Dictionary<TKey, TEntity>>
    //where TEntity : class
    //where TGetResult : class
    {
        protected CachedDictionary(IAppCache lazyCache) : base(lazyCache)
        {

        }



        #region Get Cached Entities (private)

        private Dictionary<TKey, TEntity> CachedEntities
        {
            get
            {
                var cachedEntities = GetFromCache(GetCacheKey());
                if (cachedEntities != null)
                    return cachedEntities;
                cachedEntities = CreateCacheDictionary();
                SetCache(GetCacheKey(), cachedEntities);
                return cachedEntities;
            }
            set => SetCache(GetCacheKey(), value);
        }

        protected virtual Dictionary<TKey, TEntity> CreateCacheDictionary()
        {
            return new Dictionary<TKey, TEntity>();
        }

        #endregion

        public virtual void Remove(TKey key)
        {
            var cachedEntities = GetCachedEntities();
            if (cachedEntities == null)
                return;
            if (IsKeyCached(key))
                cachedEntities.Remove(key);
        }

        //[DebuggerStepThrough]
        public virtual TEntity Get(TKey keyForFinding, bool useCache = true)
        {
            //Bu _ ve Clone metodları LOCK'tan kurtarmak için eklendi. Datayı aldıktan sonra ,CLONE ederken LOCK'dan ötürü beklemeye gerek yok!
            return (_Get(keyForFinding, useCache));
        }

        private TEntity _Get(TKey keyForFinding, bool useCache)
        {
            if (CachedEntities == null)
                throw new Exception($"{GetType()} CachedEntities is null!");

            var result = default(TEntity);
            if (useCache && TryGetEntityFromCache(keyForFinding, out result))
                return result;

            //DebugLog($"Requesting data to be cached for key {keyForFinding}");
            try
            {
                result = GetDataToBeCached(keyForFinding);
                CachedRepoBase.LastCachedItemDate = DateTime.Now;
            }
            catch (Exception e)
            {
                throw new Exception($"{GetType().Name} repo'sundan data çekilirken hata oluştu", e);
                //case OnDataFetchExceptionBehaviour.HideExReturnNull:
                //    return default(TEntity);
            }

            /*
             *Şimdi şöyle birşey var, data null döndüyse yada hata aldıysa cache'e yazmamalı okey
             * Fakat boş array/list vb döndüyse de cache'lenmeli, dönmeseymiş kardeşim yannıt dönebiliyor demek ki...                 *
             */
            if (result == null)
            {
                return CreateDefaultResult(keyForFinding);
            }

            //cache clone yazılmalı her aman dışardan manipülasyına izin vermemek için
            locker.Wait();
            try
            {
                if (!CachedEntities.ContainsKey(keyForFinding))
                    CachedEntities.Add(keyForFinding, result);
            }
            finally
            {
                locker.Release();
            }

            return result;
        }

        private TEntity CreateDefaultResult(TKey keyForFinding)
        {
            /*
        Buradaki mevzu; TEntity bir int olabilir (valueType), bir productOlabilir (object) veya Product[] gibi bir tip olabilir (IEnumerable<Product>)
        Her durum için ilgili default değerleri hesaplamak lazım ve ilgili result'u dönmek gerek
        */

            //if (hideExceptions)
            //    return default(TEntity);
            //throw new ArgumentNullException("keyForFinding", $"{keyForFinding} key'ine karşılık source'dan null değeri döndü.");
            DebugLog($"null returned as data for key: {keyForFinding}");
            var type = typeof(TEntity);
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                var array = (TEntity[])Array.CreateInstance(type, 1);
                return array.First();
            }
            //else if (type.IsValueType)
            //    return default(TEntity);
            //else
            return default(TEntity); //null geldi reference type
        }

        /// <summary>
        /// Cache'den ilgili kaydı bulabilmesinin mantığı ezilebilsin diye protected yapıldı.
        /// </summary>
        /// <param name="keyForFinding"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private bool TryGetEntityFromCache(TKey keyForFinding, out TEntity result)
        {
            if (CachedEntities.TryGetValue(keyForFinding, out result))
            {
                if (!ShouldExpireEntityItem(keyForFinding, result))
                    return true;
                return false;
            }

            return false;
        }

        /// <summary>
        /// Cache'deki item'ın varlığını kontrol etmek ve bu mekanizmayı ezmek için metod virtual hale getirildi. 
        /// Belirli durumlarda, belirli item'ların expire olması gerekiyorsa bu metod ezilmeli.
        /// </summary>
        protected virtual bool ShouldExpireEntityItem(TKey key, TEntity outResult)
        {
            return false;
        }

        public virtual void Set(TKey key, TEntity value)
        {
            if (CachedEntities == null)
                return;
            if (IsKeyCached(key))
                CachedEntities[key] = value;
            else
                CachedEntities.Add(key, value);
        }

        /// <summary>
        /// tuple gibi birden fazla key verildiğinde, containskey metodu doğru çalışmyıor aynı tuple oluşturmamıza rağmen. ondan ötürü ezilmesi gerekiyor
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal virtual bool IsKeyCached(TKey key)
        {
            return CachedEntities.ContainsKey(key);
        }

        public void SetCachedEntities(Dictionary<TKey, TEntity> value)
        {
            CachedEntities = value;
        }

        public virtual Dictionary<TKey, TEntity> GetCachedEntities()
        {
            return CachedEntities;
        }

        public override void ReleaseCache()
        {
            CachedEntities = null;
        }

        protected abstract TEntity GetDataToBeCached(TKey key);
    }
}
