using System;
using System.Collections.Generic;

namespace UIBindings.Runtime.Utils
{
    public static class ListExtensions
    {
        public static bool TryFirst<T>(this IReadOnlyList<T> list, out T value)
        {
            if (list == null || list.Count == 0)
            {
                value = default;
                return false;
            }

            value = list[0];
            return true;
        }

        public static bool TryFirst<T>(this IReadOnlyList<T> list, Predicate<T> predicate, out T value)
        {
            if( list == null || predicate == null || list.Count == 0 )
            {
                value = default;
                return false;
            }

            for ( int i = 0; i < list.Count; i++ )
            {
                if ( predicate( list[ i ] ) )
                {
                    value = list[ i ];
                    return true;
                }    
            }
            
            value = default;
            return false;
        }

        public static bool Any<T>(this IReadOnlyList<T> list, Predicate<T> predicate)
        {
            if (list == null || predicate == null || list.Count == 0)
                return false;

            for (int i = 0; i < list.Count; i++)
            {
                if (predicate(list[i]))
                    return true;
            }

            return false;
        }

        public static string JoinToString<T>(this IReadOnlyList<T> list, string separator = ", ", int maxValuesToShow = -1)
        {
            if (list == null || list.Count == 0)
                return string.Empty;

            var result = new System.Text.StringBuilder();
            int count = maxValuesToShow > 0 && maxValuesToShow < list.Count ? maxValuesToShow : list.Count;
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                    result.Append(separator);
                result.Append(list[i]);
            }

            if (maxValuesToShow > 0 && maxValuesToShow < list.Count)
                result.Append($"... ({list.Count - maxValuesToShow} more)");

            return result.ToString();
        }

        public static string JoinToString<T>(this IEnumerable<T> list, string separator = ", ", int maxValuesToShow = -1)
        {
            if (list == null)
                return string.Empty;

            // If it's already an IReadOnlyList<T>, use the optimized version
            if (list is IReadOnlyList<T> readOnlyList)
                return JoinToString(readOnlyList, separator, maxValuesToShow);

            // Otherwise, enumerate manually
            var result = new System.Text.StringBuilder();
            int count = 0;
            int totalCount = 0;
            using (var enumerator = list.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (maxValuesToShow > 0 && count >= maxValuesToShow)
                    {
                        totalCount++;
                        continue;
                    }
                    if (count > 0)
                        result.Append(separator);
                    result.Append(enumerator.Current);
                    count++;
                    totalCount++;
                }
            }
            if (maxValuesToShow > 0 && totalCount > maxValuesToShow)
                result.Append($"... ({totalCount - maxValuesToShow} more)");
            return result.ToString();
        }
    }
}