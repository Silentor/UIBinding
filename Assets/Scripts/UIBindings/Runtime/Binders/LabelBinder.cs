using System;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using Object = System.Object;

namespace UIBindings
{
    public class LabelBinder : MonoBehaviour
    {
        public TextMeshProUGUI          Label;
        public ValueBinding<String>          TextBinding;
        public ValueBinding<Color>           ColorBinding            = new (){Enabled = false};

        private void Awake( )
        {
            if ( !Label )
                Label = GetComponent<TextMeshProUGUI>();
            Assert.IsTrue( Label );

            TextBinding.SetDebugInfo( this, nameof(TextBinding) );
            TextBinding.Init(  );
            TextBinding.SourceChanged += ProcessText;

            ColorBinding.SetDebugInfo( this, nameof(ColorBinding) );
            ColorBinding.Init(  );
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
            TextBinding.Subscribe();
            ColorBinding.Subscribe();
        }

        private void OnDisable( )
        {
            TextBinding.Unsubscribe();
            ColorBinding.Unsubscribe();
        }
    }
}