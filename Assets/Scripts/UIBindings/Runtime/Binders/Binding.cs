﻿using System;
using System.Collections.Generic;
using System.Reflection;
using UIBindings.Adapters;
using UIBindings.Converters;
using UIBindings.Runtime.PlayerLoop;
using UIBindings.Runtime.Utils;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;
using Object = System.Object;

namespace UIBindings
{
    [Serializable]
    public abstract class Binding
    {
        public        Boolean                   Enabled             = true;                        //Checked once on start!

        public        UnityEngine.Object        Source;
        public        String                    SourceType;
        public        Boolean                   BindToType;

        public        String                    Path;
    }

    [Serializable]
    public abstract class DataBinding : Binding
    {
        public abstract bool IsTwoWay { get; }

        public abstract Type DataType { get; }

        public UpdateMode Update = new (){Mode = EUpdateMode.AfterLateUpdate};

        [SerializeField]
        protected ConvertersList _converters = new (){Converters = Array.Empty<ConverterBase>()};
        public       IReadOnlyList<ConverterBase> Converters => _converters.Converters;
        public const String                       ConvertersPropertyName = nameof(_converters);

        //Mostly debug
        public abstract Object  GetDebugLastValue( );
        public abstract bool    IsRuntimeValid { get; }

        public static (Type valueType, Type templateType) GetBindingTypeInfo( Type bindingType )
        {
            Assert.IsTrue( typeof(Binding).IsAssignableFrom( bindingType ) );

            while (bindingType != null)
            {
                if (bindingType.IsGenericType 
                    && ( bindingType.GetGenericTypeDefinition() == typeof(Binding<>) || bindingType.GetGenericTypeDefinition() == typeof(BindingTwoWay<>) ) )
                {
                    var valueType = bindingType.GetGenericArguments()[0];
                    var template  = bindingType.GetGenericTypeDefinition();
                    return ( valueType, template );
                }
                bindingType = bindingType.BaseType;
            }
            throw new InvalidOperationException("Base type was not found");
        }

        [Serializable]
        public class ConvertersList
        {
            [SerializeReference]
            public ConverterBase[] Converters;
        }

        [Serializable]
        public struct UpdateMode
        {
            public EUpdateMode  Mode;
            public float        Delay;
            public bool         UnscaledTime;
        }

        public enum EUpdateMode
        {
            AfterUpdate,
            BeforeLateUpdate,
            AfterLateUpdate,
            Manual
        }

        protected static readonly ProfilerMarker ReadDirectValueMarker = new ( ProfilerCategory.Scripts,  $"{nameof(Binding)}.ReadDirectValue" );
        protected static readonly ProfilerMarker WriteDirectValueMarker = new ( ProfilerCategory.Scripts,  $"{nameof(Binding)}.WriteDirectValue" );
        protected static readonly ProfilerMarker ReadConvertedValueMarker = new ( ProfilerCategory.Scripts,  $"{nameof(Binding)}.ReadConvertedValue" );
        protected static readonly ProfilerMarker WriteConvertedValueMarker = new ( ProfilerCategory.Scripts,  $"{nameof(Binding)}.WriteConvertedValue" );
        protected static readonly ProfilerMarker UpdateTargetMarker = new ( ProfilerCategory.Scripts,  $"{nameof(Binding)}.UpdateTarget" );
    }

    [Serializable]
    public class Binding<T> : DataBinding, IAfterLateUpdate, IBeforeLateUpdate, IUpdate
    {
        public override Boolean IsTwoWay => false;

        public override Type DataType => typeof(T);

        public override Boolean IsRuntimeValid => _isValid;

        /// <summary>
        /// May be called before Awake for useful logs in case of errors.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="bindingName"></param>
        public void SetDebugInfo( MonoBehaviour host, String bindingName )
        {
            _debugHost = host;
            _debugHostName = host.name;
            _debugBindingName = bindingName;
            _debugBindingInfo = $"{typeof(T).Name} {host.name}.{bindingName}";
            var sourceName = Source ? Source.name : "null";
            var direction = IsTwoWay ? "<->" : "->";
            _debugSourceBindingInfo = $"{sourceName}.{Path} {direction} {_debugBindingInfo}";
        }

        public void Awake( Object sourceObject = null )
        {
            if ( !Enabled )   
                return;

            if ( !Source )
            {
                Debug.LogError( $"[{nameof(Binding)}] Source is not assigned at {_debugBindingInfo}", _debugHost );
                return;
            }

            if ( String.IsNullOrEmpty( Path ) )
            {
                Debug.LogError( $"[{nameof(Binding)}] Path is not assigned at {_debugBindingInfo}", _debugHost );
                return;
            }

            var timer = ProfileUtils.GetTraceTimer(); 

            var sourceType = Source.GetType();
            var property   = sourceType.GetProperty( Path );

            timer.AddMarker( "GetProperty" );

            if ( property == null )
            {
                Debug.LogError( $"[{nameof(Binding)}] Property {Path} not found in {sourceType.Name}", _debugHost );
                return;
            }

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
                    var result = firstConverter.InitAttachToSource( propReader, IsTwoWay, Update.UnscaledTime );
                    if( !result )
                    {
                        Debug.LogError( $"[{nameof(Binding)}] First converter {firstConverter} cannot be attached to source property adapter {propReader} at binding {_debugSourceBindingInfo}", _debugHost );
                        return;
                    }
                    lastConverter = firstConverter;
                    timer.AddMarker( "InitConv0" );

                    //Make each converter know about prev converter
                    for ( var i = 1; i < converters.Length; i++ )
                    {
                        var prevConverter = converters[i - 1];
                        var currentConverter = converters[i];

                        result = converters[i].InitAttachToSource( converters[ i - 1], IsTwoWay, Update.UnscaledTime );
                        if ( !result )
                        {
                            Debug.LogError( $"[{nameof(Binding)}] Converter {currentConverter} cannot be attached to previous converter {prevConverter} at binding {_debugSourceBindingInfo}", _debugHost );
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
                        Debug.LogError( $"[{nameof(Binding)}] Cannot find implicit conversion from last converter {lastConverter} to {typeof(T)} at binding {_debugSourceBindingInfo}", _debugHost );
                        return;
                    } 
                    _lastReader = ( IDataReader<T> )lastConverterToBindingConverter;
                    timer.AddMarker( "ConnectImplicitConversion" );
                    lastConverter = lastConverterToBindingConverter;
                }
            }
            
            DoAwake( Source, property, lastConverter, _debugHost );

            timer.AddMarker( "TwoWayAwake" );
            var report = timer.GetReport();
             
            _isValid = true;
            
            Debug.Log( $"Awaked {timer.Elapsed.TotalMicroseconds()} mks, {_debugSourceBindingInfo}, is two way {IsTwoWay}: {report}" );
        }

        public void Subscribe()
        {
            if( !Enabled || !_isValid || _isSubscribed ) return;

            if ( _sourceNotify != null )                
                _sourceNotify.PropertyChanged += OnSourcePropertyChanged;
            switch ( Update.Mode )
            {
                case EUpdateMode.AfterLateUpdate:   UpdateManager.RegisterAfterLateUpdate( this ); break;
                case EUpdateMode.BeforeLateUpdate:  UpdateManager.RegisterBeforeLateUpdate( this ); break;
                case EUpdateMode.AfterUpdate:       UpdateManager.RegisterUpdate( this ); break;
                case EUpdateMode.Manual:            break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _currentUpdateMode = Update.Mode;

            _isSubscribed = true;
        }

        public void Unsubscribe()
        {
            if( !Enabled || !_isValid || !_isSubscribed ) return;

            if ( _sourceNotify != null )                
                _sourceNotify.PropertyChanged -= OnSourcePropertyChanged;
            switch ( _currentUpdateMode )
            {
                case EUpdateMode.AfterLateUpdate:  UpdateManager.UnregisterAfterLateUpdate( this ); break;
                case EUpdateMode.BeforeLateUpdate: UpdateManager.UnregisterBeforeLateUpdate( this ); break;
                case EUpdateMode.AfterUpdate:      UpdateManager.UnregisterUpdate( this ); break;
                case EUpdateMode.Manual:           break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _isSubscribed = false;
        }

        public void ManuallyCheckChanges( )
        {
            if ( !Enabled || !_isValid || !_isSubscribed ) return;

            CheckChangesInternal( );
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

        protected virtual void DoAwake(  Object source, PropertyInfo property, DataProvider lastConverter, MonoBehaviour debugHost  ) { }

        private void OnSourcePropertyChanged(Object sender, String propertyName )
        {
            if ( String.IsNullOrEmpty( propertyName ) || String.Equals( propertyName, Path, StringComparison.Ordinal ) )
            {
                _sourceChanged = true;
            }
        }

        /// <summary>
        /// To get change event at desired time (LateUpdate for example)
        /// </summary>
        private void CheckChangesInternal( )
        {
            if( _sourceNotify == null || _sourceChanged || _isTweened )
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

        private void CheckChangesPeriodically( )
        {
            if ( Update.Delay == 0 )
                CheckChangesInternal( );
            else
            {
                var time = Update.UnscaledTime ? Time.unscaledTime : Time.time;
                if( time - _lastUpdateTime >= Update.Delay )
                {
                    _lastUpdateTime = time;
                    CheckChangesInternal( );
                }
            }
        }

        void IBeforeLateUpdate.DoBeforeLateUpdate( )
        {
            CheckChangesPeriodically();           
        }

        void IAfterLateUpdate.DoAfterLateUpdate( )
        {
            CheckChangesPeriodically();           
        }

        void IUpdate.DoUpdate( )
        {
            CheckChangesPeriodically();           
        }

        private INotifyPropertyChanged _sourceNotify;
        private Func<T> _directGetter;
        private Boolean _sourceChanged;
        protected bool  _isValid;
        protected T     _lastValue;
        protected bool _isLastValueInitialized;
        protected IDataReader<T> _lastReader;
        protected Boolean _isSubscribed;
        private Boolean _isTweened;
        private EUpdateMode _currentUpdateMode = EUpdateMode.Manual;
        private float _lastUpdateTime;

        //Debug data, to make useful logs is something goes wrong
        private MonoBehaviour _debugHost;
        private String _debugHostName;
        private String _debugBindingName;
        private String _debugBindingInfo;
        protected string _debugSourceBindingInfo;
        

    }

}