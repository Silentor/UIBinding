using System;
using System.Collections.Generic;
using System.Reflection;
using UIBindings.Runtime;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using Object = System.Object;

namespace UIBindings
{
    [Serializable]
    public abstract class Binding
    {
        public        UnityEngine.Object        Source;
        public        SourcePath                Path;

        [SerializeField]
        protected ConvertersList _converters;
        public IReadOnlyList<ConverterBase> Converters => _converters.Converters;
        public const String ConvertersPropertyName = nameof(_converters);
        
        [Serializable]
        public class ConvertersList
        {
            [SerializeReference]
            public ConverterBase[] Converters;
        }

        protected static readonly ProfilerMarker ReadDirectValueMarker = new ( ProfilerCategory.Scripts,  $"{nameof(Binding)}.ReadDirectValue", MarkerFlags.Script );
        protected static readonly ProfilerMarker WriteDirectValueMarker = new ( ProfilerCategory.Scripts,  $"{nameof(Binding)}.WriteDirectValue", MarkerFlags.Script );
        protected static readonly ProfilerMarker ReadConvertedValueMarker = new ( ProfilerCategory.Scripts,  $"{nameof(Binding)}.ReadConvertedValue", MarkerFlags.Script );
        protected static readonly ProfilerMarker WriteConvertedValueMarker = new ( ProfilerCategory.Scripts,  $"{nameof(Binding)}.WriteConvertedValue", MarkerFlags.Script );
        protected static readonly ProfilerMarker UpdateTargetMarker = new ( ProfilerCategory.Scripts,  $"{nameof(Binding)}.UpdateTarget", MarkerFlags.Script );
        

        public static (Type valueType, Type templateType) GetBinderTypeInfo( Type bindingType )
        {
            Assert.IsTrue( typeof(Binding).IsAssignableFrom( bindingType ) );

            while (bindingType != null)
            {
                if (bindingType.IsGenericType 
                    && ( bindingType.GetGenericTypeDefinition() == typeof(Binding<>) || bindingType.GetGenericTypeDefinition() == typeof(BindingTwoWay<>) ) )
                {
                    var valueType  = bindingType.GetGenericArguments()[0];
                    var template   = bindingType.GetGenericTypeDefinition();
                    return ( valueType, template );
                }
                bindingType = bindingType.BaseType;
            }
            throw new InvalidOperationException("Base type was not found");
        }

    }

    [Serializable]
    public class Binding<T> : Binding
    {
        public void Awake( MonoBehaviour host )
        {
            if ( !Source )
            {
                Debug.LogError( $"[{nameof(Binding)}] Source is not assigned at {host.name}", host );
                return;
            }

            if ( !Path.IsAssigned )
            {
                Debug.LogError( $"[{nameof(Binding)}] Path is not assigned at {host.name}", host );
                return;
            }

            var sourceType = Source.GetType();
            var property   = sourceType.GetProperty( Path );

            if ( property == null )
            {
                Debug.LogError( $"[{nameof(Binding)}] Property {Path} not found in {sourceType.Name}", host );
                return;
            }

            _hostName = host.name;
            _sourceNotify = Source as INotifyPropertyChanged;

            var converters = _converters.Converters;
            if ( converters.Length > 0 )
            {
                //Prepare converters chain
                for ( int i = 0; i < converters.Length; i++ )
                    converters[ i ] = converters[ i ].ReverseMode ? converters[ i ].GetReverseConverter() : converters[ i ];

                //Connect first converter to source property
                var firstConverter                = converters[0];
                var (inputType, outputType, _) = ConverterBase.GetConverterTypeInfo( firstConverter );
                Assert.IsTrue( inputType == property.PropertyType, $"[{nameof(Binding)}]-[{nameof(Awake)}] First converter {firstConverter.GetType().Name} input type {inputType} is not equal to property {property} type {property.PropertyType.Name}" );
                firstConverter.InitAttachToSourceProperty( Source, property );

                //Make each converter know about prev converter
                var prevOutputType = outputType;
                for ( var i = 1; i < converters.Length; i++ )
                {
                    var myTypes = ConverterBase.GetConverterTypeInfo( converters[i] );
                    Assert.IsTrue( prevOutputType == myTypes.input, $"[{nameof(Binding)}]-[{nameof(Awake)}] Converter {converters[i].GetType().Name} input type {myTypes.input} is not equal to prev converter {converters[i-1].GetType().Name} output type {prevOutputType}" );
                    converters[i].InitAttachToSourceConverter( converters[ i - 1] );
                    prevOutputType = myTypes.output;
                }
                
                //Connect last converter to binder
                _lastConverter = (ITwoWayConverter<T>)converters[^1];
            }
            else                //No converters, just use the property getter
            {
                if ( typeof(T) != property.PropertyType )
                {
                    Debug.LogError($"[{nameof(Binding)}] Binding expect type {typeof(T).Name}, but property {property.DeclaringType.Name}.{property.Name} has type {property.PropertyType.Name}. Consider to add converter.");
                    return;
                } 
                var getMethod = property.GetGetMethod();
                _directGetter = (Func<T>)Delegate.CreateDelegate( typeof(Func<T>), Source, getMethod );
            }

            DoAwake( host, property );

            _isValid = true;
        }

        public void Subscribe()
        {
            if( _isSubscribed ) return;

            if ( _sourceNotify != null )                
                _sourceNotify.PropertyChanged += OnSourcePropertyChanged;

            _isSubscribed = true;
        }

        public void Unsubscribe()
        {
            if( !_isSubscribed ) return;

            if ( _sourceNotify != null )                
                _sourceNotify.PropertyChanged -= OnSourcePropertyChanged;

            _isSubscribed = false;
        }

        /// <summary>
        /// To get change event at desired time (LateUpdate for example)
        /// </summary>
        public void CheckChanges( )
        {
            if ( !_isValid || !_isSubscribed ) return;

            if( _sourceNotify == null || _sourceChanged )
            {
                T value;
                var isChangedOnSource = true;
                if ( _directGetter != null )
                {
                    ReadDirectValueMarker.Begin();
                    value = _directGetter.Invoke();
                    ReadDirectValueMarker.End();
                }
                else
                {
                    ReadConvertedValueMarker.Begin();
                    isChangedOnSource = _lastConverter.TryGetValueFromSource( out value );
                    ReadConvertedValueMarker.End();
                    //Debug.Log( $"Frame {Time.frameCount} checking changes for {GetType().Name} at {_hostName}" );
                }

                if( isChangedOnSource && (!_isLastValueInitialized || !EqualityComparer<T>.Default.Equals( value, _lastSourceValue ) ))
                {
                    _isLastValueInitialized   = true;
                    _lastSourceValue = value;

                    UpdateTargetMarker.Begin();
                    SourceChanged?.Invoke( Source, value );
                    UpdateTargetMarker.End();
                }

                _sourceChanged = false;
            }
        }

        public event Action<Object, T> SourceChanged;

        protected virtual void DoAwake( MonoBehaviour host, PropertyInfo property ) { }

        private void OnSourcePropertyChanged(Object sender, String propertyName )
        {
            if ( String.IsNullOrEmpty( propertyName ) || String.Equals( propertyName, Path, StringComparison.Ordinal ) )
            {
                _sourceChanged = true;
            }
        }

        private INotifyPropertyChanged _sourceNotify;
        private Func<T> _directGetter;
        private Boolean _sourceChanged;
        protected bool _isValid;
        protected T _lastSourceValue;
        protected bool _isLastValueInitialized;
        protected IOneWayConverter<T> _lastConverter;
        protected Boolean _isSubscribed;
        protected String _hostName;
    }

}