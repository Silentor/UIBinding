using System;
using UnityEngine;

namespace UIBindings
{
    public class IntToBoolConverter : ConverterTwoWayBase<int, bool>
    {
        [Header("Int to bool settings")]
        public int StepValue = 1;

        [Header("Bool to int settings")]
        public int FalseValue = 0;
        public int TrueValue = 1;

        public override int ConvertBack(bool value)
        {
            return value ? TrueValue : FalseValue;
        }

        public override Boolean Convert(int value )
        {
            return value >= StepValue;
        }
    }
}