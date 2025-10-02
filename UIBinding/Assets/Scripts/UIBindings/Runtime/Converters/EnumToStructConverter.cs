using System;
using System.Runtime.CompilerServices;
using UIBindings.Runtime;
using Unity.Collections.LowLevel.Unsafe;

namespace UIBindings
{
    /// <summary>
    /// Special converter to convert any enum type to StructEnum and back. Must be generated for each enum type.
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    public class EnumToStructConverter<TEnum> : SimpleConverterTwoWayBase<TEnum, StructEnum> where TEnum : struct, Enum
    {
        public override StructEnum Convert(TEnum value )
        {
            if ( TryConvert( value, out int intValue ) )
            {
                var result = new StructEnum( intValue, typeof(TEnum) );
                return result;
            }
            throw new InvalidOperationException($"Cannot convert {typeof(TEnum).Name} to StructEnum.Value. Probably enum is too big for int");
        }

        public override TEnum ConvertBack(StructEnum value )
        {
            var intValue = value.Value;
            if ( TryConvert( intValue, out TEnum enumValue ) )
            {
                return enumValue;
            }
            throw new InvalidOperationException($"Cannot convert StructEnum.Value to {typeof(TEnum).Name}. Probably enum is too small for int");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvert<TFrom, TTo>(TFrom from, out TTo to)
                where TFrom : struct
                where TTo : struct
        {
            if (UnsafeUtility.SizeOf<TFrom>() <= UnsafeUtility.SizeOf<TTo>())
            {
                to = UnsafeUtility.As<TFrom, TTo>(ref from);
                return true;
            }
            to = default;
            return false;
        }
    }
}