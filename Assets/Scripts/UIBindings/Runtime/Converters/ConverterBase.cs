using System;
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

        public abstract DataProvider InitAttachToSource(   DataProvider prevConverter, Boolean isTwoWay );

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
                    && (converterType.GetGenericTypeDefinition() == typeof(ConverterOneWayBase<,>) || converterType.GetGenericTypeDefinition() == typeof(ConverterTwoWayBase<,>)))
                {
                    var inputType  = converterType.GetGenericArguments()[0];
                    var outputType = converterType.GetGenericArguments()[1];
                    var template   = converterType.GetGenericTypeDefinition();
                    return ( inputType, outputType, template );
                }
            }
            throw new InvalidOperationException("Base type was not found");
        }

    }
}