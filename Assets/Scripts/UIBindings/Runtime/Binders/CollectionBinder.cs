using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace UIBindings
{
    public class CollectionBinder : MonoBehaviour
    {
        public CollectionBinding     Collection;
        public GameObject            ItemViewPrefab;
        public Transform             ItemViewsParent;
        public bool                  PoolItemViews = true;

        private readonly Stack<GameObject> _pooledViews = new Stack<GameObject>();

        private void Awake( )
        {
            Collection.SetDebugInfo( this, nameof(Collection) );
            Collection.Init();
            Collection.CollectionChanged += OnCollectionModified;
            Collection.ItemAdded += OnItemAdded;
            Collection.ItemRemoved += OnItemRemoved;
            Collection.ItemMoved += OnItemMoved;
            Collection.ItemChanged += OnItemChanged;
        }

        

        private void OnEnable( )
        {
            Collection.Subscribe();
        }

        private void OnDisable( )
        {
            Collection.Unsubscribe();
        }


        private void OnCollectionModified( CollectionBinding sender, IReadOnlyList<System.Object> collection )
        {
            while ( ItemViewsParent.childCount > 0 )
            {
                var child = ItemViewsParent.GetChild( ItemViewsParent.childCount - 1 );
                ReleaseViewItem( child.gameObject );
            }

            foreach ( var item in collection )
            {
                var viewItem           = GetViewItem();
                if( sender.BindViewItemMethod != null )
                    sender.BindViewItemMethod( item, viewItem );
            }
        }

        private void OnItemChanged(CollectionBinding sender, Int32 changedItemIndex, System.Object changedObject )
        {
            var viewItem = ItemViewsParent.GetChild( changedItemIndex ).gameObject;
            if( sender.BindViewItemMethod != null )
                sender.BindViewItemMethod( changedObject, viewItem );
        }

        private void OnItemMoved(CollectionBinding sender, Int32 oldIndex, Int32 newIndex, System.Object movedItem )
        {
            var viewItem = ItemViewsParent.GetChild( oldIndex ).gameObject;
            viewItem.transform.SetSiblingIndex( newIndex );
            if( sender.BindViewItemMethod != null )
                sender.BindViewItemMethod( movedItem, viewItem );
        }

        private void OnItemRemoved(CollectionBinding sender, Int32 removedItemIndex, System.Object removedItem )
        {
            var viewItem = ItemViewsParent.GetChild( removedItemIndex ).gameObject;
            ReleaseViewItem( viewItem );
        }

        private void OnItemAdded(CollectionBinding sender, Int32 addedItemIndex, System.Object addedItem )
        {
            var viewItem       = GetViewItem();
            if( sender.BindViewItemMethod != null )
                sender.BindViewItemMethod( addedItem, viewItem );
            viewItem.transform.SetSiblingIndex( addedItemIndex );
        }

        private GameObject GetViewItem( )
        {
            if( _pooledViews.Count > 0 )
            {
                var view = _pooledViews.Pop();
                view.SetActive( true );
                return view;
            }

            return Instantiate( ItemViewPrefab, ItemViewsParent );
        }

        private void ReleaseViewItem( GameObject itemView )
        {
            if( PoolItemViews )
            {
                itemView.SetActive( false );
                _pooledViews.Push( itemView );
            }
            else
            {
                Destroy( itemView );
            }
        }
        
    }
   
}