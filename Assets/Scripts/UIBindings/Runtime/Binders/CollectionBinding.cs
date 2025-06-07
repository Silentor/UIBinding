using System;
using System.Collections;
using System.Collections.Generic;
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
        public IReadOnlyList<object> SourceList => _sourceCollection?.SourceList;
        public IReadOnlyList<object> ViewList   => _processedList;

        public override Type    DataType => typeof(ViewCollection);
        public override Boolean IsTwoWay => false;

        public override    Object  GetDebugLastValue( )
        {
            return _sourceHashes;
        }

        public override    Boolean IsRuntimeValid => _isValid;

        public void Init( )
        {
            if ( !Enabled )   
                return;

            AssertWithContext.IsNotNull( Source, $"[{nameof(CollectionBinding)}] Source is not assigned at {_debugBindingInfo}", _debugHost );
            AssertWithContext.IsNotNull( Path, $"[{nameof(CollectionBinding)}] Path is not assigned at {_debugBindingInfo}", _debugHost );

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

        public event Action<object, IReadOnlyList<object>, Action<object, GameObject>> SourceChanged; 


        private readonly List<int>              _sourceHashes = new List<int>();
        private          ViewCollection         _sourceCollection;
        private          List<System.Object>    _processedList = new List<System.Object>();
        private          INotifyPropertyChanged _notifyPropertyChanged;
        private          Boolean                _propertyWasModified = true;
        private          Func<ViewCollection>   _viewCollectionGetter;
        private          Boolean                _isLastValueInitialized;
        private          Func<IEnumerable>      _enumerableGetter;
        private          Action<object, GameObject>             _bindMethod;
        private          Action<List<Object>>                _processMethod;

        protected static readonly ProfilerMarker ReadViewCollectionMarker     = new ( ProfilerCategory.Scripts,  $"{nameof(CollectionBinding)}.ReadViewCollection" );
        protected static readonly ProfilerMarker ReadIEnumerableMarker     = new ( ProfilerCategory.Scripts,  $"{nameof(CollectionBinding)}.ReadIEnumerable" );

        private Boolean IsEqual( IReadOnlyList<int> hashes, IReadOnlyList<object> sourceList )
        {
            if( hashes.Count != sourceList.Count )
                return false;

            for ( int i = 0; i < sourceList.Count; i++ )
            {
                if( sourceList[i].GetHashCode() != hashes[i] )
                    return false;
            }

            return true;
        }

        protected override void    CheckChangesInternal( )
        {
            if( _sourceNotify == null || _sourceChanged || !_isLastValueInitialized )
            {
                Action<List<object> > processListAction;
                Action<Object, GameObject> bindViewItemAction;
                _processedList.Clear();
                
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
                    Profiler.BeginSample( "BindingTestEnumerable" );//DEBUG
                    if ( enumerable != null )                        
                        _processedList.AddRange( enumerable.Cast<Object>() );
                    Profiler.EndSample();
                    processListAction = _processMethod;
                    bindViewItemAction = _bindMethod;
                    ReadIEnumerableMarker.End();
                }

                if( (!_isLastValueInitialized || _sourceChanged || !IsEqual( _sourceHashes, _processedList ) ))
                {
                    _isLastValueInitialized = true;
                    _sourceHashes.Clear();              
                    _sourceHashes.AddRange( _processedList.Select( v => v.GetHashCode() ) );

                    processListAction( _processedList );

                    UpdateTargetMarker.Begin( _debugSourceBindingInfo );
                    SourceChanged?.Invoke( Source, _processedList, bindViewItemAction );
                    UpdateTargetMarker.End( );
                }

                _sourceChanged = false;
            }
        }

        public readonly struct ModifiedEventArgs
        {
            public readonly IReadOnlyList<(object, int)> Added;
            public readonly IReadOnlyList<(object, int)> Removed;

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
        
        public CollectionBindingAttribute( String bindMethodName, String processMethodName = null )
        {
            BindMethodName            = bindMethodName;
            ProcessMethodName = processMethodName;
        }
    }
}