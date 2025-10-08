using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using Object = System.Object;

namespace UIBindings
{
    public class DropdownBinder : BinderBase
    {
        public TMP_Dropdown             Dropdown;
        public ValueBinding<int>        ValueBinding; 
        public CollectionBinding        OptionsBinding;

        protected void Awake( )
        {
            if ( !Dropdown )
                Dropdown = GetComponent<TMP_Dropdown>();
            Assert.IsTrue( Dropdown );

            ValueBinding.SetDebugInfo( this, nameof(ValueBinding) );
            ValueBinding.Init( GetSource(ValueBinding), forceOneWay: !Dropdown.interactable );
            ValueBinding.SourceChanged += ProcessValue;

            OptionsBinding.SetDebugInfo( this, nameof(OptionsBinding) );
            OptionsBinding.Init( GetSource(OptionsBinding) );
            OptionsBinding.CollectionChanged += ProcessOptionsChanged;
        }

        private void ProcessOptionsChanged(CollectionBinding sender, IReadOnlyList<Object> options )
        {
            Dropdown.options.Clear();
            if ( options != null )
            {
                foreach ( var option in options )
                {
                    if( option is string optionStr )
                        Dropdown.options.Add( new TMP_Dropdown.OptionData( optionStr ) );
                    else if( option is Sprite optionSprite )
                        Dropdown.options.Add( new TMP_Dropdown.OptionData( optionSprite ) );
                    else if( option is TMP_Dropdown.OptionData optionData )
                        Dropdown.options.Add( optionData );
                    else
                        Dropdown.options.Add( new TMP_Dropdown.OptionData( option.ToString() ) );
                }
            }

            Dropdown.SetValueWithoutNotify( _lastValue );
            Dropdown.RefreshShownValue();
        }

        private void OnEnable( )
        {
            ValueBinding.Subscribe( GetUpdateOrder() );
            OptionsBinding.Subscribe( GetUpdateOrder() );
            Dropdown.onValueChanged.AddListener( OnValueChanged );
        }

        private void OnDisable( )
        {
            ValueBinding.Unsubscribe();
            OptionsBinding.Unsubscribe();
            Dropdown.onValueChanged.RemoveListener( OnValueChanged );
        }

#if UNITY_EDITOR
        private void Reset( )
        {
            ValueBinding.Settings.Mode = DataBinding.EMode.TwoWay;
            OptionsBinding.Enabled = false;
        }
#endif

        private void OnValueChanged(Int32 newValue )
        {
            ValueBinding.SetValue( newValue );
        }

        private void ProcessValue(Object sender, Int32 v )
        {
            Dropdown.SetValueWithoutNotify( v );
            _lastValue = v;         //Need to store value because options list can be not set yet (if binded) and control just clamp value to 0
        }

        private int _lastValue = -1;
    }
}
