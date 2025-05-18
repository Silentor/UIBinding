using System;
using UnityEngine;

namespace UIBindings
{
    public class UppercaseConverter : ConverterTwoWayBase<String, String>
    {
        public override String Convert(String value)
        {
            return value.ToUpper();
        }

        public override String ConvertBack(String value )
        {
            return value.ToUpper();
        }
    }
}