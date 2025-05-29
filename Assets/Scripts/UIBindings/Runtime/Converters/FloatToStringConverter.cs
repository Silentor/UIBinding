using System;
using System.Globalization;
using UnityEngine;

namespace UIBindings
{
    public class FloatToStringConverter : SimpleConverterTwoWayBase<float, string>
    {
        [Header("Float to string settings")]
        public float ValueOnParseError = 0;
        //TODO add culture settings

        public override string Convert(float value)
        {
            return value.ToString( CultureInfo.CurrentCulture );
        }

        public override Single ConvertBack(String value )
        {
            if ( Single.TryParse( value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result ) )
                return result;

            return ValueOnParseError;
        }
    }
}