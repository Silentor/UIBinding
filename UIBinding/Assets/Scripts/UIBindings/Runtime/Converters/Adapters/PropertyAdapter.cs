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
        public override Boolean IsTwoWay { get; }

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

        public static PropertyAdapter GetPropertyAdapter( [NotNull] object source, [NotNull] PropertyInfo propertyInfo, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged )
        {
            Assert.IsNotNull( source );
            Assert.IsNotNull( propertyInfo );
            var type = propertyInfo.PropertyType;

            //Fast way for some common types
            if ( type == typeof(int) )
                return new PropertyAdapter<int>( source, propertyInfo, isTwoWayBinding, notifyPropertyChanged );
            else if ( type == typeof(float) )
                return new PropertyAdapter<float>( source, propertyInfo, isTwoWayBinding, notifyPropertyChanged );
            else if( type == typeof(bool) )
                return new PropertyAdapter<bool>( source, propertyInfo, isTwoWayBinding, notifyPropertyChanged );
            else if ( type == typeof(string) )
                return new PropertyAdapter<string>( source, propertyInfo, isTwoWayBinding, notifyPropertyChanged );
            else if( type.IsEnum )
                return new StructEnumPropertyAdapter( source, propertyInfo, isTwoWayBinding, notifyPropertyChanged );

            //Slow way for all other types
            var adapterType = typeof(PropertyAdapter<>).MakeGenericType( type );
            return (PropertyAdapter)Activator.CreateInstance( adapterType, source, propertyInfo, isTwoWayBinding, notifyPropertyChanged );
        }

        public static PropertyAdapter GetInnerPropertyAdapter( PropertyAdapter sourceAdapter, PropertyInfo propertyInfo, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged )
        {
            var sourceType = propertyInfo.DeclaringType;
            var propertyType = propertyInfo.PropertyType;
            var complexAdapterType = typeof(InnerPropertyAdapter<,>).MakeGenericType( sourceType, propertyType );
            return (PropertyAdapter)Activator.CreateInstance( complexAdapterType, sourceAdapter, propertyInfo, isTwoWayBinding, notifyPropertyChanged );
        }

        public PropertyAdapter( [NotNull] PropertyInfo propertyInfo, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged )
        {
            Assert.IsNotNull( propertyInfo );
            Assert.IsTrue( !isTwoWayBinding || propertyInfo.CanWrite );

            NotifyPropertyChanged = notifyPropertyChanged;
            PropertyName = propertyInfo.Name;
            IsTwoWay = isTwoWayBinding;
        }

        /// <summary>
        /// For debugging, its ignore boxing
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract EResult TryGetValue(out object value );

        /// <summary>
        /// Property adapter will be detect his property changing by subscribing to INotifyPropertyChanged of source object.
        /// If source object does not support INotifyPropertyChanged, method will do nothing.
        /// </summary>
        public virtual void Subscribe( )
        {
            IsSubscribed = true;
        }

        public virtual void Unsubscribe( )
        {
            IsSubscribed = false;
        }

        public abstract Boolean IsNeedPolling( );

        protected bool IsSubscribed;
        protected readonly String PropertyName;
        protected readonly Action<Object, String> NotifyPropertyChanged;

        protected static readonly ProfilerMarker ReadPropertyMarker  = new ( ProfilerCategory.Scripts,  $"Binding.{nameof(PropertyAdapter)}.ReadProperty" );
        protected static readonly ProfilerMarker WritePropertyMarker = new ( ProfilerCategory.Scripts,  $"Binding.{nameof(PropertyAdapter)}.WriteProperty" );
    }
}