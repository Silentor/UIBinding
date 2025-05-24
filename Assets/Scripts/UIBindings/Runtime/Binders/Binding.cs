using System;
using System.Collections.Generic;
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
        public ConvertersList Converters;
        //public IReadOnlyList<ConverterBase> Converters => _converters.Converters;
        public const String ConvertersPropertyName = nameof(Converters);
        
        [Serializable]
        public class ConvertersList
        {
            [SerializeReference]
            public ConverterBase[] Converters;
        }

        protected static readonly ProfilerMarker ReadDirectBindingMarker = new ( ProfilerCategory.Scripts,  $"{nameof(Binding)}.ReadDirectBinding", MarkerFlags.Script );

        public static (Type valueType, Type templateType) GetBinderTypeInfo( Type bindingType )
        {
            Assert.IsTrue( typeof(Binding).IsAssignableFrom( bindingType ) );

            while (bindingType != null)
            {
                if (bindingType.IsGenericType && bindingType.GetGenericTypeDefinition() == typeof(Binding<>) )
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

            _sourceNotify = Source as INotifyPropertyChanged;

            var converters = Converters.Converters;
            if ( converters.Length > 0 )
            {
                //Prepare converters chain
                for ( int i = 0; i < converters.Length; i++ )
                    converters[ i ] = converters[ i ].ReverseMode ? converters[ i ].GetReverseConverter() : converters[ i ];

                throw new NotImplementedException();
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

            _isValid = true;
        }

        public void Subscribe()
        {
            if ( _sourceNotify != null )
            {
                _sourceNotify.PropertyChanged += OnSourcePropertyChanged;
            }
        }

        public void Unsubscribe()
        {
            if ( _sourceNotify != null )
            {
                _sourceNotify.PropertyChanged -= OnSourcePropertyChanged;
            }
        }

        /// <summary>
        /// To get change event at desired time (LateUpdate for example)
        /// </summary>
        public void CheckChanges( )
        {
            if ( !_isValid ) return;

            if( _sourceNotify == null || _sourceChanged )
            {
                _sourceChanged = false;

                if ( _directGetter != null )
                {
                    ReadDirectBindingMarker.Begin();
                    var value = _directGetter.Invoke();
                    ReadDirectBindingMarker.End();
                    if( !_isInitialized || !EqualityComparer<T>.Default.Equals( value, _lastSourceValue ) )
                    {
                        _isInitialized = true;
                        _lastSourceValue = value;
                        SourceChanged?.Invoke( Source, value );
                    }
                }
                // else
                // {
                //     Assert.IsTrue( _firstConverter != null, $"[{nameof(Binding<T>)}] First converter is not initialized" );
                //     _firstConverter.OnSourcePropertyChanged();
                // }
            }
        }

        public event Action<Object, T> SourceChanged; 

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
        private bool _isValid;
        private T _lastSourceValue;
        private bool _isInitialized;
    }

}