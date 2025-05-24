using System;
using System.Globalization;
using UnityEngine;

namespace UIBindings
{
    public class IntToStringConverter : ConverterTwoWayBase<int, string>
    {
        [Header("String to int settings")]
        public int ValueOnParseError = 0;
        //TODO add culture settings

        public override string Convert(int value)
        {
            return value.ToString( CultureInfo.InvariantCulture );
        }

        public override Int32 ConvertBack(String value )
        {
            if ( Int32.TryParse( value, out var result ) )
                return result;

            return ValueOnParseError;
        }
    }
}