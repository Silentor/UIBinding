using UnityEngine;
using UnityEngine.Assertions;
using Object = System.Object;

namespace UIBindings
{
    public class TransformBinder : BinderBase
    {
        public Transform                     Transform;
        public ValueBinding<Vector3>         LocalPositionBinding;
        public ValueBinding<Vector3>         LocalRotationBinding;                  //Optional
        public ValueBinding<Vector3>         LocalScaleBinding   ;                  //Optional

        private void Awake( )
        {
            if ( !Transform )
                Transform = GetComponent<Transform>();
            Assert.IsTrue( Transform );

            LocalPositionBinding.SetDebugInfo( this, nameof(LocalPositionBinding) );
            LocalPositionBinding.Init( GetParentSource() );
            LocalPositionBinding.SourceChanged += ProcessLocalPosition;

            LocalScaleBinding.SetDebugInfo( this, nameof(LocalScaleBinding) );
            LocalScaleBinding.Init( GetParentSource() );
            LocalScaleBinding.SourceChanged += ProcessScale;

            LocalRotationBinding.SetDebugInfo( this, nameof(LocalRotationBinding) );
            LocalRotationBinding.Init( GetParentSource() );
            LocalRotationBinding.SourceChanged += ProcessLocalRotation;
        }

        private void ProcessLocalRotation(Object sender, Vector3 value )
        {
            Transform.localEulerAngles = value;
        }

        private void ProcessLocalPosition( Object sender, Vector3 value)
        {
            Transform.localPosition = value;
        }

        private void ProcessScale(Object sender, Vector3 value )
        {
            Transform.localScale = value;
        }

        private void OnEnable( )
        {
            LocalPositionBinding.Subscribe( GetUpdateOrder() );
            LocalScaleBinding.Subscribe( GetUpdateOrder() );
            LocalRotationBinding.Subscribe( GetUpdateOrder() );
        }

        private void OnDisable( )
        {
            LocalPositionBinding.Unsubscribe();
            LocalScaleBinding.Unsubscribe();
            LocalRotationBinding.Unsubscribe();
        }

#if UNITY_EDITOR
        private void Reset( )
        {
            LocalRotationBinding.Enabled = false;
            LocalScaleBinding.Enabled = false;
        }
#endif
    }
}