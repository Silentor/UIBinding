using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Object = System.Object;

namespace UIBindings
{
    public class ToggleBinder : MonoBehaviour
    {
        public Toggle Toggle;
        public BindingTwoWay<bool> ValueBinding; 

        protected void Awake( )
        {
            if ( !Toggle )
                Toggle = GetComponent<Toggle>();
            Assert.IsTrue( Toggle );

            ValueBinding.SetDebugInfo( this, nameof(ValueBinding) );
            ValueBinding.Awake(  );
            ValueBinding.SourceChanged += ProcessSourceToTarget;
            
        }

        private void OnEnable( )
        {
            ValueBinding.Subscribe();
            Toggle.onValueChanged.AddListener( OnToggleValueChanged );
        }

        private void OnDisable( )
        {
            ValueBinding.Unsubscribe();
            Toggle.onValueChanged.RemoveListener( OnToggleValueChanged );
        }

        private void LateUpdate( )
        {
            ValueBinding.CheckChanges();
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
