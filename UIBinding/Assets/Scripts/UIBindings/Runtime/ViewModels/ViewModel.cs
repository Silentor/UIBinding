using System;
using UIBindings.SourceGen;
using UnityEngine;
using Object = System.Object;

namespace UIBindings.Runtime
{
    /// <summary>
    /// Source of data for the UI Bindings system. Child bindings use VM as default data source.
    /// </summary>
    [INotifyPropertyChanged]
    public abstract partial class ViewModel : BinderBase
    {
        
    }

    /// <summary>
    /// ViewModel with a specific data source type. Data source can be binded to some other source property
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ViewModel<T> : ViewModel
    {
        public ValueBinding<T> DataSourceBinding; 

        /// <summary>
        /// The data source for this ViewModel.
        /// </summary>
        public T Source { get; private set; }

        private void Awake( )
        {
            DataSourceBinding.SetDebugInfo( this, nameof(DataSourceBinding) );
            DataSourceBinding.Init( GetSource( DataSourceBinding ) );
            DataSourceBinding.SourceChanged += OnDataSourceChanged;
        }

        private void OnEnable( )
        {
            DataSourceBinding.Subscribe( GetUpdateOrder() );
        }

        private void OnDisable( )
        {
            DataSourceBinding.Unsubscribe();
        }

        private void OnDataSourceChanged(Object sender, T value )
        {
            Source = value;
        }

        /// <summary>
        /// Initializes the ViewModel with the given data source.
        /// </summary>
        /// <param name="dataSource">The data source to use.</param>
        public void Initialize(T dataSource)
        {
            Source = dataSource;
        }
    }
}