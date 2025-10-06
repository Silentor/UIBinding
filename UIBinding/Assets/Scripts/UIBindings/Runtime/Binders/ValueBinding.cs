using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
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

        public override Boolean IsInited => _isInited;

        public override Boolean IsCompatibleWith(Type type ) => type == typeof(T);

        public void Init( object sourceObject = null, bool forceOneWay = false )
        {
            if ( !Enabled )   
                return;

            Type sourceType = null;
            if(sourceObject != null )                   // Parameter has highest priority (as well as SourceObject property)
                sourceType = sourceObject.GetType();
            else if ( !BindToType && Source )
            {
                sourceType = Source.GetType();
                sourceObject = Source;
            }
            else if ( BindToType && !string.IsNullOrEmpty( SourceType ) )
            {
                sourceType = Type.GetType( SourceType, throwOnError: false );
                // sourceObject is null, but its okay, so binding will returns default value. Only type is crucial for init
            }

            if ( sourceType == null )
            {
                if(BindToType)
                    Debug.LogError( $"[{nameof(ValueBinding<T>)}] Failed to get source type, binding not inited. Provide correct type in property SourceType or set sourceObject parameter of method Init(). Binding: {GetBindingTargetInfo()}", _debugHost );
                else
                    Debug.LogError( $"[{nameof(ValueBinding<T>)}] Failed to get source type, binding not inited. Provide correct source object reference in property Source or set sourceObject parameter of method Init(). Binding: {GetBindingTargetInfo()}", _debugHost );
                return;
            }

            InitInfrastructure( sourceType, sourceObject, forceOneWay );
            SetSourceObjectWithoutNotify( sourceObject );
        }

        public event Action<Object, T> SourceChanged;

        private void InitInfrastructure( Type sourceType, object sourceObject, bool forceOneWay = false )
        {
            if ( !Enabled )   
                return;
            
            AssertWithContext.IsNotEmpty( Path, $"[{nameof(ValueBinding<T>)}] Path is not assigned at {GetBindingTargetInfo()}", _debugHost );

            var timer = ProfileUtils.GetTraceTimer( );

            //Here we process deep binding
            var pathProcessor = new PathProcessor( sourceType, Path, IsTwoWay, OnSourcePropertyChangedFromPathAdapter );
            timer.AddMarker( "GetProperty" );

            DataProvider lastDataSource = null;

            //Check fast pass - direct getter from property of source object
            PropertyInfo firstProperty;
            if ( Converters.Count == 0 && !pathProcessor.IsComplexPath && (firstProperty = pathProcessor.PeekNextPropertyInfo())?.PropertyType == typeof(T) )
            {
                _directGetter = CreateDirectGetter( sourceObject, firstProperty );
                _isSupportNotify = typeof(INotifyPropertyChanged).IsAssignableFrom( sourceType );
                _directPropertyInfo = firstProperty;
                timer.AddMarker( "DirectGetter" );
            }
            else        //Need adapters/converters/etc
            {
                while( pathProcessor.MoveNext( ) )
                {
                    timer.AddMarker( "CreateAdapter" );
                }

                _pathAdapter = pathProcessor.CurrentAdapter;
                lastDataSource = _pathAdapter;
                if( sourceObject != null )
                    _pathAdapter.SetSourceObject( sourceObject );

                //Prepare conversion chain
                var converters = _converters.Converters;
                if ( converters.Length > 0 )
                {
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
                        timer.AddMarker( "InitConv" );
                    }
                }

                if ( lastDataSource is IDataReader<T> compatibleConverter )
                {
                    _lastReader = compatibleConverter;
                    timer.AddMarker( "Connect" );           //Data pipeline is completed
                }
                else
                {
                    var lastImplicitConverter = ImplicitConversion.GetConverter( lastDataSource, typeof(T) );
                    if( lastImplicitConverter == null )
                    {
                        Debug.LogError( $"[{nameof(BindingBase)}] Cannot find implicit conversion from last converter {lastDataSource} to {typeof(T)} at binding {GetBindingTargetInfo()}", _debugHost );
                        return;
                    } 
                    _lastReader    = ( IDataReader<T> )lastImplicitConverter;
                    lastDataSource = lastImplicitConverter;
                    timer.AddMarker( "ConnectImplicitConverter" );
                }
            }
            
            OnInitInfrastructure( sourceObject, lastDataSource, forceOneWay, _debugHost );
            timer.AddMarker( "AdditionalInit" );
            var report = timer.StopAndGetReport();

            _isValueInitialized = false;
            _isInited = true;
            
            Debug.Log( $"[{nameof(ValueBinding<T>)}].[{nameof(InitInfrastructure)}] Inited {timer.Elapsed.TotalMicroseconds()} mks, {GetBindingTargetInfo()}, is two way {IsTwoWay}. Profile {report}", _debugHost );
        }

        protected virtual void OnInitInfrastructure(  Object source, DataProvider lastConverter, bool forceOneWay, MonoBehaviour debugHost  ) { }

        protected override void OnSetSourceObject( object oldValue, Object value )
        {
            if(!_isInited)
                throw new InvalidOperationException("Cannot set source object before binding is inited. Call Init() first.");

            if(_directGetter != null)
            {
                if( _isSupportNotify )
                {
                    if ( oldValue is INotifyPropertyChanged oldNotify )
                        oldNotify.PropertyChanged -= OnSourcePropertyChangedDirect;

                    if ( value is INotifyPropertyChanged newNotify )
                        newNotify.PropertyChanged += OnSourcePropertyChangedDirect;
                }

                _directGetter = CreateDirectGetter( value, _directPropertyInfo );
            }
            else
            {
                Assert.IsNotNull( _pathAdapter );
                _pathAdapter.SetSourceObject( value );
            }

            _isValueInitialized = false;
        }

        /// <summary>
        /// To get change event at desired time (LateUpdate for example)
        /// </summary>
        protected override void  CheckChangesInternal( )
        {
            if( !_isValueInitialized || !_isSupportNotify || _sourceValueChanged || _isTweened )
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
                    _isSupportNotify = _pathAdapter.IsSupportNotify;         //todo optimize? do not need to check if value not changed
                    ReadConvertedValueMarker.End();
                    //Debug.Log( $"Frame {Time.frameCount} checking changes for {GetType().Name} at {_hostName}" );
                }

                if( !_isValueInitialized || isChangedOnSource || !EqualityComparer<T>.Default.Equals( value, _lastValue ) ) //todo consider is comparison needs here because we already do same comparison in PropertyAdapter
                {
                    _isValueInitialized = true;
                    _lastValue              = value;

                    UpdateTargetMarker.Begin( ProfilerMarkerName );
                    SourceChanged?.Invoke( SourceObject, value );
                    UpdateTargetMarker.End( );
                }

                _sourceValueChanged = false;
            }
        }

        // Direct getter uses if path is just one property and no converters are used
        protected Func<T>        _directGetter;
        protected PropertyInfo _directPropertyInfo; //For recreating delegate if source object changes
        private static readonly Func<T> DefaultGetter = () => default; 
                                       
        // All other cases uses path adapter(s) as source of data
        private   PathAdapter   _pathAdapter;    //Last path adapter in the chain. Because path adapters are chained, this is enough to work with whole path

        protected T              _lastValue;
        protected IDataReader<T> _lastReader;       //Last reader in the chain (prop adapter or converter)
        private   Boolean        _isTweened;
        private   Boolean        _isSupportNotify;
        private   Type           _sourceObjectType;
        //private   Action<object, string> _sourceNotifyFromPropertyAdapterDelegate;

        protected override void OnSubscribe( )
        {
            base.OnSubscribe();

            if ( _directGetter != null )
            {
                if( _isSupportNotify )
                    ((INotifyPropertyChanged)SourceObject).PropertyChanged += OnSourcePropertyChangedDirect;
            }
            else
                _pathAdapter.Subscribe();
        }

        protected override void OnUnsubscribe( )
        {
            base.OnUnsubscribe();

            if ( _directGetter != null )
            {
                if( _isSupportNotify )
                    ((INotifyPropertyChanged)SourceObject).PropertyChanged -= OnSourcePropertyChangedDirect;
            }
            else
                _pathAdapter.Unsubscribe();
        }

        private void OnSourcePropertyChangedDirect(Object sender, String propertyName )
        {
            if ( String.IsNullOrEmpty( propertyName ) || String.Equals( propertyName, Path, StringComparison.Ordinal ) )
            {
                _sourceValueChanged = true;
            }
        }

        private void OnSourcePropertyChangedFromPathAdapter(Object sender, String propertyName )
        {
            //No property name checking needed, property adapter will check it
            _sourceValueChanged = true;
        }

        private Func<T> CreateDirectGetter( object sourceObject, PropertyInfo propertyInfo )
        {
            if ( sourceObject != null )
                return (Func<T>)Delegate.CreateDelegate( typeof(Func<T>), sourceObject,
                        propertyInfo.GetGetMethod( true ) );
            else
                return DefaultGetter;
        }

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
            if( !_isInited )
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
            else if ( _pathAdapter != null )
            {
                _pathAdapter.TryGetValue( out var propValue );
                return propValue?.ToString() ?? "null";
            }
        
            return "?";
        }

        // Special adapter to read source object from Binding in generic way. This allows to freely replace source object in Binding - the pipeline will work correctly.
        // private class SourceObjectReaderAdapter<TSource> : DataProvider, IDataReader<T>
        // {
        //     public SourceObjectReaderAdapter([NotNull] PropertyInfo propertyInfo, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged ) : base( propertyInfo, isTwoWayBinding, notifyPropertyChanged )
        //     {
        //     }
        //
        //     public override Type InputType => typeof(TSource);
        //     public override Type OutputType => typeof(TSource);
        //
        //     public override EResult TryGetValue(out object value )
        //     {
        //         throw new NotImplementedException();
        //     }
        //
        //     public override bool IsNeedPolling( )
        //     {
        //         throw new NotImplementedException();
        //     }
        //
        //     public EResult TryGetValue(out T value )
        //     {
        //         
        //     }
        //
        //     private TSource _lastSourceObject;
        //     private bool _isInited;
        // }
    }
}