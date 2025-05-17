using System;
using UnityEngine.UI;

namespace UIBindings
{
    public class SliderBinder : BinderTwoWayBase<float>
    {
        public Slider Slider;

        protected override void Awake( )
        {
            base.Awake();

            if ( !Slider )
                Slider = GetComponent<Slider>();

            InitSetter();
            
        }

        protected override void OnEnable( )
        {
            base.OnEnable();

            Slider.onValueChanged.AddListener( OnValueChange );
        }

        private void OnValueChange(Single value )
        {
            ProcessTargetToSource( value );
        }

        public override void ProcessSourceToTarget(Single value )
        {
            Slider.SetValueWithoutNotify( value );
        }
    }
}
