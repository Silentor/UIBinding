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
        public Binding<float>       MinValueBinding;
        public Binding<float>       MaxValueBinding;

        protected void Awake( )
        {
            if ( !Slider )
                Slider = GetComponent<Slider>();
            Assert.IsTrue( Slider );

            ValueBinding.Awake( this );
            ValueBinding.SourceChanged += UpdateValue;
            MinValueBinding.Awake( this );
            MinValueBinding.SourceChanged += UpdateMinValue;
            MaxValueBinding.Awake( this );
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

        private void LateUpdate( )
        {
            ValueBinding.CheckChanges();
            MinValueBinding.CheckChanges();
            MaxValueBinding.CheckChanges();
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
    }
}
