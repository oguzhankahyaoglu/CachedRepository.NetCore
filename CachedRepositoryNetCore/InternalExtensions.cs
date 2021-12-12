using System;

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
            var delta = d.Ticks - modTicks;
            return new DateTime(dt.Ticks + delta, dt.Kind);
        }

        public static bool IsDefault<T>(this T parameter)
        {
            return parameter.Equals(default(T));
        }
    }
}
