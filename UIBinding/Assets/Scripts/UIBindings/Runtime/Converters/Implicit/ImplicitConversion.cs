using System;
using System.Collections.Generic;
using System.Linq;

namespace UIBindings.Converters
{
    public static class ImplicitConversion
    {
        /// <summary>
        /// Get converter to convert source output to given type. It's for implicit conversions, without user specifying converter explicitly.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="convertTo"></param>
        /// <returns></returns>
        public static DataProvider GetConverter( DataProvider source, Type convertTo )    
        {
            var sourceType = source.GetType();
            if(sourceType.IsGenericType )
            {
                // Special case for enum, we cannot prepare converters for all enum types.
                var enumType = GetOutputTypes( sourceType ).FirstOrDefault( t => t.IsEnum ); 
                if ( enumType != null && Array.IndexOf( EnumConverterInfo.OutputTypes, convertTo ) >= 0 )
                {
                    var enumConverterTypeDef = typeof(ImplicitEnumConverter<>);
                    var enumConverterType    = enumConverterTypeDef.MakeGenericType( enumType );
                    var instance             = (DataProvider)Activator.CreateInstance( enumConverterType, source );
                    return instance;
                }
            }
            foreach ( var converterInfo in ConverterInfoCache )
            {
                if( converterInfo.DataReaderType.IsAssignableFrom( sourceType ) && Array.IndexOf( converterInfo.OutputTypes, convertTo ) >= 0 )
                {
                    return converterInfo.ConverterFactory( source );
                }
            }

            return null;
        }
        
        public static bool IsConversionSupported( Type inputType, Type convertTo )
        {
            if ( inputType.IsEnum && Array.IndexOf( EnumConverterInfo.OutputTypes, convertTo ) >= 0 )
                return true;

            foreach ( var converterInfo in ConverterInfoCache )
            {
                if( converterInfo.InputType == inputType && Array.IndexOf( converterInfo.OutputTypes, convertTo ) >= 0 )
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsConversionSupported( Type inputType, Predicate<Type> isSupportedType )
        {
            if ( inputType.IsEnum && Array.Exists( EnumConverterInfo.OutputTypes, isSupportedType ) )
                return true;

            foreach ( var converterInfo in ConverterInfoCache )
            {
                if( converterInfo.InputType == inputType && Array.Exists( converterInfo.OutputTypes, isSupportedType ) )
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<Type> GetOutputTypes( Type converterType )
        {
            var interfaces = converterType.GetInterfaces();
            return interfaces.Where( i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDataReader<>))
                      .Select( i => i.GetGenericArguments()[0] ); 
        }

        private static readonly ImplicitConverterInfo[] ConverterInfoCache = InitTypeCache();
        private static readonly EnumImplicitConverterInfo EnumConverterInfo = new ( typeof(ImplicitEnumConverter<>) );

        private static ImplicitConverterInfo[] InitTypeCache( )
        {
            return new[]
                   {
                           new ImplicitConverterInfo( typeof(int), typeof(ImplicitIntConverter), static inp => new ImplicitIntConverter( inp ) ),
                           new ImplicitConverterInfo( typeof(float), typeof(ImplicitFloatConverter),  static inp => new ImplicitFloatConverter( inp ) ),
                           new ImplicitConverterInfo( typeof(bool), typeof(ImplicitBoolConverter), static inp => new ImplicitBoolConverter( inp ) ),
                           new ImplicitConverterInfo( typeof(byte), typeof(ImplicitByteConverter),  static inp => new ImplicitByteConverter( inp ) ),
                           new ImplicitConverterInfo( typeof(double), typeof(ImplicitDoubleConverter),  static inp => new ImplicitDoubleConverter( inp ) ),
                           new ImplicitConverterInfo( typeof(long), typeof(ImplicitLongConverter), static inp => new ImplicitLongConverter( inp ) ),
                   };
        }

        private readonly struct ImplicitConverterInfo
        {
            public readonly Type InputType;
            public readonly Type DataReaderType;
            public readonly Type[] OutputTypes;
            public readonly Func<DataProvider, DataProvider> ConverterFactory;

            public ImplicitConverterInfo(Type inputType, Type converterType, Func<DataProvider, DataProvider> converterFactory )
            {
                InputType = inputType;
                DataReaderType = typeof(IDataReader<>).MakeGenericType( inputType );
                OutputTypes = GetOutputTypes( converterType ).ToArray();
                ConverterFactory = converterFactory;
            }
        }

        private readonly struct EnumImplicitConverterInfo
        {
            public readonly Type[] OutputTypes;

            public EnumImplicitConverterInfo( Type converterType )
            {
                OutputTypes = GetOutputTypes( converterType ).ToArray();
            }
        }
    }
}

