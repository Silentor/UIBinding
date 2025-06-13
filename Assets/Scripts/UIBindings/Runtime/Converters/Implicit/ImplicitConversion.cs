using System;
using System.Collections.Generic;
using System.Linq;
using UIBindings.Runtime;
using UIBindings.Runtime.Utils;

namespace UIBindings.Converters
{
    public static class ImplicitConversion
    {
        /// <summary>
        /// Get converter to convert source output to given type
        /// </summary>
        /// <param name="source"></param>
        /// <param name="convertTo"></param>
        /// <returns></returns>
        public static DataProvider GetConverter( DataProvider source, Type convertTo )    
        {
            var sourceType = source.GetType();
            foreach ( var converterInfo in ConverterInfoCache )
            {
                if( converterInfo.DataReaderType.IsAssignableFrom( sourceType ) && Array.IndexOf( converterInfo.OutputTypes, convertTo ) >= 0 )
                {
                    return converterInfo.ConverterFactory( source );
                }
            }

            return null;
        }
        
        public static bool IsConversionSupported( Type sourceType, Type convertTo )
        {
            foreach ( var converterInfo in ConverterInfoCache )
            {
                if( converterInfo.InputType == sourceType && Array.IndexOf( converterInfo.OutputTypes, convertTo ) >= 0 )
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsConversionSupported( Type sourceType, Predicate<Type> isSupportedType )
        {
            foreach ( var converterInfo in ConverterInfoCache )
            {
                if( converterInfo.InputType == sourceType && Array.Exists( converterInfo.OutputTypes, isSupportedType ) )
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
                           new ImplicitConverterInfo( typeof(StructEnum), typeof(ImplicitEnumConverter), static inp => new ImplicitEnumConverter( inp ) ),
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
    }
}

