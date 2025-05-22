using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using Object = System.Object;

namespace UIBindings
{
    [Serializable]
    public abstract class ConverterBase
    {
        //Two way converters can  be used output->input for source->target
        public bool ReverseMode;          //todo consider this

        //For first converter in chain (attached to source property)
        public abstract void InitAttachToSourceProperty( System.Object source, PropertyInfo sourceProp );
        
        //For all other converters in chain
        public abstract void InitSourceToTarget( Object nextConverter );

        //For all other converters in chain to reverse the conversion (If it supports reverse conversion)
        public abstract void InitTargetToSource( Object prevConverter );

        /// <summary>
        /// If converter connected to source property, it should read and process property value
        /// </summary>
        public abstract void OnSourcePropertyChanged( );

        public abstract ConverterBase GetReverseConverter( );

        public static (Type input, Type output, Type template) GetConverterTypeInfo( ConverterBase converter )
        {
            return GetConverterTypeInfo( converter.GetType() );
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