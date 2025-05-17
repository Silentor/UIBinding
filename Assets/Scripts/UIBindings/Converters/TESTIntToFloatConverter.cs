using System;
using System.Globalization;

namespace UIBindings
{
    public class TESTIntToFloatConverter : ConverterTwoWayBase<int, float>
    {
        public override int Convert(float value)
        {
            return (int)Math.Round( value );
        }

        public override Single Convert(Int32 value )
        {
            return value;
        }
    }
}