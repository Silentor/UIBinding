using System;
using Unity.Properties;

namespace UIBindings.Runtime
{
    /// <summary>
    /// Collection item view with custom data source
    /// </summary>
    public class CollectionItemViewModel : ViewModel
    {
        /// <summary>
        /// Initializes the ViewModel with the given item.
        /// </summary>
        /// <param name="collectionItem">The item to use.</param>
        public virtual void Initialize( Object collectionItem )
        {
        }
    }

    /// <summary>
    /// Collection item view model with an autobinding to a specific type of collection item.
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    public class CollectionItemViewModel<TItem> : CollectionItemViewModel
    {
        public TItem SourceItem { get; private set; }

        /// <summary>
        /// Initializes the ViewModel with the given item.
        /// </summary>
        /// <param name="collectionItem">The item to use.</param>
        public override void Initialize( Object collectionItem)
        {
            SourceItem = (TItem)Convert.ChangeType( collectionItem, typeof(TItem) );
            OnPropertyChangedAll();
        }

        /// <summary>
        /// Clears the current item.
        /// </summary>
        public void Clear()
        {
            SourceItem = default;
        }
    }
}