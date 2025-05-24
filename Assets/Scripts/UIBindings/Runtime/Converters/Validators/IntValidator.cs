using System;

namespace UIBindings
{
    public class IntValidator : ConverterOneWayBase<int, bool>
    {
        public int MinValue = 0;
        public int MaxValue = 100;

        public override bool Convert( int value )
        {
            return value >= MinValue && value <= MaxValue;
        }
    }
}