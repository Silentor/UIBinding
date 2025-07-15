using System;

namespace UIBindings
{
    public class WrapFloatConverter : SimpleConverterOneWayBase<float, float>
    {
        public float MinValue = 0; 
        public float MaxValue = 1; 

        public override float Convert(float value )
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