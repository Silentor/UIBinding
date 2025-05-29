using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Object = System.Object;

namespace UIBindings
{
    public class SliderBinder : MonoBehaviour
    {
        public Slider Slider;
        public BindingTwoWay<float> ValueBinding;
        public Binding<float>       MinValueBinding     = new (){Enabled = false};
        public Binding<float>       MaxValueBinding     = new (){Enabled = false};

        protected void Awake( )
        {
            if ( !Slider )
                Slider = GetComponent<Slider>();
            Assert.IsTrue( Slider );

            ValueBinding.SetDebugInfo( this, nameof(ValueBinding) );
            ValueBinding.Awake(  );
            ValueBinding.SourceChanged += UpdateValue;
            MinValueBinding.SetDebugInfo( this, nameof(MinValueBinding) );
            MinValueBinding.Awake( );
            MinValueBinding.SourceChanged += UpdateMinValue;
            MaxValueBinding.SetDebugInfo( this, nameof(MaxValueBinding) );
            MaxValueBinding.Awake(  );
            MaxValueBinding.SourceChanged += UpdateMaxValue;
        }

        protected void OnEnable( )
        {
            ValueBinding.Subscribe();
            MinValueBinding.Subscribe();
            MaxValueBinding.Subscribe();
            Slider.onValueChanged.AddListener( OnValueChange );
        }

        private void OnDisable( )
        {
            ValueBinding.Unsubscribe();
            MinValueBinding.Unsubscribe();
            MaxValueBinding.Unsubscribe();
            Slider.onValueChanged.RemoveListener( OnValueChange );
        }

        private void OnValueChange(Single value )
        {
            ValueBinding.SetValue( value );
        }

        private void UpdateMaxValue(Object sender, Single value )
        {
            Slider.maxValue = value;
        }

        private void UpdateValue(Object sender, Single value )
        {
            Slider.SetValueWithoutNotify( value );
        }

        private void UpdateMinValue(Object sender, Single value )
        {
            Slider.minValue = value;
        }

        // private void OnValidate( )
        // {
        //     var slider = Slider || GetComponent<Slider>();
        //     if( slider )
        //         ValueBinding.OverrideOneWayMode = !Slider.interactable;
        // }
    }
}
