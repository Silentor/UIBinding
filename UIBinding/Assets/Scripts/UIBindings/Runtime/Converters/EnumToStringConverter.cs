using System;
using System.Collections.Generic;
using System.Globalization;
using UIBindings.Runtime;
using UnityEngine;

namespace UIBindings
{
    public class EnumToStringConverter<TEnum> : SimpleConverterOneWayBase<TEnum, string> where TEnum : struct, Enum
    {
        public override string Convert(TEnum value)
        {
            if ( _cachedValues.TryGetValue( value, out var strValue ) )
                return strValue;

            var convertedValue = Enum.GetName( typeof(TEnum), value );
            if ( convertedValue == null )
            {
                convertedValue = value.ToString( );       
            }
            _cachedValues.Add( value, convertedValue );
            return convertedValue;
        }

        private readonly Dictionary<TEnum, string> _cachedValues = new ();
    }
}