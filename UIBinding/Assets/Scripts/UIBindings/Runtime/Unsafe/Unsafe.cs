using System;
using Unity.Collections.LowLevel.Unsafe;

namespace UIBindings.Runtime.Unsafe
{
    public static class Unsafe
    {
        public static TTo Convert<TFrom, TTo>(ref TFrom from)
            where TFrom : unmanaged
            where TTo   : unmanaged
        {
            unsafe
            {
                TTo to = default;
                var size = Math.Min(UnsafeUtility.SizeOf<TFrom>(), UnsafeUtility.SizeOf<TTo>());
                fixed (TFrom* pFrom = &from)
                {
                    UnsafeUtility.MemCpy(&to, pFrom, size);
                }
                return to;
            }
        }
    }
}
