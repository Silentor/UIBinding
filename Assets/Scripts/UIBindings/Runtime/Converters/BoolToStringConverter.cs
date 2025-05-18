using System;
using UnityEngine;

namespace UIBindings
{
    public class BoolToStringConverter : ConverterTwoWayBase<bool, String>
    {
        [Header("Two way settings")]        
        public string TrueString = "True";
        public string FalseString = "False";

        [Header("Bool to string settings")]
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