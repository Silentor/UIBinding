using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace UIBindings
{
    public abstract class BinderTwoWayBase<T> : BinderBase<T>, IOutput<T>
    {
        private IOutput<T> _firstSetterConverter;
        private Action<T>     _directSetter;

        protected override void Awake( )
        {
            base.Awake();

            InitSetter();
        }

        protected void InitSetter( )
        {
            var sourceType = Source.GetType();
            var property   = _sourceProperty;
            if( property == null ) 
            {
                Debug.LogError( $"Property {Path} not found in {sourceType.Name}" );
                return;
            }
    
            //Init converters chain from last to first
            var converters = Converters;
            if ( converters.Count > 0 )
            {
                _firstSetterConverter       = (IOutput<T>)converters[^1];

                for ( int i = converters.Count - 1; i >= 1; i++ )
                {
                    var currentConverter = converters[ i ];
                    var prevConverter = converters[ i - 1 ];
                    currentConverter.InitTargetToSource( prevConverter );
                }
            }
            else
            {
                Assert.IsTrue( typeof(T) == property.PropertyType, $"[{nameof(BinderBase)}]-[{nameof(InitSetter)}] Binder type expected {property.PropertyType.Name} to be equal to target property type {property} but actual {typeof(T).Name}" );
                _directSetter = (Action<T>)Delegate.CreateDelegate( typeof(Action<T>), Source, property.GetSetMethod() );
            }

        }

        public void ProcessTargetToSource( T value )
        {
            if( _directSetter != null)
            {
                _directSetter( value );
            }
            else
            {
                _firstSetterConverter.ProcessTargetToSource( value );
            }
        }
    }
}