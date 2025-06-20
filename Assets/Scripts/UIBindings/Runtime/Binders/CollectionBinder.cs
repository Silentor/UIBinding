using System;
using System.Collections.Generic;
using UIBindings.Runtime.Utils;
using UnityEngine;

namespace UIBindings
{
    public class CollectionBinder : BinderBase
    {
        public CollectionBinding     Collection;
        public GameObject            ItemViewPrefab;
        public Transform             ItemViewsParent;
        public bool                  PoolItemViews = true;

        private readonly Stack<GameObject> _pooledViews = new ();
        private readonly List<GameObject> _visibleViews = new ();

        private void Awake( )
        {
            Collection.SetDebugInfo( this, nameof(Collection) );
            Collection.Init( GetParentSource() );
            Collection.CollectionChanged += OnCollectionModified;
            Collection.ItemAdded += OnItemAdded;
            Collection.ItemRemoved += OnItemRemoved;
            Collection.ItemMoved += OnItemMoved;
            Collection.ItemChanged += OnItemChanged;
        }

        private void OnEnable( )
        {
            Collection.Subscribe( GetUpdateOrder() );
        }

        private void OnDisable( )
        {
            Collection.Unsubscribe();
        }

        private void OnCollectionModified( CollectionBinding sender, IReadOnlyList<System.Object> collection )
        {
            while ( _visibleViews.Count > 0 )
            {
                var view = _visibleViews[ ^1 ];
                _visibleViews.RemoveAt( _visibleViews.Count - 1 );
                ReleaseViewItem( view );
            }

            for ( var i = 0; i < collection.Count; i++ )
            {
                var item     = collection[ i ];
                var viewItem = GetViewItem();
                viewItem.name = item.ToString();
                viewItem.transform.SetSiblingIndex( i );
                //viewItem.name = item.ToString();
                _visibleViews.Add( viewItem );
                if ( sender.BindViewItemMethod != null )
                    sender.BindViewItemMethod( item, viewItem );
            }
        }

        private void OnItemChanged(CollectionBinding sender, Int32 changedItemIndex, System.Object changedObject )
        {
            var viewItem = _visibleViews[changedItemIndex];
            if( sender.BindViewItemMethod != null )
                sender.BindViewItemMethod( changedObject, viewItem );
        }

        private void OnItemMoved(CollectionBinding sender, Int32 oldIndex, Int32 newIndex, System.Object movedItem )
        {
            var viewItem = _visibleViews[ oldIndex ];
            viewItem.transform.SetSiblingIndex( newIndex );
            _visibleViews.RemoveAt( oldIndex );
            _visibleViews.Insert( newIndex, viewItem );
            //if( sender.BindViewItemMethod != null )
              //  sender.BindViewItemMethod( movedItem, viewItem );
        }

        private void OnItemRemoved(CollectionBinding sender, Int32 removedItemIndex, System.Object removedItem )
        {
            var viewItem = _visibleViews[ removedItemIndex ];
            _visibleViews.RemoveAt( removedItemIndex );
            ReleaseViewItem( viewItem );
        }

        private void OnItemAdded(CollectionBinding sender, Int32 addedItemIndex, System.Object addedItem )
        {
            var viewItem       = GetViewItem();
            if( sender.BindViewItemMethod != null )
                sender.BindViewItemMethod( addedItem, viewItem );
            viewItem.transform.SetSiblingIndex( addedItemIndex );
            _visibleViews.Insert( addedItemIndex, viewItem );
        }

        private GameObject GetViewItem( )
        {
            if( _pooledViews.Count > 0 )
            {
                var view = _pooledViews.Pop();
                view.SetActive( true );
                return view;
            }

            var newViewItem = Instantiate( ItemViewPrefab, ItemViewsParent );
            return newViewItem;
        }

        private void ReleaseViewItem( GameObject itemView )
        {
            AssertWithContext.IsNotNull( itemView, context: this );
            if( PoolItemViews )
            {
                itemView.SetActive( false );
                _pooledViews.Push( itemView );
                AssertWithContext.IsTrue( _pooledViews.Count < 1000, $"Something wrong, too many pooled views", this );
            }
            else
            {
                Destroy( itemView );
            }
        }
        
    }
   
}