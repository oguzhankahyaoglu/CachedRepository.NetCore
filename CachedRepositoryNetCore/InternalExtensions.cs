using System;
using System.Collections.Generic;
using System.Linq;

namespace CachedRepository.NetCore
{
    internal static class InternalExtensions
    {
        /// <summary>
        /// Verilen tarihi, verilen timespan ile verilen en yakın tarihe yuvarlar, örneğin  her 10 dk'da bir gibi.
        /// var roundedUp = date.RoundUp(TimeSpan.FromMinutes(15)); // 2010/02/05 10:45:00
        /// </summary>
        public static DateTime RoundUp(this DateTime dt, TimeSpan d)
        {
            var modTicks = dt.Ticks % d.Ticks;
            var delta = modTicks != 0 ? d.Ticks - modTicks : 0;
            return new DateTime(dt.Ticks + delta, dt.Kind);
        }

        public static bool AnyAndNotNull<T>(this IEnumerable<T> enumerable)
        {
            return enumerable != null && enumerable.Any();
        }
        /// <summary>
        /// gereksiz nullcheck'ten kurtarmak için. defaultValue parametresi verilirse, o zaman null yada 0 elemanlı olması durumunda bu değer dönecektir
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="coll"></param>
        /// <returns></returns>
        public static IEnumerable<T> DefaultIfNull<T>(this IEnumerable<T> coll, IEnumerable<T> defaultValue = null)
        {
            if (coll.AnyAndNotNull())
                return coll;
            if (defaultValue.AnyAndNotNull())
                return defaultValue;
            return Enumerable.Empty<T>();
        }

        /// <summary>
        /// gereksiz nullcheck'ten kurtarmak için. defaultValue parametresi verilirse, o zaman null yada 0 elemanlı olması durumunda bu değer dönecektir
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="coll"></param>
        /// <returns></returns>
        public static List<T> DefaultIfNullToList<T>(this IEnumerable<T> coll, IEnumerable<T> defaultValue = null)
        {
            return DefaultIfNull(coll, defaultValue).ToList();
        }

        /// <summary>
        /// gereksiz nullcheck'ten kurtarmak için. defaultValue parametresi verilirse, o zaman null yada 0 elemanlı olması durumunda bu değer dönecektir
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="coll"></param>
        /// <returns></returns>
        public static T[] DefaultIfNullToArray<T>(this IEnumerable<T> coll, IEnumerable<T> defaultValue = null)
        {
            return DefaultIfNull(coll, defaultValue).ToArray();
        }

    }
}
