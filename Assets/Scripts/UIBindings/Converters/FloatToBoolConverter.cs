using System;
using UnityEngine;

namespace UIBindings
{
    public class FloatToBoolConverter : ConverterTwoWayBase<float, bool>
    {
        [Header("Float to bool settings")]
        public float StepValue = 0.5f;

        [Header("Bool to float settings")]
        public float FalseValue = 0f;
        public float TrueValue = 0f;

        public override float Convert(bool value)
        {
            return value ? TrueValue : FalseValue;
        }

        public override Boolean Convert(Single value )
        {
            return value >= StepValue;
        }
    }
}