using System;
using System.Collections.Generic;
using UIBindings.Runtime.PlayerLoop;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using Object = System.Object;

namespace UIBindings
{
    /// <summary>
    /// Binding that can transfer data from source property to some target (and back if two-way). Data can be converted in both ways.
    /// </summary>
    [Serializable]
    public abstract class DataBinding : BindingBase
    {
        public          UpdateMode Update = new (){Mode = EUpdateMode.AfterLateUpdate, ScaledTime = true};

        [SerializeField]
        protected ConvertersList _converters = new (){Converters = Array.Empty<ConverterBase>()};
        public       IReadOnlyList<ConverterBase> Converters => _converters.Converters;
        public const String                       ConvertersPropertyName = nameof(_converters);

        public abstract Type       DataType { get; }

        public abstract bool       IsTwoWay { get; }

        /// <summary>
        /// May be called before Awake for useful logs in case of errors.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="bindingName"></param>
        public void SetDebugInfo( MonoBehaviour host, String bindingName )
        {
            _debugHost        = host;
            _debugHostName    = host.name;
            _debugBindingName = bindingName;
            _debugBindingInfo = $"{DataType.Name} {host.name}.{bindingName}";
            var sourceName = Source ? Source.name : "null";
            var direction  = IsTwoWay ? "<->" : "->";
            _debugSourceBindingInfo = $"{sourceName}.{Path} {direction} {_debugBindingInfo}";
        }

        /// <summary>
        /// React to source property changes, either by subscribing to INotifyPropertyChanged or by checking changes periodically.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Subscribe()
        {
            if( !Enabled || !_isValid || _isSubscribed ) return;

            if ( _sourceNotify != null )                
                _sourceNotify.PropertyChanged += OnSourcePropertyChanged;
            switch ( Update.Mode )
            {
                case EUpdateMode.AfterLateUpdate:  UpdateManager.RegisterAfterLateUpdate( DoUpdate ); break;
                case EUpdateMode.BeforeLateUpdate: UpdateManager.RegisterBeforeLateUpdate( DoUpdate ); break;
                case EUpdateMode.AfterUpdate:      UpdateManager.RegisterUpdate( DoUpdate ); break;
                case EUpdateMode.Manual:           break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _currentUpdateMode = Update.Mode;

            _isSubscribed = true;
        }

        /// <summary>
        /// Do not react to source changes
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Unsubscribe()
        {
            if( !Enabled || !_isValid || !_isSubscribed ) return;

            if ( _sourceNotify != null )                
                _sourceNotify.PropertyChanged -= OnSourcePropertyChanged;
            switch ( _currentUpdateMode )
            {
                case EUpdateMode.AfterLateUpdate:  UpdateManager.UnregisterAfterLateUpdate( DoUpdate ); break;
                case EUpdateMode.BeforeLateUpdate: UpdateManager.UnregisterBeforeLateUpdate( DoUpdate ); break;
                case EUpdateMode.AfterUpdate:      UpdateManager.UnregisterUpdate( DoUpdate ); break;
                case EUpdateMode.Manual:           break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _isSubscribed = false;
        }

        /// <summary>
        /// Check changes immediately
        /// </summary>
        public void ManuallyCheckChanges( )
        {
            if ( !Enabled || !_isValid || !_isSubscribed ) return;

            CheckChangesInternal( );
        }


        protected INotifyPropertyChanged _sourceNotify;
        protected Boolean                _sourceChanged;
        protected bool                   _isValid;
        protected Boolean                _isSubscribed;
        private   EUpdateMode            _currentUpdateMode = EUpdateMode.Manual;
        protected float                  _lastUpdateTime;
        protected string                 _debugSourceBindingInfo;
        protected MonoBehaviour          _debugHost;
        private   String                 _debugHostName;
        private   String                 _debugBindingName;
        protected String                 _debugBindingInfo;


        //Mostly debug
        public abstract Object GetDebugLastValue( );
        public abstract bool   IsRuntimeValid { get; }

        public static (Type valueType, Type templateType) GetBindingTypeInfo( Type bindingType )
        {
            Assert.IsTrue( typeof(BindingBase).IsAssignableFrom( bindingType ) );

            while (bindingType != null)
            {
                if (bindingType.IsGenericType 
                 && ( bindingType.GetGenericTypeDefinition() == typeof(ValueBinding<>) || bindingType.GetGenericTypeDefinition() == typeof(ValueBindingRW<>) ) )
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
            public EUpdateMode Mode;
            public float       Delay;
            public bool        ScaledTime;
        }

        public enum EUpdateMode
        {
            AfterUpdate,
            BeforeLateUpdate,
            AfterLateUpdate,
            Manual
        }

        protected static readonly ProfilerMarker ReadDirectValueMarker     = new ( ProfilerCategory.Scripts,  $"{nameof(BindingBase)}.ReadDirectValue" );
        protected static readonly ProfilerMarker WriteDirectValueMarker    = new ( ProfilerCategory.Scripts,  $"{nameof(BindingBase)}.WriteDirectValue" );
        protected static readonly ProfilerMarker ReadConvertedValueMarker  = new ( ProfilerCategory.Scripts,  $"{nameof(BindingBase)}.ReadConvertedValue" );
        protected static readonly ProfilerMarker WriteConvertedValueMarker = new ( ProfilerCategory.Scripts,  $"{nameof(BindingBase)}.WriteConvertedValue" );
        protected static readonly ProfilerMarker UpdateTargetMarker        = new ( ProfilerCategory.Scripts,  $"{nameof(BindingBase)}.UpdateTarget" );



        private void OnSourcePropertyChanged(Object sender, String propertyName )
        {
            if ( String.IsNullOrEmpty( propertyName ) || String.Equals( propertyName, Path, StringComparison.Ordinal ) )
            {
                Debug.Log( $"[{nameof(DataBinding)}] Source property '{propertyName}' changes detected in binding {_debugBindingInfo}", _debugHost );
                _sourceChanged = true;
            }
        }

        
        private void CheckChangesPeriodically( )
        {
            if ( Update.Delay == 0 )
            {
                CheckChangesInternal( );
            }
            else
            {
                var time = Update.ScaledTime ? Time.time : Time.unscaledTime;
                if( time - _lastUpdateTime >= Update.Delay )
                {
                    _lastUpdateTime = time;
                    CheckChangesInternal( );
                }
            }
        }

        private void DoUpdate( )
        {
            CheckChangesPeriodically();           
        }

        protected abstract void CheckChangesInternal( );


        
    }
}