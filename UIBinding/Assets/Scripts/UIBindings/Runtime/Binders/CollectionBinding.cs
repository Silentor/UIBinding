using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using UIBindings.Adapters;
using UIBindings.Runtime.Utils;
using UnityEngine;
using Object = System.Object;
using Unity.Profiling;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using UnityEngine.Scripting;

namespace UIBindings
{
    [Serializable]
    public class CollectionBinding : DataBinding
    {
        public IReadOnlyList<object> ViewList   => _processedList;

        public Action<object, GameObject> BindViewItemMethod { get; private set; }

        public override Boolean IsCompatibleWith(Type type ) => typeof(IEnumerable).IsAssignableFrom( type ); 
            

        public override Boolean IsTwoWay => false;

        public override    Boolean IsRuntimeValid => _isValid;

        public void Init( object sourceObject = null )
        {
            if ( !Enabled )   
                return;

            if ( BindToType )
            {
                AssertWithContext.IsNotNull( sourceObject, $"[{nameof(CollectionBinding)}] Source object must be assigned for binding {_debugTargetBindingInfo} from the code. Assigned object must be {SourceType} type", _debugHost );
                SourceObject = sourceObject;
            }
            else
            {
                AssertWithContext.IsNotNull( Source, $"[{nameof(CollectionBinding)}] Source object must be assigned for binding {_debugTargetBindingInfo} from the inspector", _debugHost );
                SourceObject = Source;
            }

            InitInfrastructure( );
        }

        private void InitInfrastructure( )
        {
            if ( !Enabled )   
                return;

            AssertWithContext.IsNotNull( SourceObject, $"[{nameof(CollectionBinding)}] Source is not assigned at {_debugTargetBindingInfo}", _debugHost );
            AssertWithContext.IsNotEmpty( Path, $"[{nameof(CollectionBinding)}] Path is not assigned at {_debugTargetBindingInfo}", _debugHost );

            var timer = ProfileUtils.GetTraceTimer(); 

            var sourceType = SourceObject.GetType();


            //Here we process deep binding
            var pathProcessor = new PathProcessor( SourceObject, Path, IsTwoWay, OnSourcePropertyChangedFromPropertyAdapter );
            //pathProcessor.MoveNextProperty(out var firstProperty);   //Get first property info

            timer.AddMarker( "GetProperty" );

            DataProvider lastDataSource = null;

            //Do not support converters for now
            if ( Converters.Count == 0 )
            {
                //Check fast pass - direct getter
                // if ( !pathProcessor.IsComplexPath )
                // {
                //     if ( firstProperty.PropertyType == typeof(ViewCollection) )
                //     {
                //         _viewCollectionDirectGetter = (Func<ViewCollection>)Delegate.CreateDelegate( typeof(Func<ViewCollection>), SourceObject, firstProperty.GetGetMethod( true ) );
                //         _isNeedPolling = SourceObject is INotifyPropertyChanged;
                //         timer.AddMarker( "ViewCollectionDirectGetter" );
                //     }
                //     else if( typeof(IEnumerable).IsAssignableFrom( firstProperty.PropertyType ) )
                //     {
                //         _enumerableDirectGetter = (Func<IEnumerable>)Delegate.CreateDelegate( typeof(Func<IEnumerable>), SourceObject, firstProperty.GetGetMethod( true ) );
                //         _isNeedPolling = SourceObject is INotifyPropertyChanged;
                //         var bindingAttribute = firstProperty.GetCustomAttribute<CollectionBindingAttribute>();
                //         if( bindingAttribute != null )
                //         {
                //             if( bindingAttribute.BindMethodName != null )
                //             {
                //                 var bindMethodInfo = sourceType.GetMethod( bindingAttribute.BindMethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
                //                 if( bindMethodInfo != null )
                //                     _bindMethod = (Action<object, GameObject>)Delegate.CreateDelegate( typeof(Action<object, GameObject>), SourceObject, bindMethodInfo );
                //                 var processMethodInfo = sourceType.GetMethod( bindingAttribute.ProcessMethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
                //                 if( processMethodInfo != null )
                //                     _processMethod = (Action<List<Object>>)Delegate.CreateDelegate( typeof(Action<List<Object>>), SourceObject, processMethodInfo );
                //             }
                //         }
                //         timer.AddMarker( "IEnumerableDirectGetter" );
                //     }
                //     else
                //     {
                //         throw new NotSupportedException($"CollectionBinding does not support collection type {firstProperty.PropertyType } yet.");
                //     }
                // }
                // else
                {
                    Action<object, string> notifyDelegate = OnSourcePropertyChangedFromPropertyAdapter;
                    while( pathProcessor.MoveNext( out var pathAdapter ) )
                    {
                        _propReader    = pathAdapter;
                        lastDataSource = _propReader;
                    }

                    //Try to get ViewCollection or IEnumerable from last property adapter
                    if ( lastDataSource is IDataReader<ViewCollection> viewCollectionReader )
                        _viewCollectionReader = viewCollectionReader;
                    else if ( lastDataSource is IDataReader<IEnumerable> enumerableReader )
                        _enumerableReader = enumerableReader;
                    else
                        throw new NotSupportedException( $"CollectionBinding does not support collection type {lastDataSource.GetType()} yet." );

                    timer.AddMarker( "Connect" );       //Data pipeline is completed
                }
            }
            else        //Need adapter/converters/etc
            {
                throw new NotSupportedException("CollectionBinding does not support converters yet.");
            }

            var report = timer.StopAndGetReport();
             
            _isValid = true;
            
            Debug.Log( $"[{nameof(CollectionBinding)}] Awaked {timer.Elapsed.TotalMicroseconds()} mks, {GetBindingTargetInfo()}: {report}" );
        }

        public event Action<CollectionBinding, int, object>                            ItemAdded;
        public event Action<CollectionBinding, int, object>                            ItemRemoved;
        public event Action<CollectionBinding, int, object>                            ItemChanged;
        /// <summary>
        /// Item moved from oldIndex to newIndex, object is the item itself
        /// </summary>
        public event Action<CollectionBinding, int, int, object>                       ItemMoved;
        /// <summary>
        /// Dramatic changes, its better just rebuild entire collection
        /// </summary>
        public event Action<CollectionBinding, IReadOnlyList<object>>                  CollectionChanged;   
        


        private readonly List<object>               _sourceCopy = new List<object>();
        private          ViewCollection             _sourceCollection;
        private readonly         List<Object>               _processedList = new List<System.Object>();
        private readonly         List<Object>               _processedCopy = new List<System.Object>();
        private             PathAdapter _propReader;
        private             Boolean _isNeedPolling;

        //Source property direct getters
        private          Func<ViewCollection>       _viewCollectionDirectGetter;
        private          Func<IEnumerable>          _enumerableDirectGetter;

        //Source property readers
        private         IDataReader<ViewCollection> _viewCollectionReader;
        private         IDataReader<IEnumerable>    _enumerableReader;

        //Collection process methods
        private          Action<object, GameObject> _bindMethod;
        private          Action<List<Object>>       _processMethod;
        

        protected static readonly ProfilerMarker ReadViewCollectionDirectMarker  = new ( ProfilerCategory.Scripts,  $"{nameof(CollectionBinding)}.ReadViewCollectionDirect" );
        protected static readonly ProfilerMarker ReadIEnumerableDirectMarker     = new ( ProfilerCategory.Scripts,  $"{nameof(CollectionBinding)}.ReadIEnumerableDirect" );
        protected static readonly ProfilerMarker ReadViewCollectionMarker  = new ( ProfilerCategory.Scripts,  $"{nameof(CollectionBinding)}.ReadViewCollection" );
        protected static readonly ProfilerMarker ReadIEnumerableMarker     = new ( ProfilerCategory.Scripts,  $"{nameof(CollectionBinding)}.ReadIEnumerable" );

        private void OnSourcePropertyChanged(Object sender, String propertyName )
        {
            if ( String.IsNullOrEmpty( propertyName ) || String.Equals( propertyName, Path, StringComparison.Ordinal ) )
            {
                _sourceValueChanged = true;
            }
        }

        private void OnSourcePropertyChangedFromPropertyAdapter(Object sender, String propertyName )
        {
            //No property name checking, property adapter will check it
            _sourceValueChanged = true;
        }

        protected override void OnSubscribe( )
        {
            base.OnSubscribe();

            if ( _viewCollectionDirectGetter != null || _enumerableDirectGetter != null )
            {
                if( !_isNeedPolling )
                    ((INotifyPropertyChanged)SourceObject).PropertyChanged += OnSourcePropertyChanged;
            }
            else
                _propReader.Subscribe();
        }

        protected override void OnUnsubscribe( )
        {
            base.OnUnsubscribe();

            if ( _viewCollectionDirectGetter != null || _enumerableDirectGetter != null )
            {
                if( !_isNeedPolling )
                    ((INotifyPropertyChanged)SourceObject).PropertyChanged -= OnSourcePropertyChanged;
            }
            else
                _propReader.Unsubscribe();
        }

        private Boolean IsEqual( IReadOnlyList<object> listA, IReadOnlyList<object> listB )
        {
            if( listA.Count != listB.Count )
                return false;

            for ( int i = 0; i < listB.Count; i++ )
            {
                if( listB[i] != listA[i] )
                    return false;
            }

            return true;
        }

        protected override void    CheckChangesInternal( )
        {
            if( !_isValueInitialized || _isNeedPolling || _sourceValueChanged  )
            {
                Action<List<object> > processListAction = null;
                Action<Object, GameObject> bindViewItemAction = null;
                var isChangedOnSource = true;
                _processedList.Clear();

                if ( _propReader != null )                                           //Read source collection via data pipeline
                {
                    if ( _viewCollectionReader != null )
                    {
                        ReadViewCollectionMarker.Begin( ProfilerMarkerName );
                        var changeStatus = _viewCollectionReader.TryGetValue( out var viewCollection );
                        isChangedOnSource = changeStatus != EResult.NotChanged;
                        _processedList.AddRange( viewCollection );
                        processListAction  = viewCollection.ProcessList;
                        bindViewItemAction = viewCollection.BindViewItem;
                        ReadViewCollectionMarker.End();
                    }
                    else if ( _enumerableReader != null )
                    {
                        ReadIEnumerableMarker.Begin( ProfilerMarkerName );
                        var changeStatus = _enumerableReader.TryGetValue( out var enumerable );
                        isChangedOnSource = changeStatus != EResult.NotChanged;
                        if ( enumerable != null )                        
                            _processedList.AddRange( enumerable.Cast<Object>() );
                        processListAction  = _processMethod;
                        bindViewItemAction = _bindMethod;
                        ReadIEnumerableMarker.End();
                    }
                    _isNeedPolling    = _propReader.IsNeedPolling;
                }
                else
                {
                    //Read source collection directly
                    if ( _viewCollectionDirectGetter != null )
                    {
                        ReadViewCollectionDirectMarker.Begin( ProfilerMarkerName );
                        var viewCollection = _viewCollectionDirectGetter();
                        _processedList.AddRange( viewCollection );
                        processListAction  = viewCollection.ProcessList;
                        bindViewItemAction = viewCollection.BindViewItem;
                        ReadViewCollectionDirectMarker.End();
                    }
                    else if( _enumerableDirectGetter != null )
                    {
                        ReadIEnumerableDirectMarker.Begin( ProfilerMarkerName );
                        var enumerable = _enumerableDirectGetter();
                        if ( enumerable != null )                        
                            _processedList.AddRange( enumerable.Cast<Object>() );
                        processListAction  = _processMethod;
                        bindViewItemAction = _bindMethod;
                        ReadIEnumerableDirectMarker.End();
                    }        
                }

                //Check for source collection modifications
                if( !_isValueInitialized || isChangedOnSource || !IsEqual( _sourceCopy, _processedList ))
                {
                    _isValueInitialized = true;
                    _sourceCopy.Clear();              
                    _sourceCopy.AddRange( _processedList );

                    processListAction?.Invoke( _processedList );
                    BindViewItemMethod = bindViewItemAction;

                    UpdateTargetMarker.Begin( ProfilerMarkerName );
                    CompareAndFireEvents( _processedCopy, _processedList );
                    _processedCopy.Clear();
                    _processedCopy.AddRange( _processedList );
                    UpdateTargetMarker.End( );
                }

                _sourceValueChanged = false;
            }
        }

        private void CompareAndFireEvents( List<object> oldList, List<object> newList ) 
        {
            //Fast passes
            if( oldList.Count == 0 && newList.Count == 0 )
                return; //Nothing to compare
            if( (oldList.Count == 0 && newList.Count > 0) || (newList.Count == 0 && oldList.Count > 0) )
            {
                CollectionChanged?.Invoke( this, newList );//Dramatic changes
                return;
            }
            if( oldList.Count == newList.Count && oldList.SequenceEqual( newList ) )
                return; //Nothing changed

            //Check simple modifications
            //Find added items
            var added   = new List<(object, int)>();
            for ( int i = 0; i < newList.Count; i++ )
            {
                if( !oldList.Contains( newList[i] ) )                    
                    added.Add( (newList[i], i) );
            }

            if ( added.Count == 0 && newList.Count > oldList.Count ) //Probably was added some null items, cant find granular diff
            {
                CollectionChanged?.Invoke( this, newList ); //Dramatic changes
                return;
            }

            //Simple modification - added some items without other changes
            if( added.Count > 0 && (newList.Count - oldList.Count) == added.Count )
            {
                foreach ( var addedItem in added )                    
                    ItemAdded?.Invoke( this, addedItem.Item2, addedItem.Item1 );
                return;
            }

            //Find removed items
            var removed = new List<(object, int)>();
            for ( int i = 0; i < oldList.Count; i++ )
            {
                if( !newList.Contains( oldList[i] ) )                    
                    removed.Add( (oldList[i], i) );
            }

            if ( removed.Count == 0 && oldList.Count > newList.Count ) //Probably was removed some null items, cant find granular diff
            {
                CollectionChanged?.Invoke( this, newList ); //Dramatic changes
                return;
            }

            //Simple modification - removed some items without other changes
            if( removed.Count > 0 && (oldList.Count - newList.Count) == removed.Count )
            {
                for ( var i = removed.Count - 1; i >= 0; i-- )
                {
                    var removedItem = removed[ i ];
                    ItemRemoved?.Invoke( this, removedItem.Item2, removedItem.Item1 );
                }

                return;
            }

            //Check for moved items and changed items
            if( added.Count == removed.Count )
            {
                if ( added.Count == 0 )         //Check for permutations without any additions or removals
                {
                    //TODO its not trivial, just skip check and fire CollectionChanged for now
                    CollectionChanged?.Invoke( this, newList );
                    return;
                    // for ( int i = 0; i < newList.Count; i++ )
                    // {
                    //     if( !Equals(newList[i], oldList[i]) )
                    //     {
                    //         var indexInOldList = oldList.IndexOf( newList[i] );
                    //         if( i < indexInOldList )                //Do not fire 2 events for same item
                    //             ItemMoved?.Invoke( this, indexInOldList, i, newList[i] );
                    //     }
                    // }
                }
                else
                {
                    for ( int i = 0; i < added.Count; i++ )  //Detect changed items without changing their order
                    {
                        var addedItem = added[i];
                        if( removed.TryFirst( r => addedItem.Item2 == r.Item2, out var sameIndexDifferentObject ) )
                        {
                            ItemChanged?.Invoke( this, addedItem.Item2, addedItem.Item1 );
                        }
                    }
                    return;
                }
            }

            //All other modifications consider as dramatic changes, no granular events
            CollectionChanged?.Invoke( this, newList );
        }

        public override void SetDebugInfo( MonoBehaviour host, String bindingName )
        {
            base.SetDebugInfo( host, bindingName );

            _debugTargetBindingInfo = $"'{host.name}'({host.GetType().Name}).{bindingName}";
        }

        public override String GetBindingState( )
        {
            if( !Enabled )
                return "Disabled";
            if( !_isValid )
                return "Invalid";
            if( !_isValueInitialized )
                return "Not initialized";
            return $"{_processedCopy.Count} view items";
        }

        public override String GetBindingTargetInfo( )
        {
            if ( _debugTargetBindingInfo == null )
                return "collection binding";

            return _debugTargetBindingInfo;
        }

        public override String GetSourceState( )
        {
            if ( SourceObject == null )
                return "Source not assigned";
            else if ( _viewCollectionDirectGetter != null )
            {
                var sourceCollection = _viewCollectionDirectGetter();
                return $"{sourceCollection.Count} data items";
            }
            else if ( _enumerableDirectGetter != null )
            {
                var sourceCollection = _enumerableDirectGetter().Cast<object>();
                return $"{sourceCollection.Count()} data items";
            }
        
            return "?";
        }

    }

    public class ViewCollection: IReadOnlyList<object>
    {
        public IReadOnlyList<object> SourceList => _sourceList;

        private readonly IReadOnlyList<object>       _sourceList;
        public readonly  Action<List<System.Object>> ProcessList;
        public readonly  Action<object, GameObject>  BindViewItem;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceList">Source list</param>
        /// <param name="processList">Sort/filer list to show</param>
        /// <param name="bindViewItem">init view item's instance with source list item</param>
        public ViewCollection( IReadOnlyList<object> sourceList, Action<List<object>> processList, Action<object, GameObject> bindViewItem )
        {
            _sourceList  = sourceList;
            ProcessList  = processList  ?? (_ => { });
            BindViewItem = bindViewItem ?? ((_, _) => { });
        }

        public IEnumerator<System.Object> GetEnumerator( )
        {
            return (IEnumerator<System.Object>)_sourceList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator( )
        {
            return _sourceList.GetEnumerator();
        }

        public Int32 Count  => _sourceList.Count;

        public System.Object this[ Int32 index ] => _sourceList[ index ];
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class CollectionBindingAttribute : PreserveAttribute
    {
        public String BindMethodName { get; }
        public String ProcessMethodName { get; }

        public CollectionBindingAttribute( ) : this(null, null)
        {

        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bindMethodName">Must has 2 params of type object and GameObject. First param - collection item, second param - instance of item view</param>
        /// <param name="processMethodName">Must has 1 param of type List&lt;object&gt;. You can sort/filter this list of collection items to modify visual representation</param>
        public CollectionBindingAttribute( String bindMethodName, String processMethodName = null )
        {
            BindMethodName            = bindMethodName;
            ProcessMethodName = processMethodName;
        }
    }
}