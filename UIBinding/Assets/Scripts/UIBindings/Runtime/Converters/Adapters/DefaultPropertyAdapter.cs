using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Unity.Profiling;
using UnityEngine.Assertions;

namespace UIBindings.Adapters
{
    /// <summary>
    /// Default read/write property logic 
    /// </summary>
    // public sealed class DefaultPropertyAdapter<TSource, TProperty> : PropertyAdapterTyped<TSource, TProperty>
    // {
    //     public DefaultPropertyAdapter( [NotNull] object source, [NotNull] PropertyInfo propertyInfo, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged ) 
    //             : base ( source, propertyInfo, GenerateGetter( propertyInfo ), GenerateSetter( propertyInfo, isTwoWayBinding ), isTwoWayBinding, notifyPropertyChanged )
    //     {
    //         Assert.IsTrue( propertyInfo.PropertyType  == typeof(TProperty) );
    //     }
    //
    //     public DefaultPropertyAdapter( [NotNull] PropertyAdapter source, [NotNull] PropertyInfo propertyInfo, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged ) 
    //             : base ( source, propertyInfo, GenerateGetter( propertyInfo ), GenerateSetter( propertyInfo, isTwoWayBinding ), isTwoWayBinding, notifyPropertyChanged )
    //     {
    //         Assert.IsTrue( propertyInfo.PropertyType  == typeof(TProperty) );
    //     }
    //
    //     // TODO do not create setter for non final adapter (even if two-way binding)! Value is setted only on final adapter
    //     private static Action<TSource, TProperty> GenerateSetter(PropertyInfo propertyInfo, Boolean isTwoWayBinding )
    //     {
    //         if( isTwoWayBinding )
    //             return (Action<TSource, TProperty>)Delegate.CreateDelegate( typeof(Action<TSource, TProperty>), propertyInfo.GetSetMethod( true ) );
    //
    //         return null;
    //     }
    //
    //     private static Func<TSource, TProperty> GenerateGetter( PropertyInfo property )
    //     {
    //         return (Func<TSource, TProperty>)Delegate.CreateDelegate( typeof(Func<TSource, TProperty>), property.GetGetMethod( true ) );
    //     }
    // }
}