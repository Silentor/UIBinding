using System;
using System.Collections.Generic;

namespace UIBindings.Runtime.Utils
{
    public static class ListExtensions
    {
        public static bool TryFirst<T>(this IList<T> list, out T value)
        {
            if (list == null || list.Count == 0)
            {
                value = default;
                return false;
            }

            value = list[0];
            return true;
        }

        public static bool TryFirst<T>(this IList<T> list, Predicate<T> predicate, out T value)
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
    }
}