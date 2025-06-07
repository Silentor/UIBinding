using System;
using System.Collections.Generic;
using System.Reflection;
using UIBindings.Adapters;
using UIBindings.Converters;
using UIBindings.Runtime.Utils;
using Unity.Profiling;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = System.Object;

namespace UIBindings
{
    [Serializable]
    public class ValueBinding<T> : DataBinding
    {
        public override Boolean IsTwoWay => false;

        public override Type DataType => typeof(T);

        public override Boolean IsRuntimeValid => _isValid;

        public void Init( bool forceOneWay = false )
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
            if ( Converters.Count == 0 && property.PropertyType == typeof(T) && property.CanRead )
            {
                _directGetter = (Func<T>)Delegate.CreateDelegate( typeof(Func<T>), Source, property.GetGetMethod( true ) );
                timer.AddMarker( "DirectGetter" );
            }
            else        //Need adapter/converters/etc
            {
                //First prepare property reader
                var propReader = PropertyAdapter.GetPropertyAdapter( Source, property, IsTwoWay );
                lastConverter = propReader;

                timer.AddMarker( "CreateAdapter" );

                //Prepare conversion chain
                var converters = _converters.Converters;
                if ( converters.Length > 0 )
                {
                    for ( int i = 0; i < converters.Length; i++ )
                        converters[ i ] = converters[ i ].ReverseMode ? converters[ i ].GetReverseConverter() : converters[ i ];

                    timer.AddMarker( "ReverseConv" );

                    //Connect first converter to source property
                    var firstConverter                = converters[0];
                    var result = firstConverter.InitAttachToSource( propReader, IsTwoWay, !Update.ScaledTime );
                    if( !result )
                    {
                        Debug.LogError( $"[{nameof(BindingBase)}] First converter {firstConverter} cannot be attached to source property adapter {propReader} at binding {_debugSourceBindingInfo}", _debugHost );
                        return;
                    }
                    lastConverter = firstConverter;
                    timer.AddMarker( "InitConv0" );

                    //Make each converter know about prev converter
                    for ( var i = 1; i < converters.Length; i++ )
                    {
                        var prevConverter = converters[i - 1];
                        var currentConverter = converters[i];

                        result = converters[i].InitAttachToSource( converters[ i - 1], IsTwoWay, !Update.ScaledTime );
                        if ( !result )
                        {
                            Debug.LogError( $"[{nameof(BindingBase)}] Converter {currentConverter} cannot be attached to previous converter {prevConverter} at binding {_debugSourceBindingInfo}", _debugHost );
                            return;
                        }
                        lastConverter = converters[ i ];
                        timer.AddMarker( $"InitConv{i}" );
                    }
                }

                if ( lastConverter is IDataReader<T> compatibleConverter )
                {
                    _lastReader = compatibleConverter;
                    timer.AddMarker( "Connect" );
                }
                else
                {
                    var lastConverterToBindingConverter = ImplicitConversion.GetConverter( lastConverter, typeof(T) );
                    if( lastConverterToBindingConverter == null )
                    {
                        Debug.LogError( $"[{nameof(BindingBase)}] Cannot find implicit conversion from last converter {lastConverter} to {typeof(T)} at binding {_debugSourceBindingInfo}", _debugHost );
                        return;
                    } 
                    _lastReader = ( IDataReader<T> )lastConverterToBindingConverter;
                    timer.AddMarker( "ConnectImplicitConversion" );
                    lastConverter = lastConverterToBindingConverter;
                }
            }
            
            DoInit( Source, property, lastConverter, forceOneWay, _debugHost );

            timer.AddMarker( "TwoWayAwake" );
            var report = timer.GetReport();
             
            _isValid = true;
            
            Debug.Log( $"Awaked {timer.Elapsed.TotalMicroseconds()} mks, {_debugSourceBindingInfo}, is two way {IsTwoWay}, notify support {(_sourceNotify != null)}: {report}" );
        }

        public event Action<Object, T> SourceChanged;

        public override Object GetDebugLastValue( )
        {
            if( !Enabled )
                return "not enabled";
            if( !_isValid )
                return "not valid";
            if( !_isLastValueInitialized )
                return "not initialized";
            return _lastValue;
        }

        protected virtual void DoInit(  Object source, PropertyInfo property, DataProvider lastConverter, bool forceOneWay, MonoBehaviour debugHost  ) { }

        /// <summary>
        /// To get change event at desired time (LateUpdate for example)
        /// </summary>
        protected override void CheckChangesInternal( )
        {
            if( _sourceNotify == null || _sourceChanged || !_isLastValueInitialized || _isTweened )
            {
                T   value;
                var isChangedOnSource = true;
                if ( _directGetter != null )
                {
                    ReadDirectValueMarker.Begin( _debugSourceBindingInfo );
                    value = _directGetter();
                    ReadDirectValueMarker.End();
                }
                else
                {
                    ReadConvertedValueMarker.Begin( _debugSourceBindingInfo );
                    var changeStatus = _lastReader.TryGetValue( out value );
                    isChangedOnSource = changeStatus != EResult.NotChanged;
                    _isTweened        = changeStatus == EResult.Tweened;
                    ReadConvertedValueMarker.End();
                    //Debug.Log( $"Frame {Time.frameCount} checking changes for {GetType().Name} at {_hostName}" );
                }

                if( isChangedOnSource && (!_isLastValueInitialized || !EqualityComparer<T>.Default.Equals( value, _lastValue ) ))
                {
                    _isLastValueInitialized = true;
                    _lastValue              = value;

                    UpdateTargetMarker.Begin( _debugSourceBindingInfo );
                    SourceChanged?.Invoke( Source, value );
                    UpdateTargetMarker.End( );
                }

                _sourceChanged = false;
            }
        }

        protected Func<T>        _directGetter;
        protected T              _lastValue;
        protected bool           _isLastValueInitialized;
        protected IDataReader<T> _lastReader;
        private   Boolean        _isTweened;

        //Debug data, to make useful logs is something goes wrong
    }

}