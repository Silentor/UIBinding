﻿using System;
using System.Collections.Generic;
using System.Reflection;
using UIBindings.Runtime.PlayerLoop;
using UIBindings.Runtime.Utils;
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
        //How often and when to check for changes in source property
        public          UpdateMode Update = new (){Mode = EUpdateMode.AfterLateUpdate, ScaledTime = true};

        [SerializeField]
        protected ConvertersList _converters = new (){Converters = Array.Empty<ConverterBase>()};
        public       IReadOnlyList<ConverterBase> Converters => _converters.Converters;
        public const String                       ConvertersPropertyName = nameof(_converters) + "." + nameof(ConvertersList.Converters);

        public abstract bool IsCompatibleWith( Type type );

        public abstract bool       IsTwoWay { get; }

        /// <summary>
        /// React to source property changes, either by subscribing to INotifyPropertyChanged or by checking changes periodically.
        /// </summary>
        /// <param name="host">Optional host component to autosort updates. Bindings on components higher at hierarchy will be updated first.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Subscribe( int updateOrder = 0 )
        {
            if( !Enabled || !_isValid || _isSubscribed ) return;

            if ( _sourceNotify != null )                
                _sourceNotify.PropertyChanged += OnSourcePropertyChanged;
            switch ( Update.Mode )
            {
                case EUpdateMode.AfterLateUpdate:  UpdateManager.RegisterAfterLateUpdate( DoUpdate, updateOrder ); break;
                case EUpdateMode.BeforeLateUpdate: UpdateManager.RegisterBeforeLateUpdate( DoUpdate, updateOrder ); break;
                case EUpdateMode.AfterUpdate:      UpdateManager.RegisterUpdate( DoUpdate, updateOrder ); break;
                case EUpdateMode.Manual:           break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _currentUpdateMode = Update.Mode;
            _isValueInitialized = false;        //Source can change while we are not subscribed, so we need to re-read it

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
        protected bool                   _isValueInitialized;
        private   EUpdateMode            _currentUpdateMode = EUpdateMode.Manual;
        protected float                  _lastUpdateTime;

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

        private void OnSourcePropertyChanged(Object sender, String propertyName )
        {
            if ( String.IsNullOrEmpty( propertyName ) || String.Equals( propertyName, Path, StringComparison.Ordinal ) )
            {
                Debug.Log( $"[{nameof(DataBinding)}] Source property '{propertyName}' changes detected in binding {_debugTargetBindingInfo}", _debugHost );
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

#region Debug stuff

        protected static readonly ProfilerMarker ReadDirectValueMarker     = new ( ProfilerCategory.Scripts,  $"{nameof(BindingBase)}.ReadDirectValue" );
        protected static readonly ProfilerMarker WriteDirectValueMarker    = new ( ProfilerCategory.Scripts,  $"{nameof(BindingBase)}.WriteDirectValue" );
        protected static readonly ProfilerMarker ReadConvertedValueMarker  = new ( ProfilerCategory.Scripts,  $"{nameof(BindingBase)}.ReadConvertedValue" );
        protected static readonly ProfilerMarker WriteConvertedValueMarker = new ( ProfilerCategory.Scripts,  $"{nameof(BindingBase)}.WriteConvertedValue" );
        protected static readonly ProfilerMarker UpdateTargetMarker        = new ( ProfilerCategory.Scripts,  $"{nameof(BindingBase)}.UpdateTarget" );

        protected string ProfilerMarkerName = String.Empty;

        public override String GetBindingDirection( )
        {
            var convertersCount = _converters.Converters.Length  > 0 ? $"[{_converters.Converters.Length}]" : string.Empty;
            return IsTwoWay ? $"<-{convertersCount}->" : $"-{convertersCount}->";
        }

        public override string GetBindingSourceInfo( )
        {
            if ( SourceObject.IsNotAssigned() )
            {
                return "?";
            }
            else
            {
                var sourceType       = SourceObject.GetType();
                var sourceProp       = sourceType.GetProperty( Path, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
                var sourceObjectName = SourceObject is UnityEngine.Object unityObject ? $"'{unityObject.name}'({unityObject.GetType().Name})" : SourceObject.GetType().ToString();
                if( sourceProp != null )
                {
                    return $"{sourceProp.PropertyType.GetPrettyName()} {sourceObjectName}.{sourceProp.Name}";
                }
                else
                {
                    return $"{sourceObjectName}.{Path}?";
                }
            }
        }

        public abstract bool   IsRuntimeValid { get; }

        public override String GetFullRuntimeInfo( )
        {
            var sourceInfo = GetBindingSourceInfo();
            var targetInfo = GetBindingTargetInfo();
            var direction  = GetBindingDirection();
            var targetState = GetBindingState();
            var sourceState = GetSourceState();

            return $"{sourceInfo} <{sourceState}> {direction} {targetInfo} <{targetState}>";
        }

#endregion
        
    }
}