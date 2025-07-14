using System;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Object = System.Object;

namespace UIBindings
{
    public class InteractableBinder : BinderBase
    {
        public Selectable InteractionControl;
        public ValueBinding<Boolean> InteractableBinding;

        protected void Awake( )
        {
            if( !InteractionControl )
                InteractionControl = GetComponent<Selectable>();
            Assert.IsTrue( InteractionControl );

            InteractableBinding.SetDebugInfo( this, nameof(InteractableBinding) );
            InteractableBinding.Init( GetSource(InteractableBinding) );
            InteractableBinding.SourceChanged += ProcessSourceToTarget;
        }

        private void OnEnable( )
        {
            InteractableBinding.Subscribe( GetUpdateOrder() );
        }

        private void OnDisable( )
        {
            InteractableBinding.Unsubscribe();
        }

        private void ProcessSourceToTarget(Object sender, Boolean value )
        {
            InteractionControl.interactable = value;
        }
    }
}