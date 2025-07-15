using System;
using System.Collections.Generic;
using System.Globalization;
using UIBindings.Runtime;
using UnityEngine;

namespace UIBindings
{
    public class EnumToStringConverter : SimpleConverterOneWayBase<StructEnum, string>
    {
        public override string Convert(StructEnum value)
        {
            if ( _cachedValues.TryGetValue( value.Value, out var strValue ) )
                return strValue;

            var convertedValue = Enum.GetName( value.EnumType, value.Value );
            if ( convertedValue == null )
            {
                convertedValue = value.Value.ToString( CultureInfo.InvariantCulture );       
            }
            _cachedValues.Add( value.Value, convertedValue );
            return convertedValue;
        }

        private readonly Dictionary<int, string> _cachedValues = new ();
    }
}