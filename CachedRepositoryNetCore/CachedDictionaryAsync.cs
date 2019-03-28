using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;

namespace CachedRepository.NetCore
{
    #region Overloads of CachedDictionary

    /// <inheritdoc cref="CachedDictionary{TEntity,TKey}"/>
    public abstract class CachedDictionaryAsync<TEntity, TKey1, TKey2, TKey3, TKey4> : CachedDictionaryAsync<TEntity, Tuple<TKey1, TKey2, TKey3, TKey4>>
    //where TEntity : class
    {
        protected CachedDictionaryAsync(IAppCache lazyCache) : base(lazyCache)
        {

        }

        internal override async Task<bool> IsKeyCachedAsync(Tuple<TKey1, TKey2, TKey3, TKey4> key)
        {
            var cachedEntities = await GetCachedDictionaryAsync();
            return cachedEntities.Any(t => t.Key.Equals(key) || (t.Key.Item1.Equals(key.Item1) && t.Key.Item2.Equals(key.Item2) && t.Key.Item3.Equals(key.Item3) && t.Key.Item4.Equals(key.Item4)));
        }
    }

    /// <inheritdoc cref="CachedDictionary{TEntity,TKey}"/>
    public abstract class CachedDictionaryAsync<TEntity, TKey1, TKey2, TKey3> : CachedDictionaryAsync<TEntity, Tuple<TKey1, TKey2, TKey3>>
    //where TEntity : class
    {
        protected CachedDictionaryAsync(IAppCache lazyCache) : base(lazyCache)
        {

        }

        internal override async Task<bool> IsKeyCachedAsync(Tuple<TKey1, TKey2, TKey3> key)
        {
            var cachedEntities = await GetCachedDictionaryAsync();
            return cachedEntities.Any(t => t.Key.Equals(key) || (t.Key.Item1.Equals(key.Item1) && t.Key.Item2.Equals(key.Item2) && t.Key.Item3.Equals(key.Item3)));
        }
    }

    /// <inheritdoc cref="CachedDictionary{TEntity,TKey}"/>
    public abstract class CachedDictionaryAsync<TEntity, TKey1, TKey2> : CachedDictionaryAsync<TEntity, Tuple<TKey1, TKey2>>
    //where TEntity : class
    {
        protected CachedDictionaryAsync(IAppCache lazyCache) : base(lazyCache)
        {

        }

        internal override async Task<bool> IsKeyCachedAsync(Tuple<TKey1, TKey2> key)
        {
            var cachedEntities = await GetCachedDictionaryAsync();
            return cachedEntities.Any(t => t.Key.Equals(key) || (t.Key.Item1.Equals(key.Item1) && t.Key.Item2.Equals(key.Item2)));
        }
    }

    /// <inheritdoc cref="CachedDictionary{TEntity,TKey}"/>
    public abstract class CachedDictionaryAsync<TEntity> : CachedDictionaryAsync<TEntity, int>
    //where TEntity : class
    {
        protected CachedDictionaryAsync(IAppCache lazyCache) : base(lazyCache)
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
    public abstract class CachedDictionaryAsync<TEntity, TKey> : CachedRepoBase<Dictionary<TKey, TEntity>>
    //where TEntity : class
    //where TGetResult : class
    {
        /// <summary>
        /// If you set this flag to false, I will stop retrying to retrieve value since I will cache the default value that you have provided in abstract method.
        /// </summary>
        public bool ShouldTryAgainForDefaultValuesFromDataSource = true;

        protected CachedDictionaryAsync(IAppCache lazyCache) : base(lazyCache)
        {
        }


        #region Get Cached Entities (private)

        //private Dictionary<TKey, TEntity> CachedEntities
        //{
        //    get
        //    {
        //        var cachedEntities = GetFromCache(GetCacheKey());
        //        if (cachedEntities != null)
        //            return cachedEntities;
        //        cachedEntities = CreateCacheDictionary();
        //        SetCache(GetCacheKey(), cachedEntities);
        //        return cachedEntities;
        //    }
        //    set => SetCache(GetCacheKey(), value);
        //}

        private async Task<Dictionary<TKey, TEntity>> _GetCachedDictionaryAsync()
        {
            var cachedEntities = await GetFromCacheAsync(GetCacheKey());
            if (cachedEntities != null)
                return cachedEntities;
            cachedEntities = CreateCacheDictionary();
            SetCache(GetCacheKey(), cachedEntities);
            return cachedEntities;
        }

        protected virtual Dictionary<TKey, TEntity> CreateCacheDictionary()
        {
            return new Dictionary<TKey, TEntity>();
        }

        #endregion

        public virtual async Task RemoveAsync(TKey key)
        {
            var cachedEntities = await GetCachedDictionaryAsync();
            if (cachedEntities == null)
                return;
            if (await IsKeyCachedAsync(key))
                cachedEntities.Remove(key);
        }

        //[DebuggerStepThrough]
        public virtual async Task<TEntity> GetAsync(TKey keyForFinding, bool useCache = true)
        {
            var dictionary = await _GetCachedDictionaryAsync();
            if (dictionary == null)
                throw new Exception($"{GetType()} CachedEntities is null!");

            if (useCache)
            {
                var tryResult = await TryGetEntityFromCache(keyForFinding);
                if (tryResult.Item1)
                    return tryResult.Item2;
            }

            //DebugLog($"Requesting data to be cached for key {keyForFinding}");
            TEntity result;
            try
            {
                result = await GetDataToBeCached(keyForFinding);
                LastCachedItemDate = DateTime.Now;
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
            if (result.IsDefault())
            {
                return CreateDefaultResult(keyForFinding);
            }

            //cache clone yazılmalı her aman dışardan manipülasyına izin vermemek için
            locker.Wait();
            try
            {
                if (!dictionary.ContainsKey(keyForFinding))
                    dictionary.Add(keyForFinding, result);
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
        private async Task<(bool, TEntity)> TryGetEntityFromCache(TKey keyForFinding)
        {
            var dictionary = await _GetCachedDictionaryAsync();
            if (dictionary.TryGetValue(keyForFinding, out var result))
            {
                if (ShouldTryAgainForDefaultValuesFromDataSource && result.IsDefault())
                    return (false, result);

                if (!ShouldExpireEntityItem(keyForFinding, result))
                    return (true, result);
                return (false, result);
            }

            return (false, result);
        }

        /// <summary>
        /// Cache'deki item'ın varlığını kontrol etmek ve bu mekanizmayı ezmek için metod virtual hale getirildi. 
        /// Belirli durumlarda, belirli item'ların expire olması gerekiyorsa bu metod ezilmeli.
        /// </summary>
        protected virtual bool ShouldExpireEntityItem(TKey key, TEntity outResult)
        {
            return false;
        }

        public virtual async Task SetAsync(TKey key, TEntity value)
        {
            var dictionary = await _GetCachedDictionaryAsync();
            if (dictionary == null)
                return;
            if (await IsKeyCachedAsync(key))
                dictionary[key] = value;
            else
                dictionary.Add(key, value);
        }

        /// <summary>
        /// tuple gibi birden fazla key verildiğinde, containskey metodu doğru çalışmyıor aynı tuple oluşturmamıza rağmen. ondan ötürü ezilmesi gerekiyor
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal virtual async Task<bool> IsKeyCachedAsync(TKey key)
        {
            var dictionary = await _GetCachedDictionaryAsync();
            return dictionary.ContainsKey(key);
        }

        public void SetDictionary(Dictionary<TKey, TEntity> value)
        {
            SetCache(GetCacheKey(), value);
        }

        public virtual async Task<Dictionary<TKey, TEntity>> GetCachedDictionaryAsync()
        {
            var dictionary = await _GetCachedDictionaryAsync();
            return dictionary;
        }

        public override void ReleaseCache()
        {
            SetCache(GetCacheKey(), null);
        }

        protected abstract Task<TEntity> GetDataToBeCached(TKey key);
    }
}
