using System;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using Object = System.Object;

namespace UIBindings
{
    public class LabelBinder : BinderBase<String>
    {
        public TextMeshProUGUI Label;

        protected override void Awake( )
        {
            base.Awake();

            if ( !Label )
                Label = GetComponent<TextMeshProUGUI>();
            Assert.IsTrue( Label );
        }
        

        public override void ProcessSourceToTarget(String value )
        {
            Label.text = value;
        }
    }
}
