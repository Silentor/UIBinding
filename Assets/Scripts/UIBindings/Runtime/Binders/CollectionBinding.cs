using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UIBindings.Runtime.Utils;
using UnityEngine;
using Object = System.Object;
using Unity.Profiling;

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

            if ( !Source )
            {
                Debug.LogError( $"[{nameof(CollectionBinding)}] Source is not assigned at {_debugBindingInfo}", _debugHost );
                return;
            }

            if ( String.IsNullOrEmpty( Path ) )
            {
                Debug.LogError( $"[{nameof(CollectionBinding)}] Path is not assigned at {_debugBindingInfo}", _debugHost );
                return;
            }

            var timer = ProfileUtils.GetTraceTimer(); 

            var sourceType = Source.GetType();
            var property   = sourceType.GetProperty( Path );

            timer.AddMarker( "GetProperty" );

            if ( property == null )
            {
                Debug.LogError( $"[{nameof(BindingBase)}] Property {Path} not found in {sourceType.Name}", _debugHost );
                return;
            }

            _sourceNotify = Source as INotifyPropertyChanged;
            DataProvider lastConverter = null;

            //Check fast pass - direct getter
            if ( Converters.Count == 0 && property.PropertyType == typeof(ViewCollection) && property.CanRead )
            {
                _directGetter = (Func<ViewCollection>)Delegate.CreateDelegate( typeof(Func<ViewCollection>), Source, property.GetGetMethod( true ) );
                timer.AddMarker( "DirectGetter" );
            }
            else        //Need adapter/converters/etc
            {
                throw new NotSupportedException("CollectionBinding does not support converters yet.");
            }

            timer.AddMarker( "TwoWayAwake" );
            var report = timer.GetReport();
             
            _isValid = true;
            
            Debug.Log( $"[{_debugSourceBindingInfo}] Awaked {timer.Elapsed.TotalMicroseconds()} mks, {_debugSourceBindingInfo}, is two way {IsTwoWay}: {report}" );
        }

        public event Action<object, IReadOnlyList<object>, Action<object, GameObject>> SourceChanged; 

        private readonly List<int>           _sourceHashes = new List<int>();
        private          ViewCollection         _sourceCollection;
        private          List<System.Object>    _processedList = new List<System.Object>();
        private          INotifyPropertyChanged _notifyPropertyChanged;
        private          Boolean                _propertyWasModified = true;
        private          Func<ViewCollection>   _directGetter;
        private          Boolean                _isLastValueInitialized;

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
                ViewCollection value;
                var            isChangedOnSource = true;
                if ( _directGetter != null )
                {
                    ReadDirectValueMarker.Begin( _debugSourceBindingInfo );
                    value = _directGetter();
                    ReadDirectValueMarker.End();
                }
                else
                {
                    throw new NotImplementedException("CollectionBinding does not support converters yet.");
                }

                if( isChangedOnSource && (!_isLastValueInitialized || _sourceChanged || !IsEqual( _sourceHashes, value ) ))
                {
                    _isLastValueInitialized = true;
                    _sourceHashes.Clear();              
                    _sourceHashes.AddRange( value.Select( v => v.GetHashCode() ) );

                    _processedList.Clear();
                    _processedList.AddRange( value );
                    var processListAction = value.ProcessList;
                    processListAction( _processedList );

                    UpdateTargetMarker.Begin( _debugSourceBindingInfo );
                    SourceChanged?.Invoke( Source, _processedList, value.BindViewItem );
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
}