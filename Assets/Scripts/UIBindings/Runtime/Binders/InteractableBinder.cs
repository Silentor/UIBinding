using System;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UIBindings
{
    public class InteractableBinder : BinderBase<bool>
    {
        public Selectable InteractionControl;

        protected override void Awake( )
        {
            base.Awake();

            if( !InteractionControl )
                InteractionControl = GetComponent<Selectable>();
            Assert.IsTrue( InteractionControl );
        }


        public override void ProcessSourceToTarget(Boolean value )
        {
            InteractionControl.interactable = value;
        }
    }
}