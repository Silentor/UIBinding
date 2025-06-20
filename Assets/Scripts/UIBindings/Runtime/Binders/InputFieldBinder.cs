using System;
using TMPro;
using UnityEngine.Assertions;
using Object = System.Object;

namespace UIBindings
{
    public class InputFieldBinder : BinderBase
    {
        public TMP_InputField           InputField;

        public ValueBindingRW<string>   ValueBinding;
        public ValueBinding<bool>       InteractableBinding;
        public ValueBinding<bool>       ReadonlyBinding;

        protected void Awake( )
        {
            if ( !InputField )
                InputField = GetComponent<TMP_InputField>();
            Assert.IsTrue( InputField );

            ValueBinding.SetDebugInfo( this, nameof(ValueBinding) );
            ValueBinding.Init( GetParentSource(), forceOneWay: !InputField.interactable );
            ValueBinding.SourceChanged += ProcessValue;

            InteractableBinding.SetDebugInfo( this, nameof(InteractableBinding) );
            InteractableBinding.Init( GetParentSource() );
            InteractableBinding.SourceChanged += ProcessInteractable;

            ReadonlyBinding.SetDebugInfo( this, nameof(ReadonlyBinding) );
            ReadonlyBinding.Init( GetParentSource() );
            ReadonlyBinding.SourceChanged += ProcessReadonly;
        }

        private void OnEnable( )
        {
            ValueBinding.Subscribe( GetUpdateOrder() );
            InteractableBinding.Subscribe( GetUpdateOrder() );
            ReadonlyBinding.Subscribe( GetUpdateOrder() );
            InputField.onValueChanged.AddListener( OnValueChanged );
        }

        private void OnDisable( )
        {
            ValueBinding.Unsubscribe();
            InteractableBinding.Unsubscribe();
            ReadonlyBinding.Unsubscribe();
            InputField.onValueChanged.RemoveListener( OnValueChanged );
        }

#if UNITY_EDITOR
        private void Reset( )
        {
            InteractableBinding.Enabled = false;
            ReadonlyBinding.Enabled = false;
        }
#endif

        private void OnValueChanged(String newValue )
        {
            ValueBinding.SetValue( newValue );
        }

        private void ProcessReadonly(Object sender, Boolean value )
        {
            InputField.readOnly = value;
        }

        private void ProcessInteractable(Object sender, Boolean value )
        {
            InputField.interactable = value;
        }

        private void ProcessValue(Object sender, String value )
        {
            InputField.SetTextWithoutNotify( value );
        }
    }
}
