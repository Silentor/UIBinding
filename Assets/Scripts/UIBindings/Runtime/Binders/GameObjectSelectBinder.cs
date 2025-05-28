using System;
using UIBindings.Runtime;
using UnityEngine;
using Object = System.Object;
using Unity.Collections.LowLevel.Unsafe;

namespace UIBindings
{
    public class GameObjectSelectBinder : MonoBehaviour
    {
        public KeyGameObject[] GameObjects;
        public Binding<StructEnum> SelectorBinding;

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

        private void OnSelectorValueChanged(Object sender, StructEnum value )
        {
            var intValue = (int)value;
            // for ( int i = 0; i < GameObjects.Length; i++ )
            // {
            //     GameObjects[i].SetActive( i == intValue );
            // }
        }

        public struct KeyGameObject
        {
            public int Key;
            public GameObject GameObject;
        }
    }
}