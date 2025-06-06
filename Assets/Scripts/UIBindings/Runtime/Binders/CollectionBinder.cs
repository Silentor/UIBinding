using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UIBindings
{
    public class CollectionBinder : MonoBehaviour
    {
        public CollectionBinding    Collection;
        public GameObject            ItemViewPrefab;
        public Transform             ItemViewsParent;

        private void Awake( )
        {
            Collection.SetDebugInfo( this, nameof(Collection) );
            Collection.Init();
            Collection.SourceChanged += OnCollectionModified;
        }

        private void OnEnable( )
        {
            Collection.Subscribe();
        }

        private void OnDisable( )
        {
            Collection.Unsubscribe();
        }


        private void OnCollectionModified( System.Object sender, IReadOnlyList<System.Object> collection, Action<System.Object, GameObject> initCollectionItemView )
        {
            while ( ItemViewsParent.childCount > 0 )
            {
                var child = ItemViewsParent.GetChild( 0 );
                child.SetParent( null, false );
                Destroy( child.gameObject );
            }

            foreach ( var item in Collection.ViewList )
            {
                var instance = Instantiate( ItemViewPrefab, ItemViewsParent );
                if( initCollectionItemView != null )
                    initCollectionItemView( item, instance );
            }
        }

        
    }

   
}