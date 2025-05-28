using System;
using UnityEngine;
using Object = System.Object;

namespace UIBindings
{
    public class GameObjectSelectBinder : MonoBehaviour
    {
        public KeyGameObject[] GameObjects;
        public Binding<int> SelectorBinding;

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
            var intValue = (int)value;
            for ( int i = 0; i < GameObjects.Length; i++ )
            {
                GameObjects[i].GameObject.SetActive( GameObjects[i].Key == intValue );
            }
        }

        [Serializable]
        public struct KeyGameObject
        {
            public int Key;
            public GameObject GameObject;
        }
    }
}