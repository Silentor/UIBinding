using System;
using System.Reflection;
using JetBrains.Annotations;
using UIBindings.Runtime;
using Unity.Profiling;
using UnityEngine.Assertions;

namespace UIBindings.Adapters
{
    /// <summary>
    /// Data provider that reads and writes property of some object.
    /// </summary>
    public abstract class PropertyAdapter : DataProvider
    {
        public abstract Type OutputType  { get; }

        /// <summary>
        /// Sometimes we need to adapt type of property to some other type, for example, if it is enum we want to use StructEnum
        /// </summary>
        /// <param name="propertyType"></param>
        /// <returns></returns>
        public static Type GetAdaptedType( Type propertyType )
        {
            if ( propertyType == null ) return null;
            if ( propertyType.IsEnum ) return typeof(StructEnum);
            return propertyType;
        }

        public static PropertyAdapter GetPropertyAdapter( [NotNull] object source, [NotNull] PropertyInfo propertyInfo, bool isTwoWayBinding )
        {
            Assert.IsNotNull( source );
            Assert.IsNotNull( propertyInfo );
            var type = propertyInfo.PropertyType;

            //Fast way for some common types
            if ( type == typeof(int) )
                return new PropertyAdapter<int>( source, propertyInfo, isTwoWayBinding );
            else if ( type == typeof(float) )
                return new PropertyAdapter<float>( source, propertyInfo, isTwoWayBinding );
            else if( type == typeof(bool) )
                return new PropertyAdapter<bool>( source, propertyInfo, isTwoWayBinding );
            else if ( type == typeof(string) )
                return new PropertyAdapter<string>( source, propertyInfo, isTwoWayBinding );
            else if( type.IsEnum )
                return new StructEnumPropertyAdapter( source, propertyInfo, isTwoWayBinding );

            //Slow way for all other types
            var adapterType = typeof(PropertyAdapter<>).MakeGenericType( type );
            return (PropertyAdapter)Activator.CreateInstance( adapterType, source, propertyInfo, isTwoWayBinding, null );
        }

        public static PropertyAdapter GetInnerPropertyAdapter( PropertyAdapter sourceAdapter, PropertyInfo propertyInfo, bool isTwoWayBinding )
        {
            var sourceType = propertyInfo.DeclaringType;
            var propertyType = propertyInfo.PropertyType;
            var complexAdapterType = typeof(ComplexPropertyAdapter<,>).MakeGenericType( sourceType, propertyType );
            return (PropertyAdapter)Activator.CreateInstance( complexAdapterType, sourceAdapter, propertyInfo, isTwoWayBinding );
        }

        /// <summary>
        /// For debugging, ignore boxing
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract EResult TryGetValue(out object value );

        protected static readonly ProfilerMarker ReadPropertyMarker  = new ( ProfilerCategory.Scripts,  $"{nameof(PropertyAdapter)}.ReadProperty" );
        protected static readonly ProfilerMarker WritePropertyMarker = new ( ProfilerCategory.Scripts,  $"{nameof(PropertyAdapter)}.WriteProperty" );
    }
}