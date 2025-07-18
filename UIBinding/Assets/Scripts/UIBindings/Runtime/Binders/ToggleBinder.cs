using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Object = System.Object;

namespace UIBindings
{
    public class ToggleBinder : BinderBase
    {
        public Toggle Toggle;
        public ValueBindingRW<bool> ValueBinding; 

        protected void Awake( )
        {
            if ( !Toggle )
                Toggle = GetComponent<Toggle>();
            Assert.IsTrue( Toggle );

            ValueBinding.SetDebugInfo( this, nameof(ValueBinding) );
            ValueBinding.Init( GetSource(ValueBinding), forceOneWay: !Toggle.interactable );
            ValueBinding.SourceChanged += ProcessSourceToTarget;
        }

        private void OnEnable( )
        {
            ValueBinding.Subscribe( GetUpdateOrder() );
            Toggle.onValueChanged.AddListener( OnToggleValueChanged );
        }

        private void OnDisable( )
        {
            ValueBinding.Unsubscribe();
            Toggle.onValueChanged.RemoveListener( OnToggleValueChanged );
        }

        private void OnToggleValueChanged(Boolean newValue )
        {
            ValueBinding.SetValue( newValue );
        }

        private void ProcessSourceToTarget(Object sender, Boolean value )
        {
            Toggle.SetIsOnWithoutNotify( value );
        }
    }
}
