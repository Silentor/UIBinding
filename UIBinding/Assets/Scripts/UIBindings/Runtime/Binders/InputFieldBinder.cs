using System;
using TMPro;
using UnityEngine.Assertions;
using Object = System.Object;

namespace UIBindings
{
    public class InputFieldBinder : BinderBase
    {
        public TMP_InputField           InputField;

        public ValueBinding<string>     ValueBinding;
        public ValueBinding<bool>       InteractableBinding;
        public ValueBinding<bool>       ReadonlyBinding;

        protected void Awake( )
        {
            if ( !InputField )
                InputField = GetComponent<TMP_InputField>();
            Assert.IsTrue( InputField );

            ValueBinding.SetDebugInfo( this, nameof(ValueBinding) );
            ValueBinding.Init( GetSource(ValueBinding) );
            ValueBinding.SourceChanged += ProcessValue;

            InteractableBinding.SetDebugInfo( this, nameof(InteractableBinding) );
            InteractableBinding.Init( GetSource(InteractableBinding) );
            InteractableBinding.SourceChanged += ProcessInteractable;

            ReadonlyBinding.SetDebugInfo( this, nameof(ReadonlyBinding) );
            ReadonlyBinding.Init( GetSource(ReadonlyBinding) );
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
            ValueBinding.Settings.Mode  = DataBinding.EMode.TwoWay;
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
