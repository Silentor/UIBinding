using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Object = System.Object;

namespace UIBindings
{
    public class InteractableBinder : MonoBehaviour
    {
        public Selectable InteractionControl;
        public Binding<Boolean> InteractableBinding;

        protected void Awake( )
        {
            if( !InteractionControl )
                InteractionControl = GetComponent<Selectable>();
            Assert.IsTrue( InteractionControl );

            InteractableBinding.SetDebugInfo( this, nameof(InteractableBinding) );
            InteractableBinding.Awake( );
            InteractableBinding.SourceChanged += ProcessSourceToTarget;
        }

        private void OnEnable( )
        {
            InteractableBinding.Subscribe();
        }

        private void OnDisable( )
        {
            InteractableBinding.Unsubscribe();
        }

        private void LateUpdate( )
        {
            InteractableBinding.CheckChanges();
        }

        private void ProcessSourceToTarget(Object sender, Boolean value )
        {
            InteractionControl.interactable = value;
        }
    }
}