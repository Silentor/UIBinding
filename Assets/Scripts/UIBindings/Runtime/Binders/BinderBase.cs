using System;
using System.Collections.Generic;
using System.Linq;
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
    public abstract class BinderBase : MonoBehaviour
    {
        public UnityEngine.Object   Source;
        public SourcePath           Path;

        [FormerlySerializedAs( "Converters" )] [SerializeField, HideInInspector]
        protected ConvertersList _converters;

        public IReadOnlyList<ConverterBase> Converters => _converters.Converters;

        public static readonly string ConvertersFieldName = nameof(_converters);

        [Serializable]
        public class ConvertersList
        {
            [SerializeReference]
            public ConverterBase[] Converters;
        }

        public static (Type value, Type template) GetBinderTypeInfo( Type binderType )
        {
            Assert.IsTrue( typeof(BinderBase).IsAssignableFrom( binderType ) );

            while (binderType.BaseType != null)
            {
                binderType = binderType.BaseType;
                if (binderType.IsGenericType 
                    && (binderType.GetGenericTypeDefinition() == typeof(BinderBase<>)|| binderType.GetGenericTypeDefinition() == typeof(BinderTwoWayBase<>)) )
                {
                    var valueType  = binderType.GetGenericArguments()[0];
                    var template   = binderType.GetGenericTypeDefinition();
                    return ( valueType, template );
                }
            }
            throw new InvalidOperationException("Base type was not found");
        }

    }

    public abstract class BinderBase<T> : BinderBase, IInput<T>
    {
        private ConverterBase _firstConverter;
        private Func<T> _directGetter;
        private INotifyPropertyChanged _sourceNotify;
        private Boolean _sourceChanged;

        protected PropertyInfo _sourceProperty;
        

        private static readonly ProfilerMarker UpdateBinderMarker = new ( ProfilerCategory.Scripts,  $"{nameof(BinderBase)}.UpdateBinder", MarkerFlags.Script );

        public abstract void ProcessSourceToTarget(T value );

        protected virtual void Awake( )
        {
            Assert.IsTrue( !Source || Path.IsAssigned, $"[{nameof(BinderBase)}] Path is not assigned on binder {name}." );

            InitGetter();
        }

        protected virtual void OnEnable( )
        {
            if( _sourceNotify != null )
                _sourceNotify.PropertyChanged += OnSourceNotifyPropertyChanged;
        }

        protected virtual void OnDisable( )
        {
            if( _sourceNotify != null )
                _sourceNotify.PropertyChanged -= OnSourceNotifyPropertyChanged;
        }

        private void OnSourceNotifyPropertyChanged( Object sender, String propertyName )
        {
            if ( String.IsNullOrEmpty( propertyName ) || String.Equals( propertyName, Path, StringComparison.Ordinal ) )
                _sourceChanged = true;
        }

        protected void InitGetter( )
        {
            var sourceType = Source.GetType();
            var property   = sourceType.GetProperty( Path );

            if( property == null ) 
            {
                Debug.LogError( $"Property {Path} not found in {sourceType.Name}" );
                return;
            }

            _sourceNotify = Source as INotifyPropertyChanged;

            //Prepare converters chain
            var converters = _converters.Converters;
            for ( int i = 0; i < converters.Length; i++ )                
                converters[i] = converters[i].ReverseMode ? converters[i].GetReverseConverter() : converters[i];

            if( converters.Length > 0 )
            {
                //Connect first converter to source property
                _firstConverter = converters[0];
                var (inputType, outputType, _) = ConverterBase.GetConverterTypeInfo( _firstConverter );
                Assert.IsTrue( inputType == property.PropertyType, $"[{nameof(BinderBase)}]-[{nameof(InitGetter)}] First converter input type expected {property.PropertyType.Name} to be equal to source property type {property} but actual {inputType.Name}" );
                _firstConverter.InitAttachToSourceProperty( Source, property );

                //Make each converter know about next converter
                for ( var i = 0; i < converters.Length - 1; i++ )
                {
                    var nextConverterTypes = ConverterBase.GetConverterTypeInfo( converters[i + 1] );
                    Assert.IsTrue( outputType == nextConverterTypes.input, $"[{nameof(BinderBase)}]-[{nameof(InitGetter)}] Converter {converters[i].GetType().Name} output type expected {nextConverterTypes.input.Name} to be equal to input type of converter {converters[i+1].GetType().Name} but actual {outputType.Name}" );
                    converters[i].InitSourceToTarget( converters[ i + 1] );
                    (inputType, outputType, _) = nextConverterTypes;
                }
                
                //Connect last converter to binder
                Assert.IsTrue( outputType == typeof(T), $"[{nameof(BinderBase)}]-[{nameof(InitGetter)}] Last converter output type expected {typeof(T).Name} to be equal to input type of binder {GetType().Name} but actual {outputType.Name}" );
                converters[^1].InitSourceToTarget( this );
            }
            else                //No converters, just use the property getter
            {
                Assert.IsTrue( property.PropertyType == typeof(T), $"[{nameof(BinderBase)}]-[{nameof(InitGetter)}] Binder {GetType().Name} expect type {typeof(T).Name}, but property {property.DeclaringType.Name}.{property.Name} has type {property.PropertyType.Name}. Consider to add converter." );
                var getMethod = property.GetGetMethod();
                _directGetter = (Func<T>)Delegate.CreateDelegate( typeof(Func<T>), Source, getMethod );
            }

            _sourceProperty = property;
        }

        protected virtual void LateUpdate()
        {
            if ( _sourceNotify == null || _sourceChanged )                
            {
                _sourceChanged = false;

                UpdateBinderMarker.Begin( );
                if ( _directGetter != null )
                {
                    var value = _directGetter();
                    ProcessSourceToTarget( value );
                }
                else
                {
                    _firstConverter.OnSourcePropertyChanged();
                }

                Debug.Log( $"[{nameof(BinderBase)}]-[{nameof(LateUpdate)}] updated binder {name} on frame {Time.frameCount}", this );

                UpdateBinderMarker.End();
            }
        }
    }
}