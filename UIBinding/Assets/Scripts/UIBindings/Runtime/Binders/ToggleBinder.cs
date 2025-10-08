using System;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Object = System.Object;

namespace UIBindings
{
    public class ToggleBinder : BinderBase
    {
        public Toggle Toggle;
        public ValueBinding<bool> ValueBinding = new (){Settings = new DataBinding.UpdateMode(){Mode = DataBinding.EMode.TwoWay, Timing = DataBinding.ETiming.AfterLateUpdate}}; 

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
