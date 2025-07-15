using System;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using Object = System.Object;

namespace UIBindings
{
    public class LabelBinder : BinderBase
    {
        public TMP_Text                      Label;
        public ValueBinding<String>          TextBinding;
        public ValueBinding<Color>           ColorBinding;          //Optional

        private void Awake( )
        {
            if ( !Label )
                Label = GetComponent<TextMeshProUGUI>();

            if ( !Label )
            {
                Debug.LogError( "LabelBinder: Label component not found. Please add a TMP_Text component to the same GO or assign TMP_Text from another GO." );
                return;
            }

            TextBinding.SetDebugInfo( this, nameof(TextBinding) );
            TextBinding.Init( GetSource(TextBinding) );
            TextBinding.SourceChanged += ProcessText;

            ColorBinding.SetDebugInfo( this, nameof(ColorBinding) );
            ColorBinding.Init( GetSource(ColorBinding) );
            ColorBinding.SourceChanged += ProcessColor;
        }

        private void ProcessText(Object sender, String value )
        {
            Label.text = value;
        }

        private void ProcessColor(Object sender, Color value )
        {
            Label.color = value;
        }

        private void OnEnable( )
        {
            TextBinding.Subscribe( GetUpdateOrder() );
            ColorBinding.Subscribe( GetUpdateOrder() );
        }

        private void OnDisable( )
        {
            TextBinding.Unsubscribe();
            ColorBinding.Unsubscribe();
        }

#if UNITY_EDITOR
        private void Reset( )
        {
            ColorBinding.Enabled = false;
        }
#endif
    }
}