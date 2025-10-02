using System;
using System.Runtime.CompilerServices;
using Silentor.Runtime.Utils;
using UIBindings.Runtime;
using UIBindings.Runtime.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace UIBindings.Converters
{
    public class ImplicitEnumConverter<TEnum> : DataProvider, IDataReadWriter<StructEnum>, IDataReadWriter<int>, IDataReadWriter<byte>, IDataReadWriter<long> where TEnum : unmanaged, Enum
    {
        public override Boolean IsTwoWay  => true;
        public override Type    InputType => typeof(TEnum);

        private readonly IDataReader<TEnum> _reader;
        private readonly IDataReadWriter<TEnum> _writer;

        public ImplicitEnumConverter( DataProvider source )
        {
            _reader = (IDataReader<TEnum>)source;
            _writer = source as IDataReadWriter<TEnum>;
        }

        public EResult TryGetValue( out Int32 value )
        {
            var result = _reader.TryGetValue( out var sourceValue );
            if ( result != EResult.NotChanged )
            {
                value = ConvertStructs<TEnum, long>( sourceValue ).ClampToInt32();
            }
            else
                value = 0;
            return result;
        }

        public EResult TryGetValue( out Byte value )
        {
            var result = _reader.TryGetValue( out var sourceValue );
            if ( result != EResult.NotChanged )
            {
                value = ConvertStructs<TEnum, long>( sourceValue ).ClampToByte();
            }
            else
                value = 0;
            return result;
        }

        public EResult TryGetValue( out Int64 value )
        {            
            var result = _reader.TryGetValue( out var sourceValue );
            if ( result != EResult.NotChanged )
            {
                value = ConvertStructs<TEnum, long>( sourceValue );
            }
            else
                value = 0;
            return result;
        }

        public EResult TryGetValue( out StructEnum value )
        {
            var result = _reader.TryGetValue( out var sourceValue );
            if ( result != EResult.NotChanged )
            {
                var intValue = ConvertStructs<TEnum, long>( sourceValue ).ClampToInt32();
                value = new StructEnum(intValue, typeof(TEnum));
            }
            else
                value = new StructEnum(0, typeof(TEnum));
            return result;
        }


        public void SetValue( Int32 value )
        {
            _writer.SetValue( ConvertStructs<int, TEnum>( value ) );
        }

        public void SetValue( Byte value )
        {
            _writer.SetValue( ConvertStructs<byte, TEnum>( value ) );
        }

        public void SetValue(Int64 value )
        {
            _writer.SetValue( ConvertStructs<Int64, TEnum>( value ) );
        }

        public void SetValue( StructEnum value )
        {
            _writer.SetValue( ConvertStructs<int, TEnum>( value.Value ) );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TTo ConvertStructs<TFrom, TTo>( TFrom from )
                where TFrom : unmanaged
                where TTo : unmanaged
        {
            if (UnsafeUtility.SizeOf<TFrom>() <= UnsafeUtility.SizeOf<TTo>())
                return UnsafeUtility.As<TFrom, TTo>(ref from);

            return Unsafe.Convert<TFrom, TTo>( ref from );
        }
    }
}