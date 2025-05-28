using System;
using UIBindings.Runtime;
using UnityEngine;
using Object = System.Object;
using Unity.Collections.LowLevel.Unsafe;

namespace UIBindings
{
    public class GameObjectsEnumBinder : MonoBehaviour
    {
        public GameObject[] GameObjects;
        public Binding<IntedEnum> SelectorBinding;

        private void Awake( )
        {
            SelectorBinding.Awake( this );
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

        private void OnSelectorValueChanged(Object sender, IntedEnum value )
        {
            var intValue = (int)value;
            // for ( int i = 0; i < GameObjects.Length; i++ )
            // {
            //     GameObjects[i].SetActive( i == intValue );
            // }
        }
    }
}