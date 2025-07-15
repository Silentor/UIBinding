using System;

namespace UIBindings
{
    public class WrapIntConverter : SimpleConverterOneWayBase<int, int>
    {
        public int MinValue = 0; 
        public int MaxValue = 1; 

        public override Int32 Convert(Int32 value )
        {
            if ( value < MinValue )
            {
                return MaxValue - ( MinValue - value ) % ( MaxValue - MinValue );
            }
            else if ( value > MaxValue )
            {
                return MinValue + ( value - MaxValue ) % ( MaxValue - MinValue );
            }

            return value;
        }
    }
}