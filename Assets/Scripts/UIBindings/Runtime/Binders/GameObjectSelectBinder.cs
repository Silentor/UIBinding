using UIBindings.Runtime.Types;
using UnityEngine;
using Object = System.Object;

namespace UIBindings
{
    public class GameObjectSelectBinder : MonoBehaviour
    {
        public KeyValue<GameObject>[]   GameObjects;
        public Binding<int>             SelectorBinding;

        private void Awake( )
        {
            SelectorBinding.SetDebugInfo( this, nameof(SelectorBinding) );
            SelectorBinding.Awake(  );
            SelectorBinding.SourceChanged += OnSelectorValueChanged;
        }

        private void OnEnable( )
        {
            SelectorBinding.Subscribe();
        }

        private void OnDisable( )
        {
            SelectorBinding.Unsubscribe();
        }

        private void LateUpdate( )
        {
            SelectorBinding.CheckChanges();
        }

        private void OnSelectorValueChanged(Object sender, int value )
        {
            for ( int i = 0; i < GameObjects.Length; i++ )
            {
                var kv = GameObjects[i];
                if( kv.Value )
                    kv.Value.SetActive( kv.Key == value );
            }
        }
    }
}