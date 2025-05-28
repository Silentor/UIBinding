using System;

namespace Silentor.Runtime.Utils
{
    public static class ClampExtension
    {
        public static Byte ClampToByte( this int value )
        {
            return (Byte)Math.Clamp( value, Byte.MinValue, Byte.MaxValue );
        }

        public static SByte ClampToSByte( this int value )
        {
            return (SByte)Math.Clamp( value, SByte.MinValue, SByte.MaxValue );
        }

        public static UInt16 ClampToUInt16( this int value )
        {
            return (UInt16)Math.Clamp( value, UInt16.MinValue, UInt16.MaxValue );
        }

        public static Int16 ClampToInt16( this int value )
        {
            return (Int16)Math.Clamp( value, Int16.MinValue, Int16.MaxValue );
        }

        public static UInt32 ClampToUInt32( this int value )
        {
            return (UInt32)Math.Clamp( value, UInt32.MinValue, UInt32.MaxValue );
        }

        public static UInt64 ClampToUInt64( this int value )
        {
            return (UInt64)Math.Clamp( value, 0, Int32.MaxValue );
        }

        public static Byte ClampToByte( this long value )
        {
            return (Byte)Math.Clamp( value, Byte.MinValue, Byte.MaxValue );
        }

        public static Int32 ClampToInt32( this long value )
        {
            return (Int32)Math.Clamp( value, Int32.MinValue, Int32.MaxValue );
        }

        public static Int32 ClampToInt32( this ulong value )
        {
            return (Int32)Math.Clamp( value, ulong.MinValue, Int32.MaxValue );
        }

        public static Int32 ClampToInt32( this uint value )
        {
            return (Int32)Math.Clamp( value, Int32.MinValue, Int32.MaxValue );
        }

        public static Byte ClampToByte( this float value )
        {
            return (Byte)Math.Clamp( value, Byte.MinValue, Byte.MaxValue );
        }

        public static Int32 ClampToInt32( this float value )
        {
            return (Int32)Math.Clamp( value, Int32.MinValue, Int32.MaxValue );
        }

        public static Int64 ClampToInt64( this float value )
        {
            return (Int64)Math.Clamp( value, Int64.MinValue, Int64.MaxValue );
        }

        public static Int32 ClampToInt32( this double value )
        {
            return (Int32)Math.Clamp( value, Int32.MinValue, Int32.MaxValue );
        }

        public static Int64 ClampToInt64( this double value )
        {
            return (Int64)Math.Clamp( value, Int64.MinValue, Int64.MaxValue );
        }

        public static float ClampToFloat( this double value )
        {
            return (float)Math.Clamp( value, float.MinValue, float.MaxValue );
        }

        public static Byte ClampToByte( this double value )
        {
            return (Byte)Math.Clamp( value, Byte.MinValue, Byte.MaxValue );
        }

        // public static TEnum ClampToEnum<TEnum>( this int value ) where TEnum : System.Enum
        // {
        //     if ( Enum.IsDefined( typeof(TEnum), value ) )
        //         return (TEnum)Enum.ToObject( typeof(TEnum), value );
        //
        //     if ( Enum.GetUnderlyingType( typeof(TEnum) ) == typeof(int) )
        //     {
        //         var values = Enum.GetValues( typeof(TEnum) );
        //         var minDiff = int.MaxValue;
        //         foreach ( var enumValue in values )
        //         {
        //             var intValue = (int)enumValue;
        //             var diff = Math.Abs( intValue - value );
        //             if( diff < minDiff )
        //             {
        //                 minDiff = diff;
        //             }
        //         }
        //     }
        //     
        //
        // }
    }
}