using System;
using System.Diagnostics.CodeAnalysis;
using UIBindings.Converters;
using UnityEngine.Assertions;

namespace UIBindings
{
    /// <summary>
    /// Item of data binding pipeline. Always has one input and possible has many outputs. Always implements at least one <see cref="IDataReader{TOutput}"/>
    /// </summary>
    public abstract class DataProvider
    {
        /// <summary>
        /// Would it be process data in both directions. If true, it must implement al least one <see cref="IDataReadWriter{TOutput}"/>
        /// </summary>
        public abstract bool IsTwoWay { get; }

        /// <summary>
        /// Type of input data that this provider can accept
        /// </summary>
        public abstract Type InputType { get; }

        public override String ToString( )
        {
            return $"{GetType().Name} (Input: {InputType.Name}, IsTwoWay: {IsTwoWay})";
        }
    } 

    /// <summary>
    /// User accessible converters with inspector in Unity Editor.
    /// </summary>
    [Serializable]
    public abstract class ConverterBase : DataProvider
    {
        //Two way converters can  be used output->input if needed
        public bool ReverseMode;          //todo consider this
        
        public abstract Type OutputType { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prevConverter"></param>
        /// <param name="isTwoWay"></param>
        /// <param name="unscaledTime"></param>
        /// <returns>False if attach failed</returns>
        public abstract Boolean InitAttachToSource(  DataProvider prevConverter, Boolean isTwoWay, Boolean unscaledTime );

        public abstract ConverterBase GetReverseConverter( );

        public static (Type input, Type output, Type template) GetConverterTypeInfo( ConverterBase converter )
        {
            var rawInfo = GetConverterTypeInfo( converter.GetType() );
            if ( converter.ReverseMode )
                return new ValueTuple<Type, Type, Type>( rawInfo.output, rawInfo.input, rawInfo.template );
            return rawInfo;
        }

        public static (Type input, Type output, Type template) GetConverterTypeInfo( Type converterType )
        {
            Assert.IsTrue( typeof(ConverterBase).IsAssignableFrom( converterType ) );

            while (converterType.BaseType != null)
            {
                converterType = converterType.BaseType;
                if (converterType.IsGenericType 
                    && (converterType.GetGenericTypeDefinition() == typeof(SimpleConverterOneWayBase<,>) || converterType.GetGenericTypeDefinition() == typeof(SimpleConverterTwoWayBase<,>) || converterType.GetGenericTypeDefinition() == typeof(ConverterBase<,>)))
                {
                    var inputType  = converterType.GetGenericArguments()[0];
                    var outputType = converterType.GetGenericArguments()[1];
                    var template   = converterType.GetGenericTypeDefinition();
                    return ( inputType, outputType, template );
                }
            }
            throw new InvalidOperationException("Base type was not found");
        }

        public override String ToString( )
        {
            return $"{GetType().Name} (Input: {InputType.Name}, Output: {OutputType.Name}, IsTwoWay: {IsTwoWay})";
        }
    }

    public abstract class ConverterBase<TInput, TOutput> : ConverterBase
    {
        public override Type InputType  => typeof(TInput);

        public override Type OutputType => typeof(TOutput);

        //Returns false if attach to source failed, probably incompatible types
        public override Boolean InitAttachToSource(   [NotNull] DataProvider prevConverter, Boolean isTwoWay, Boolean unscaledTime )
        {
            if ( prevConverter == null ) throw new ArgumentNullException( nameof(prevConverter) );
            if ( prevConverter is IDataReader<TInput> properSource )
            {
                _prev =  properSource;
                OnAttachToSource( prevConverter, isTwoWay );
            }
            else
            {
                var implicitTypeConverter = ImplicitConversion.GetConverter( prevConverter, typeof(TInput) );
                if ( implicitTypeConverter != null )
                {
                    _prev = (IDataReader<TInput>)implicitTypeConverter;
                    OnAttachToSource( implicitTypeConverter, isTwoWay );
                }
            }

            _unscaledTime = unscaledTime;
            return _prev != null;
        }

        protected virtual void OnAttachToSource( DataProvider prevConverter, Boolean isTwoWay )
        {
            //Default implementation does nothing
            //Override this method if you need to do something on attach
        }

        protected float GetDeltaTime( )
        {
            return _unscaledTime ? UnityEngine.Time.unscaledDeltaTime : UnityEngine.Time.deltaTime;
        }

        protected IDataReader<TInput> _prev;
        protected Boolean _unscaledTime;
    }
}