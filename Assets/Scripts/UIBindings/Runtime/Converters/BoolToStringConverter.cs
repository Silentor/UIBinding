using System;
using UnityEngine;

namespace UIBindings
{
    public class BoolToStringConverter : SimpleConverterTwoWayBase<bool, String>
    {
        [Header("Bool to string settings")]        
        public string TrueString = "True";
        public string FalseString = "False";

        [Header("String to bool settings")]
        public bool ValueOnParseError = false;

        public override String Convert(bool value)
        {
            return value ? TrueString : FalseString;
        }

        public override Boolean ConvertBack(String value )
        {
            if ( String.Equals( value, TrueString, StringComparison.OrdinalIgnoreCase ) )
                return true;
            else if ( String.Equals( value, FalseString, StringComparison.OrdinalIgnoreCase ) )
                return false;

            return ValueOnParseError;
        }
    }
}