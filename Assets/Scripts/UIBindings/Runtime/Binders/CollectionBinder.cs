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
        public CollectionBinding     Collection;
        public GameObject            ItemViewPrefab;
        public Transform             ItemViewsParent;
        public bool                  PoolItemViews = true;

        private readonly List<GameObject> _pooledViews = new List<GameObject>();

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


        private void OnCollectionModified( System.Object sender, IReadOnlyList<System.Object> collection, Action<System.Object, GameObject> bindItem )
        {
            if( PoolItemViews )
            {
                for( var i = 0; i < ItemViewsParent.childCount; i++) 
                {
                    var child = ItemViewsParent.GetChild( i ).gameObject;
                    child.SetActive( false );
                    _pooledViews.Add( child );
                }
            }
            else
            {
                while ( ItemViewsParent.childCount > 0 )
                {
                    var child = ItemViewsParent.GetChild( 0 );
                    child.SetParent( null, false );
                    Destroy( child.gameObject );
                }
            }

            if( PoolItemViews )
            {
                foreach ( var item in Collection.ViewList )
                {
                    TryGetPooledView( out var instance );
                    if( bindItem != null )
                        bindItem( item, instance );
                }
            }
            else
            {
                foreach ( var item in Collection.ViewList )
                {
                    var instance = Instantiate( ItemViewPrefab, ItemViewsParent );
                    if( bindItem != null )
                        bindItem( item, instance );
                }
            }
            
        }

        private bool TryGetPooledView( out GameObject view )
        {
            if( _pooledViews.Count > 0 )
            {
                view = _pooledViews[0];
                _pooledViews.RemoveAt( 0 );
                view.SetActive( true );
                return true;
            }

            view = Instantiate( ItemViewPrefab, ItemViewsParent );
            return false;
        }

        
    }

   
}