using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UIBindings.Adapters;
using UIBindings.Converters;
using UIBindings.Runtime.Utils;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Assertions;
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
            
            AssertWithContext.IsNotEmpty( Path, $"[{nameof(ValueBinding<T>)}] Path is not assigned at {GetBindingTargetInfo()}", _debugHost );

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

            //Here we process deep binding
            var splitPath = Path.Split( '.' );
            var isComplexPath = splitPath.Length > 1;
            
            var firstPropertyName = splitPath.First();
            var firstProperty   = _sourceObjectType.GetProperty( firstPropertyName );

            timer.AddMarker( "GetProperty" );

            AssertWithContext.IsNotNull( firstProperty, $"Property {firstPropertyName} not found in {_sourceObjectType.Name}", _debugHost );
            AssertWithContext.IsTrue( firstProperty.CanRead, $"Property {firstPropertyName} in {_sourceObjectType.Name} must be readable for binding", _debugHost );

            _sourceNotify = SourceObject as INotifyPropertyChanged;
            DataProvider lastDataSource = null;

            //Check fast pass - direct getter
            if ( Converters.Count == 0 && !isComplexPath && firstProperty.PropertyType == typeof(T) )
            {
                _directGetter = (Func<T>)Delegate.CreateDelegate( typeof(Func<T>), SourceObject, firstProperty.GetGetMethod( true ) );

                //TODO logic to create open getter. Its support on-the-fly changing of source object because source object its just a parameter of a getter.
                //This also allows to cache the same getter for many source objects of the same type. May be useful for collection items view models.

                //var convertMethod       = this.GetType().GetMethod( nameof( CreateOpenDelegate ), BindingFlags.NonPublic | BindingFlags.Static );
                //var createdConvertMethod = convertMethod.MakeGenericMethod( _sourceObjectType );
                //var directOpenGetter = (Func<Object, T>) createdConvertMethod.Invoke( null, new Object[]{property.GetGetMethod( true )} );
                //_directGetter = () => directOpenGetter( _sourceObject );

                timer.AddMarker( "DirectGetter" );
            }
            else        //Need adapters/converters/etc
            {
                //Create first property adapter
                _propReader = PropertyAdapter.GetPropertyAdapter( SourceObject, firstProperty, IsTwoWay );
                lastDataSource = _propReader;

                //Create inner property adapters
                if ( isComplexPath )
                {
                    var sourceObjectType = firstProperty.PropertyType;
                    foreach ( var pathPart in splitPath.Skip( 1 ) )
                    {
                        var innerProp = sourceObjectType.GetProperty( pathPart );
                        AssertWithContext.IsNotNull( innerProp, $"Property {pathPart} not found in {sourceObjectType.Name} at binding {GetBindingTargetInfo()}", _debugHost );
                        AssertWithContext.IsTrue( innerProp.CanRead, $"Property {pathPart} in {sourceObjectType.Name} must be readable for binding at {GetBindingTargetInfo()}", _debugHost );
                        sourceObjectType = innerProp.PropertyType;
                        var innerPropAdapter = PropertyAdapter.GetInnerPropertyAdapter( _propReader, innerProp, IsTwoWay );
                        _propReader = innerPropAdapter;     //We will use last inner property to get value. Last property will process all previous inner properties.
                        lastDataSource = _propReader;
                    }
                }

                timer.AddMarker( "CreateAdapter" );

                //Prepare conversion chain
                var converters = _converters.Converters;
                if ( converters.Length > 0 )
                {
                    for ( int i = 0; i < converters.Length; i++ )   //Some converters can be reversed
                        converters[ i ] = converters[ i ].ReverseMode ? converters[ i ].GetReverseConverter() : converters[ i ];

                    timer.AddMarker( "ReverseConv" );

                    //Make each converter know about prev data source
                    for ( var i = 0; i < converters.Length; i++ )
                    {
                        var currentConverter = converters[i];

                        var result = currentConverter.InitAttachToSource( lastDataSource, IsTwoWay, !Update.ScaledTime );
                        if ( !result )
                        {
                            Debug.LogError( $"[{nameof(BindingBase)}] Converter {currentConverter} cannot be attached to previous data source {lastDataSource} at binding {GetBindingTargetInfo()}", _debugHost );
                            return;
                        }
                        lastDataSource = currentConverter;
                        timer.AddMarker( $"InitConv{i}" );
                    }
                }

                if ( lastDataSource is IDataReader<T> compatibleConverter )
                {
                    _lastReader = compatibleConverter;
                    timer.AddMarker( "Connect" );
                }
                else
                {
                    var lastImplicitConverter = ImplicitConversion.GetConverter( lastDataSource, typeof(T) );
                    if( lastImplicitConverter == null )
                    {
                        Debug.LogError( $"[{nameof(BindingBase)}] Cannot find implicit conversion from last converter {lastDataSource} to {typeof(T)} at binding {GetBindingTargetInfo()}", _debugHost );
                        return;
                    } 
                    _lastReader = ( IDataReader<T> )lastImplicitConverter;
                    lastDataSource = lastImplicitConverter;
                    timer.AddMarker( "ConnectImplicitConverter" );
                }
            }
            
            DoInitInfrastructure( SourceObject, firstProperty, lastDataSource, forceOneWay, _debugHost );
            timer.AddMarker( "AdditionalInit" );
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
        private   PropertyAdapter _propReader;
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

            _debugTargetBindingInfo = $"{typeof(T).Name} '{host.name}'({host.GetType().Name}).{bindingName}";
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
            return $"{_lastValue}{(_isTweened ? " (tween)" : string.Empty)}";
        }

        public override String GetBindingTargetInfo( )
        {
            if ( _debugTargetBindingInfo == null )
                return $"{typeof(T).Name} value binding";

            return _debugTargetBindingInfo;
        }

        public override String GetSourceState( )
        {
            if ( SourceObject == null )
                return "Source not assigned";
            else if ( _directGetter != null )
            {
                return _directGetter()?.ToString() ?? "null";
            }
            else if ( _propReader != null )
            {
                _propReader.TryGetValue( out var propValue );
                return propValue?.ToString() ?? "null";
            }
        
            return "?";
        }
    }
}