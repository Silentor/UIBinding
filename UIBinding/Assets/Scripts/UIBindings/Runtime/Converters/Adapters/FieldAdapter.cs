using System;
using System.Reflection;
using UnityEngine.Assertions;

namespace UIBindings.Adapters
{
    /// <summary>
    /// Read/write some field of some source object. This adapter inevitably boxes value types.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TField"></typeparam>
    public class FieldAdapter<TSource, TField> : PathAdapterT<TSource, TField>
    {
        public override bool IsTwoWay => base.IsTwoWay && !_isReadonly;

        public override string MemberName { get; }

        private readonly FieldInfo _field;
        private readonly bool _isReadonly;

        public FieldAdapter(FieldInfo field, PathAdapter sourceAdapter, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged ) : base( sourceAdapter, isTwoWayBinding, notifyPropertyChanged )
        {
            Assert.IsTrue( field.FieldType  == typeof(TField) );
            MemberName = field.Name;
            _field = field;
            _isReadonly = field.IsInitOnly;
        }

        public FieldAdapter(FieldInfo field, Type sourceObjectType, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged ) : base( sourceObjectType, isTwoWayBinding, notifyPropertyChanged )
        {
            Assert.IsTrue( field.FieldType  == typeof(TField) );
            MemberName = field.Name;
            _field = field;
            _isReadonly = field.IsInitOnly;
        }

        protected override TField GetValue(TSource sourceObject )
        {
            return (TField)_field.GetValue( sourceObject );
        }

        protected override void SetValue(TSource sourceObject, TField value )
        {
            _field.SetValue( sourceObject, value );
        }
    }
}