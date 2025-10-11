using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Object = System.Object;

namespace UIBindings
{
    public class SliderBinder : BinderBase
    {
        public Slider Slider;
        public ValueBinding<float>       ValueBinding;
        public ValueBinding<float>       MinValueBinding;     //Optional
        public ValueBinding<float>       MaxValueBinding;     //Optional

        protected void Awake( )
        {
            if ( !Slider )
                Slider = GetComponent<Slider>();
            Assert.IsTrue( Slider );

            ValueBinding.SetDebugInfo( this, nameof(ValueBinding) );
            ValueBinding.Init( GetSource(ValueBinding) );
            ValueBinding.SourceChanged += UpdateValue;

            MinValueBinding.SetDebugInfo( this, nameof(MinValueBinding) );
            MinValueBinding.Init( GetSource(MinValueBinding) );
            MinValueBinding.SourceChanged += UpdateMinValue;

            MaxValueBinding.SetDebugInfo( this, nameof(MaxValueBinding) );
            MaxValueBinding.Init( GetSource(MaxValueBinding) );
            MaxValueBinding.SourceChanged += UpdateMaxValue;
        }

        protected void OnEnable( )
        {
            ValueBinding.Subscribe( GetUpdateOrder() );
            MinValueBinding.Subscribe( GetUpdateOrder() );
            MaxValueBinding.Subscribe( GetUpdateOrder() );
            Slider.onValueChanged.AddListener( OnValueChange );
        }

        private void OnDisable( )
        {
            ValueBinding.Unsubscribe();
            MinValueBinding.Unsubscribe();
            MaxValueBinding.Unsubscribe();
            Slider.onValueChanged.RemoveListener( OnValueChange );
        }

#if UNITY_EDITOR
        private void Reset( )
        {
            ValueBinding.Settings.Mode = DataBinding.EMode.TwoWay;
            MinValueBinding.Enabled = false;
            MaxValueBinding.Enabled = false;
        }
#endif

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
