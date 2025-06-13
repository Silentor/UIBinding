using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
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

        public override    Object  GetDebugLastValue( )
        {
            return _sourceCopy;
        }

        public override    Boolean IsRuntimeValid => _isValid;

        public void Init( )
        {
            if ( !Enabled )   
                return;

            AssertWithContext.IsNotNull( Source, $"[{nameof(CollectionBinding)}] Source is not assigned at {_debugTargetBindingInfo}", _debugHost );
            AssertWithContext.IsNotNull( Path, $"[{nameof(CollectionBinding)}] Path is not assigned at {_debugTargetBindingInfo}", _debugHost );

            var timer = ProfileUtils.GetTraceTimer(); 

            var sourceType = Source.GetType();
            var property   = sourceType.GetProperty( Path );

            timer.AddMarker( "GetProperty" );

            AssertWithContext.IsNotNull( property, $"Property {Path} not found in {sourceType.Name}", _debugHost );
            AssertWithContext.IsTrue( property.CanRead, $"Property {Path} in {sourceType.Name} must be readable for binding", _debugHost );

            _sourceNotify = Source as INotifyPropertyChanged;
            DataProvider lastConverter = null;

            //Check fast pass - direct getter
            if ( Converters.Count == 0 )
            {
                if ( property.PropertyType == typeof(ViewCollection) )
                {
                    _viewCollectionGetter = (Func<ViewCollection>)Delegate.CreateDelegate( typeof(Func<ViewCollection>), Source, property.GetGetMethod( true ) );
                    timer.AddMarker( "ViewCollectionGetter" );
                }
                else if( typeof(IEnumerable).IsAssignableFrom( property.PropertyType ) )
                {
                    _enumerableGetter = (Func<IEnumerable>)Delegate.CreateDelegate( typeof(Func<IEnumerable>), Source, property.GetGetMethod( true ) );
                    var bindingAttribute = property.GetCustomAttribute<CollectionBindingAttribute>();
                    if( bindingAttribute != null )
                    {
                        if( bindingAttribute.BindMethodName != null )
                        {
                            var bindMethodInfo = sourceType.GetMethod( bindingAttribute.BindMethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
                            if( bindMethodInfo != null )
                                _bindMethod = (Action<object, GameObject>)Delegate.CreateDelegate( typeof(Action<object, GameObject>), Source, bindMethodInfo );
                            var processMethodInfo = sourceType.GetMethod( bindingAttribute.ProcessMethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
                            if( processMethodInfo != null )
                                _processMethod = (Action<List<Object>>)Delegate.CreateDelegate( typeof(Action<List<Object>>), Source, processMethodInfo );
                        }
                    }
                    timer.AddMarker( "IEnumerableGetter" );
                }
                else
                {
                    throw new NotSupportedException($"CollectionBinding does not support collection type {property.PropertyType } yet.");
                }
            }
            else        //Need adapter/converters/etc
            {
                throw new NotSupportedException("CollectionBinding does not support converters yet.");
            }

            timer.AddMarker( "TwoWayAwake" );
            var report = timer.GetReport();
             
            _isValid = true;
            
            Debug.Log( $"[{nameof(CollectionBinding)}] Awaked {timer.Elapsed.TotalMicroseconds()} mks, {_debugSourceBindingInfo}, is two way {IsTwoWay}, notify support {(_sourceNotify != null)}: {report}" );
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
        private          INotifyPropertyChanged     _notifyPropertyChanged;

        //Source property access getters
        private          Func<ViewCollection>       _viewCollectionGetter;
        private          Func<IEnumerable>          _enumerableGetter;

        //Collection process methods
        private          Action<object, GameObject> _bindMethod;
        private          Action<List<Object>>       _processMethod;

        protected static readonly ProfilerMarker ReadViewCollectionMarker     = new ( ProfilerCategory.Scripts,  $"{nameof(CollectionBinding)}.ReadViewCollection" );
        protected static readonly ProfilerMarker ReadIEnumerableMarker     = new ( ProfilerCategory.Scripts,  $"{nameof(CollectionBinding)}.ReadIEnumerable" );

        private Boolean IsEqual( IReadOnlyList<object> hashes, IReadOnlyList<object> sourceList )
        {
            if( hashes.Count != sourceList.Count )
                return false;

            for ( int i = 0; i < sourceList.Count; i++ )
            {
                if( sourceList[i] != hashes[i] )
                    return false;
            }

            return true;
        }

        private bool IsEquivalent( IReadOnlyList<object> oldList, IReadOnlyList<object> newList )
        {
            if ( oldList.Count != newList.Count )
                return false;

            for ( int i = 0; i < oldList.Count; i++ )
            {
                if ( !Equals( oldList[i], newList[i] ) )
                    return false;
            }

            return true;
        }

        protected override void    CheckChangesInternal( )
        {
            if( _sourceNotify == null || _sourceChanged || !_isValueInitialized )
            {
                Action<List<object> > processListAction;
                Action<Object, GameObject> bindViewItemAction;
                _processedList.Clear();
                
                //Read source collection
                if ( _viewCollectionGetter != null )
                {
                    ReadViewCollectionMarker.Begin( _debugSourceBindingInfo );
                    var viewCollection = _viewCollectionGetter();
                    _processedList.AddRange( viewCollection );
                    processListAction = viewCollection.ProcessList;
                    bindViewItemAction = viewCollection.BindViewItem;
                    ReadViewCollectionMarker.End();
                }
                else
                {
                    ReadIEnumerableMarker.Begin( _debugSourceBindingInfo );
                    var enumerable = _enumerableGetter();
                    if ( enumerable != null )                        
                        _processedList.AddRange( enumerable.Cast<Object>() );
                    processListAction = _processMethod;
                    bindViewItemAction = _bindMethod;
                    ReadIEnumerableMarker.End();
                }

                //Check for source collection modifications
                if( !_isValueInitialized || (_sourceNotify != null ? _sourceChanged : !IsEqual( _sourceCopy, _processedList )))
                {
                    _isValueInitialized = true;
                    _sourceCopy.Clear();              
                    _sourceCopy.AddRange( _processedList );

                    processListAction?.Invoke( _processedList );
                    BindViewItemMethod = bindViewItemAction;

                    UpdateTargetMarker.Begin( _debugSourceBindingInfo );
                    CompareAndFireEvents( _processedCopy, _processedList );
                    _processedCopy.Clear();
                    _processedCopy.AddRange( _processedList );
                    UpdateTargetMarker.End( );
                }

                _sourceChanged = false;
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

            _debugTargetBindingInfo = $"Collection '{host.name}'.{bindingName}";
        }

        public override String GetBindingState( )
        {
            if( !Enabled )
                return "Disabled";
            if( !_isValid )
                return "Invalid";
            if( !_isValueInitialized )
                return "Not initialized";
            return $"Value: {_processedCopy.Count} view items";
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