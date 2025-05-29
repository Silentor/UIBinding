using UnityEngine;
using UnityEngine.Assertions;
using Object = System.Object;

namespace UIBindings
{
    public class TransformBinder : MonoBehaviour
    {
        public Transform                Transform;
        public Binding<Vector3>         LocalPositionBinding;
        public Binding<Vector3>         ScaleBinding         = new (){Enabled = false};
        public Binding<Vector3>         LocalRotationBinding = new (){Enabled = false};

        private void Awake( )
        {
            if ( !Transform )
                Transform = GetComponent<Transform>();
            Assert.IsTrue( Transform );

            LocalPositionBinding.SetDebugInfo( this, nameof(LocalPositionBinding) );
            LocalPositionBinding.Awake(  );
            LocalPositionBinding.SourceChanged += ProcessLocalPosition;

            ScaleBinding.SetDebugInfo( this, nameof(ScaleBinding) );
            ScaleBinding.Awake(  );
            ScaleBinding.SourceChanged += ProcessScale;

            LocalRotationBinding.SetDebugInfo( this, nameof(LocalRotationBinding) );
            LocalRotationBinding.Awake(  );
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
            LocalPositionBinding.Subscribe();
            ScaleBinding.Subscribe();
            LocalRotationBinding.Subscribe();
        }

        private void OnDisable( )
        {
            LocalPositionBinding.Unsubscribe();
            ScaleBinding.Unsubscribe();
            LocalRotationBinding.Unsubscribe();
        }

        private void LateUpdate( )
        {
            LocalPositionBinding.CheckChanges();
            ScaleBinding.CheckChanges();
            LocalRotationBinding.CheckChanges();
        }
    }
}