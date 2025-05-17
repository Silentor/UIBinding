using System;
using System.Reflection;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using UnityEngine;
using UnityEngine.Assertions;
using Object = System.Object;

namespace UIBindings
{
    public abstract class BinderBase : MonoBehaviour
    {
        public UnityEngine.Object Source;
        public String             Path;

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
            Assert.IsTrue( Source );
            Assert.IsTrue( !String.IsNullOrEmpty( Path ) );

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
            var converters = GetComponents<ConverterBase>();
            if( converters.Length > 0 )
            {
                _firstConverter = converters[0];
                var (inputType, outputType, _) = GetConverterTypes( _firstConverter );
                Assert.IsTrue( inputType == property.PropertyType, $"[{nameof(BinderBase)}]-[{nameof(InitGetter)}] First converter input type expected {property.PropertyType.Name} to be equal to source property type {property} but actual {inputType.Name}" );
                _firstConverter.InitAttachToSource( Source, property );

                for ( var i = 0; i < converters.Length - 1; i++ )
                {
                    var nextConverterTypes = GetConverterTypes( converters[i + 1] );
                    Assert.IsTrue( outputType == nextConverterTypes.input, $"[{nameof(BinderBase)}]-[{nameof(InitGetter)}] Converter {converters[i].GetType().Name} output type expected {nextConverterTypes.input.Name} to be equal to input type of converter {converters[i+1].GetType().Name} but actual {outputType.Name}" );
                    converters[i].InitSourceToTarget( converters[ i + 1] );
                    (inputType, outputType, _) = nextConverterTypes;
                }
                
                //Check last converter output type
                Assert.IsTrue( outputType == typeof(T), $"[{nameof(BinderBase)}]-[{nameof(InitGetter)}] Last converter output type expected {typeof(T).Name} to be equal to input type of binder {GetType().Name} but actual {outputType.Name}" );
                converters[^1].InitSourceToTarget( this );
            }
            else                //No converters, just use the property getter
            {
                Assert.IsTrue( property.PropertyType == typeof(T), $"[{nameof(BinderBase)}]-[{nameof(InitGetter)}] Binder type expected {property.PropertyType.Name} to be equal to source property type {property} but actual {typeof(T).Name}" );
                var getMethod = property.GetGetMethod();
                _directGetter = (Func<T>)Delegate.CreateDelegate( typeof(Func<T>), Source, getMethod );
            }

            _sourceProperty = property;
        }

        protected (Type input, Type output, Type template) GetConverterTypes( ConverterBase converter )
        {
            var converterType = converter.GetType();

            while (converterType.BaseType != null)
            {
                converterType = converterType.BaseType;
                if (converterType.IsGenericType 
                    && (converterType.GetGenericTypeDefinition() == typeof(ConverterOneWayBase<,>) || converterType.GetGenericTypeDefinition() == typeof(ConverterTwoWayBase<,>)))
                {
                    var inputType  = converterType.GetGenericArguments()[0];
                    var outputType = converterType.GetGenericArguments()[1];
                    return ( inputType, outputType, converterType );
                }
            }
            throw new InvalidOperationException("Base type was not found");
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
                    _firstConverter.OnChange();
                }

                Debug.Log( $"[{nameof(BinderBase)}]-[{nameof(LateUpdate)}] updated binder {name} on frame {Time.frameCount}", this );

                UpdateBinderMarker.End();
            }
        }
    }
}