using System;
using System.Globalization;

namespace UIBindings
{
    public class FloatToIntConverter : ConverterTwoWayBase<float, int>
    {
        public override int Convert(float value)
        {
            return (int)Math.Round( value );
        }

        public override Single ConvertBack(Int32 value )
        {
            return value;
        }
    }
}