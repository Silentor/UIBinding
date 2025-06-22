using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Object = System.Object;

namespace UIBindings
{
    public class BehaviorBinder : BinderBase
    {
        public Behaviour Behavior;
        public ValueBinding<Boolean> EnabledBinding;

        protected void Awake( )
        {
            if( !Behavior )
                Behavior = GetComponent<Selectable>();
            Assert.IsTrue( Behavior );

            EnabledBinding.SetDebugInfo( this, nameof(EnabledBinding) );
            EnabledBinding.Init( GetParentSource() );
            EnabledBinding.SourceChanged += ProcessSourceToTarget;
        }

        private void OnEnable( )
        {
            EnabledBinding.Subscribe( GetUpdateOrder() );
        }

        private void OnDisable( )
        {
            EnabledBinding.Unsubscribe();
        }

        private void ProcessSourceToTarget(Object sender, Boolean value )
        {
            Behavior.enabled = value;
        }
    }
}