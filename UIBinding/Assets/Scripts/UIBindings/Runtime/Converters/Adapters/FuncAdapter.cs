using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

namespace UIBindings.Adapters
{
    /// <summary>
    /// Read some value from function of some source object
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    public class FuncAdapter<TSource, TResult> : PathAdapterT<TSource, TResult>
    {
        public override string MemberName { get; }

        private readonly Func<TSource, TResult> _getter;
        private readonly Action<TSource, TResult> _setter;

        public FuncAdapter(MethodInfo function, PathAdapter sourceAdapter, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged ) : base( sourceAdapter, isTwoWayBinding, notifyPropertyChanged )
        {
            Assert.IsTrue( function.ReturnType  == typeof(TResult) );
            MemberName = function.Name;
            _getter = (Func<TSource, TResult>)Delegate.CreateDelegate( typeof(Func<TSource, TResult>), function );
            if ( isTwoWayBinding )
                throw new InvalidOperationException("FuncAdapter does not support two-way binding"); //Actual only for last part of path
        }

        public FuncAdapter(MethodInfo function, Type sourceObjectType, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged ) : base( sourceObjectType, isTwoWayBinding, notifyPropertyChanged )
        {
            Assert.IsTrue( function.ReturnType  == typeof(TResult) );
            MemberName = function.Name;
            _getter = (Func<TSource, TResult>)Delegate.CreateDelegate( typeof(Func<TSource, TResult>), function );
            if ( isTwoWayBinding )
                throw new InvalidOperationException("FuncAdapter does not support two-way binding"); //Actual only for last part of path
        }

        protected override TResult GetValue(TSource sourceObject )
        {
            return _getter( sourceObject );
        }

        protected override void SetValue(TSource sourceObject, TResult value )
        {
            _setter( sourceObject, value );
        }
    }
}