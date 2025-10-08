using System;
using System.Reflection;
using UnityEngine.Assertions;

namespace UIBindings.Adapters
{
    /// <summary>
    /// Read/write some property of some source object
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TProperty"></typeparam>
    public class PropertyAdapter<TSource, TProperty> : PathAdapterT<TSource, TProperty>
    {
        public override bool IsTwoWay => base.IsTwoWay && _setter != null;

        public override string MemberName { get; }

        private readonly Func<TSource, TProperty> _getter;
        private readonly Action<TSource, TProperty> _setter;
        private bool _actualIsTwoWay;

        public PropertyAdapter(PropertyInfo property, PathAdapter sourceAdapter, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged ) : base( sourceAdapter, isTwoWayBinding, notifyPropertyChanged )
        {
            Assert.IsTrue( property.PropertyType  == typeof(TProperty) );
            MemberName = property.Name;
            _getter = (Func<TSource, TProperty>)Delegate.CreateDelegate( typeof(Func<TSource, TProperty>), property.GetGetMethod( true ) );
            if ( isTwoWayBinding )
            {
                var setMethod = property.GetSetMethod( true );
                if ( setMethod != null )
                {
                    _setter = (Action<TSource, TProperty>)Delegate.CreateDelegate( typeof(Action<TSource, TProperty>), property.GetSetMethod( true ) );
                }
            }
        }

        public PropertyAdapter(PropertyInfo property, Type sourceObjectType, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged ) : base( sourceObjectType, isTwoWayBinding, notifyPropertyChanged )
        {
            Assert.IsTrue( property.PropertyType  == typeof(TProperty) );
            MemberName = property.Name;
            _getter = (Func<TSource, TProperty>)Delegate.CreateDelegate( typeof(Func<TSource, TProperty>), property.GetGetMethod( true ) );
            if ( isTwoWayBinding )
            {
                var setMethod = property.GetSetMethod( true );
                if ( setMethod != null )
                {
                    _setter = (Action<TSource, TProperty>)Delegate.CreateDelegate( typeof(Action<TSource, TProperty>), property.GetSetMethod( true ) );
                }
            }
        }

        protected override TProperty GetValue(TSource sourceObject )
        {
            return _getter( sourceObject );
        }

        protected override void SetValue(TSource sourceObject, TProperty value )
        {
            _setter( sourceObject, value );
        }
    }
}