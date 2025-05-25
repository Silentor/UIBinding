using System;
using TMPro;
using UIBindings.Runtime;
using UnityEngine;
using UnityEngine.Assertions;
using Object = System.Object;

namespace UIBindings
{
    public class LabelBinder : MonoBehaviour
    {
        public TextMeshProUGUI          Label;
        public Binding<String>          TextBinding;

        private void Awake( )
        {
            if ( !Label )
                Label = GetComponent<TextMeshProUGUI>();
            Assert.IsTrue( Label );

            TextBinding.Awake( this );
            TextBinding.SourceChanged += ProcessSourceToTarget; 
        }

        private void ProcessSourceToTarget(Object sender, String value )
        {
            Label.text = value;
        }

        private void OnEnable( )
        {
            TextBinding.Subscribe();
        }

        private void OnDisable( )
        {
            TextBinding.Unsubscribe();
        }

        private void LateUpdate( )
        {
            TextBinding.CheckChanges();
        }
    }

}