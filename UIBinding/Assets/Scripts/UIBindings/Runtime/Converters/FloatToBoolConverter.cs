using System;
using UnityEngine;

namespace UIBindings
{
    public class FloatToBoolConverter : SimpleConverterTwoWayBase<float, bool>
    {
        [Header("Float to bool settings")]
        public float StepValue = 0.5f;

        [Header("Bool to float settings")]
        public float FalseValue = 0f;
        public float TrueValue = 1f;

        public override float ConvertBack(bool value)
        {
            return value ? TrueValue : FalseValue;
        }

        public override Boolean Convert(Single value )
        {
            return value >= StepValue;
        }
    }
}