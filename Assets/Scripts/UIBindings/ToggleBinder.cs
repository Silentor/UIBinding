using System;
using UnityEngine.UI;

namespace UIBindings
{
    public class ToggleBinder : BinderTwoWayBase<bool>
    {
        public Toggle Toggle;

        protected override void Awake( )
        {
            base.Awake();

            if ( !Toggle )
                Toggle = GetComponent<Toggle>();

            Toggle.onValueChanged.AddListener( OnToggleValueChanged );
            InitSetter();
        }

        private void OnToggleValueChanged(Boolean newValue )
        {
            ProcessTargetToSource( newValue );
        }

        public override void ProcessSourceToTarget(bool value )
        {
            Toggle.SetIsOnWithoutNotify( value );
        }
    }
}
