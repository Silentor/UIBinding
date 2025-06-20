using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
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

        public override Boolean IsCompatibleWith(Type type ) => type == typeof(T);

        public void Init( object sourceObject = null, bool forceOneWay = false )
        {
            if ( !Enabled )   
                return;
          
            if ( BindToType )
            {
                if ( sourceObject == null )
                {
                    Debug.LogError( $"[{nameof(ValueBinding<T>)}] Source object must be assigned for binding {GetBindingTargetInfo()} from the code. Assigned object must be {SourceType} type", _debugHost );
                    return;
                }
                SourceObject = sourceObject;
            }
            else
            {
                if ( !Source )
                {
                    var unityObjectSource = sourceObject as UnityEngine.Object;
                    if( !unityObjectSource )
                    {
                        Debug.LogError( $"[{nameof(ValueBinding<T>)}] Source object must be assigned for binding {GetBindingTargetInfo()} from the Inspector", _debugHost );
                        return;
                    }
                    SourceObject = unityObjectSource;
                }
                else
                    SourceObject = Source;
            }

            InitInfrastructure( forceOneWay );
        }

        private void InitInfrastructure( bool forceOneWay = false )
        {
            if ( !Enabled )   
                return;
            
            AssertWithContext.IsNotNull( Path, $"[{nameof(ValueBinding<T>)}] Path is not assigned at {GetBindingTargetInfo()}", _debugHost );

            var timer = ProfileUtils.GetTraceTimer( );

            if ( BindToType )
            {
                _sourceObjectType = SourceObject.GetType();
                AssertWithContext.IsNotNull( _sourceObjectType, $"[{nameof(ValueBinding<T>)}] SourceType '{SourceType}' not found at {GetBindingTargetInfo()}", _debugHost );
                AssertWithContext.IsTrue( _sourceObjectType.IsInstanceOfType( SourceObject ),
                    $"[{nameof(ValueBinding<T>)}] Source object {SourceObject.GetType().Name}  is not compartible with declared source object type '{_sourceObjectType.Name}' at {GetBindingTargetInfo()}", _debugHost );
            }
            else
            {
                _sourceObjectType = SourceObject.GetType();
            }

            var property   = _sourceObjectType.GetProperty( Path );

            timer.AddMarker( "GetProperty" );

            AssertWithContext.IsNotNull( property, $"Property {Path} not found in {_sourceObjectType.Name}", _debugHost );
            AssertWithContext.IsTrue( property.CanRead, $"Property {Path} in {_sourceObjectType.Name} must be readable for binding", _debugHost );

            _sourceNotify = SourceObject as INotifyPropertyChanged;
            DataProvider lastConverter = null;

            //Check fast pass - direct getter
            if ( Converters.Count == 0 && property.PropertyType == typeof(T) && property.CanRead )
            {
                _directGetter = (Func<T>)Delegate.CreateDelegate( typeof(Func<T>), SourceObject, property.GetGetMethod( true ) );

                //TODO logic to create open getter. Its support on-the-fly changing of source object because source object its just a parameter of a getter.
                //This also allows to cache the same getter for many source objects of the same type. May be useful for collection items view models.

                //var convertMethod       = this.GetType().GetMethod( nameof( CreateOpenDelegate ), BindingFlags.NonPublic | BindingFlags.Static );
                //var createdConvertMethod = convertMethod.MakeGenericMethod( _sourceObjectType );
                //var directOpenGetter = (Func<Object, T>) createdConvertMethod.Invoke( null, new Object[]{property.GetGetMethod( true )} );
                //_directGetter = () => directOpenGetter( _sourceObject );

                timer.AddMarker( "DirectGetter" );
            }
            else        //Need adapter/converters/etc
            {
                //First prepare property reader
                var propReader = PropertyAdapter.GetPropertyAdapter( SourceObject, property, IsTwoWay );
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
                        Debug.LogError( $"[{nameof(BindingBase)}] First converter {firstConverter} cannot be attached to source property adapter {propReader} at binding {GetBindingTargetInfo()}", _debugHost );
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
                            Debug.LogError( $"[{nameof(BindingBase)}] Converter {currentConverter} cannot be attached to previous converter {prevConverter} at binding {GetBindingTargetInfo()}", _debugHost );
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
                        Debug.LogError( $"[{nameof(BindingBase)}] Cannot find implicit conversion from last converter {lastConverter} to {typeof(T)} at binding {GetBindingTargetInfo()}", _debugHost );
                        return;
                    } 
                    _lastReader = ( IDataReader<T> )lastConverterToBindingConverter;
                    timer.AddMarker( "ConnectImplicitConversion" );
                    lastConverter = lastConverterToBindingConverter;
                }
            }
            
            DoInitInfrastructure( SourceObject, property, lastConverter, forceOneWay, _debugHost );

            timer.AddMarker( "TwoWayAwake" );
            var report = timer.StopAndGetReport();

            _isValueInitialized = false;
            _isValid = true;
            
            Debug.Log( $"Awaked {timer.Elapsed.TotalMicroseconds()} mks, {GetBindingTargetInfo()}, is two way {IsTwoWay}, notify support {(_sourceNotify != null)}. Profile {report}", _debugHost );
        }

        public event Action<Object, T> SourceChanged;

        public override Boolean IsRuntimeValid => _isValid;

        protected virtual void DoInitInfrastructure(  Object source, PropertyInfo property, DataProvider lastConverter, bool forceOneWay, MonoBehaviour debugHost  ) { }

        /// <summary>
        /// To get change event at desired time (LateUpdate for example)
        /// </summary>
        protected override void  CheckChangesInternal( )
        {
            if( _sourceNotify == null || _sourceChanged || !_isValueInitialized || _isTweened )
            {
                T   value;
                var isChangedOnSource = true;
                if ( _directGetter != null )
                {
                    var timer = new Stopwatch();
                    ReadDirectValueMarker.Begin( ProfilerMarkerName );
                    timer.Start();
                    value = _directGetter( );
                    timer.Stop();
                    ReadDirectValueMarker.End();

                    //Debug.Log( timer.Elapsed.TotalMicroseconds() );
                }
                else
                {
                    ReadConvertedValueMarker.Begin( ProfilerMarkerName );
                    var changeStatus = _lastReader.TryGetValue( out value );
                    isChangedOnSource = changeStatus != EResult.NotChanged;
                    _isTweened        = changeStatus == EResult.Tweened;
                    ReadConvertedValueMarker.End();
                    //Debug.Log( $"Frame {Time.frameCount} checking changes for {GetType().Name} at {_hostName}" );
                }

                if( isChangedOnSource && (!_isValueInitialized || !EqualityComparer<T>.Default.Equals( value, _lastValue ) ))
                {
                    _isValueInitialized = true;
                    _lastValue              = value;

                    UpdateTargetMarker.Begin( ProfilerMarkerName );
                    SourceChanged?.Invoke( SourceObject, value );
                    UpdateTargetMarker.End( );
                }

                _sourceChanged = false;
            }
        }

        protected Func<T>        _directGetter;
        protected T              _lastValue;
        protected IDataReader<T> _lastReader;
        private   Boolean        _isTweened;
        private   Type           _sourceObjectType;

        //Create open delegate for getter. Source object is passed as parameter.
        // private static Func<Object, T> CreateOpenDelegate<TSource>( MethodInfo method )
        // {
        //     var            strongTyped = (Func<TSource, T>) Delegate.CreateDelegate( typeof(Func<TSource, T>), method );
        //     Func<Object, T> weakTyped   = (sourceObj) => strongTyped( (TSource)sourceObj );
        //     return weakTyped;
        // }

        public override void SetDebugInfo( MonoBehaviour host, String bindingName )
        {
            base.SetDebugInfo( host, bindingName );

            _debugTargetBindingInfo = $"{typeof(T).Name} '{host.name}'.{bindingName}";
            ProfilerMarkerName = GetBindingTargetInfo();
        }

        public override String GetBindingState( )
        {
            if( !Enabled )
                return "Disabled";
            if( !_isValid )
                return "Invalid";
            if( !_isValueInitialized )
                return "Not initialized";
            return $"Value: {_lastValue} {(_isTweened ? "(tween)" : string.Empty)}";
        }

        public override String GetBindingTargetInfo( )
        {
            if ( _debugTargetBindingInfo == null )
                return $"{typeof(T).Name} value binding";

            return _debugTargetBindingInfo;
        }
    }
}