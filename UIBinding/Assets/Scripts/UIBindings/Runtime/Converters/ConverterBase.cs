using System;
using System.Diagnostics.CodeAnalysis;
using UIBindings.Converters;
using UnityEngine.Assertions;

namespace UIBindings
{
    /// <summary>
    /// User accessible converters with inspector in Unity Editor.
    /// </summary>
    [Serializable]
    public abstract class ConverterBase : DataProvider
    {
        public abstract Type OutputType { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prevConverter"></param>
        /// <param name="isTwoWay"></param>
        /// <param name="unscaledTime"></param>
        /// <returns>False if attach failed</returns>
        public abstract Boolean InitAttachToSource(  DataProvider prevConverter, Boolean isTwoWay, Boolean unscaledTime );

        public static TypeInfo GetConverterTypeInfo( ConverterBase converter )
        {
            var rawInfo = GetConverterTypeInfo( converter.GetType() );
            return rawInfo;
        }

        public static TypeInfo GetConverterTypeInfo( Type converterType )
        {
            Assert.IsTrue( typeof(ConverterBase).IsAssignableFrom( converterType ) );

            var originalType = converterType;
            while (converterType.BaseType != null)
            {
                converterType = converterType.BaseType;
                if (converterType.IsGenericType && converterType.GetGenericTypeDefinition() == typeof(SimpleConverterTwoWayBase<,>))
                {
                    var inputType  = converterType.GetGenericArguments()[0];
                    var outputType = converterType.GetGenericArguments()[1];
                    var template   = converterType.GetGenericTypeDefinition();
                    return new TypeInfo( inputType, outputType, template, originalType, EMode.TwoWay );
                }
                if (converterType.IsGenericType && converterType.GetGenericTypeDefinition() == typeof(ConverterBase<,>))
                {
                    var inputType  = converterType.GetGenericArguments()[0];
                    var outputType = converterType.GetGenericArguments()[1];
                    var template   = converterType.GetGenericTypeDefinition();
                    return new TypeInfo( inputType, outputType, template, originalType, EMode.OneWay );
                }
            }
            throw new InvalidOperationException("Base type was not found");
        }

        public override String ToString( )
        {
            return $"{GetType().Name} (Input: {InputType.Name}, Output: {OutputType.Name}, IsTwoWay: {IsTwoWay})";
        }

        public enum EMode
        {
            OneWay,
            TwoWay,
        }

        /// <summary>
        /// Editor only, so allocations is not a big deal
        /// </summary>
        public readonly struct TypeInfo 
        {
            public Type  Input    { get; }
            public bool  IsInputGenericParam { get; } //If Input is generic parameter
            public bool  IsInputEnumParam { get; }  //If Input is generic parameter and has Enum constraint
            public Type  Output   { get; }
            public Type  Template { get; }
            public Type  FullType { get; }
            public string FullTypeName { get; }
            public EMode Mode      { get; }

            public TypeInfo( Type input, Type output, Type template, Type fullType, EMode mode )
            {
                Input    = input;
                IsInputGenericParam = false;
                IsInputEnumParam = false;
                Output   = output;
                Template = template;
                FullType = fullType;
                Mode         = mode;

                if ( Input.IsGenericTypeParameter )
                {
                    IsInputGenericParam = true;
                    var constraints = Input.GetGenericParameterConstraints();             
                    IsInputEnumParam = Array.Exists( constraints, c => c == typeof(Enum) );
                }

                if ( FullType.IsGenericType )
                {
                    var backtickIndex = FullType.Name.IndexOf('`');
                    if ( backtickIndex > 0 )
                        FullTypeName = FullType.Name[ .. backtickIndex ];
                    else
                        FullTypeName = FullType.Name;
                }
                else
                    FullTypeName = FullType.Name;
            }
        }
    }

    public abstract class ConverterBase<TInput, TOutput> : ConverterBase
    {
        public override Type InputType  => typeof(TInput);

        public override Type OutputType => typeof(TOutput);

        //Returns false if attach to source failed, probably incompatible types
        public override Boolean InitAttachToSource( [NotNull] DataProvider prevConverter, Boolean isTwoWay, Boolean unscaledTime )
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