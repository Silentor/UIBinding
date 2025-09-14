using System;
using System.Reflection;

namespace UIBindings.Runtime.Utils
{
    public static class DelegateUtils
    {
        public static Action<TSource, object> GetOpenAction <TSource>( MethodInfo method, Type paramType )
        {
            var constructMethod  = _constructWrappedOpenAction1 ??= typeof(DelegateUtils).GetMethod( nameof( ConstructWrappedOpenAction1 ), BindingFlags.NonPublic | BindingFlags.Static );
            var constructMethod2 = constructMethod.MakeGenericMethod( typeof(TSource), paramType );
            var result              = (Action<TSource, object>)constructMethod2.Invoke( null, new [] { method } );
            return result;
        }

        public static Action<TSource, object, object> GetOpenAction <TSource>( MethodInfo method, Type paramType1, Type paramType2 )
        {
            var constructMethod  = _constructWrappedOpenAction2 ??= typeof(DelegateUtils).GetMethod( nameof( ConstructWrappedOpenAction2 ), BindingFlags.NonPublic | BindingFlags.Static );
            var constructMethod2 = constructMethod.MakeGenericMethod( typeof(TSource), paramType1, paramType2 );
            var result              = (Action<TSource, object, object>)constructMethod2.Invoke( null, new [] { method } );
            return result;
        }

        public static Func<TSource, object, TResult> GetOpenFunc <TSource, TResult>( MethodInfo method, Type paramType1 )
        {
            var constructMethod  = _constructWrappedOpenFunc1 ??= typeof(DelegateUtils).GetMethod( nameof( ConstructWrappedOpenFunc1 ), BindingFlags.NonPublic | BindingFlags.Static );
            var constructMethod2 = constructMethod.MakeGenericMethod( typeof(TSource), paramType1, typeof(TResult) );
            var result              = (Func<TSource, object, TResult>)constructMethod2.Invoke( null, new [] { method } );
            return result;
        }

        public static Func<TSource, object, object, TResult> GetOpenFunc <TSource, TResult>( MethodInfo method, Type paramType1, Type paramType2 )
        {
            var constructMethod  = _constructWrappedOpenFunc2 ??= typeof(DelegateUtils).GetMethod( nameof( ConstructWrappedOpenFunc2 ), BindingFlags.NonPublic | BindingFlags.Static );
            var constructMethod2 = constructMethod.MakeGenericMethod( typeof(TSource), paramType1, paramType2, typeof(TResult) );
            var result              = (Func<TSource, object, object, TResult>)constructMethod2.Invoke( null, new [] { method } );
            return result;
        }

        private static Action<TSource, object> ConstructWrappedOpenAction1<TSource, TParam>( MethodInfo method )
        {
            var strongTyped = (Action<TSource, TParam>) Delegate.CreateDelegate( typeof(Action<TSource, TParam>), method );
            void WeakTyped(TSource s, object p ) => strongTyped( s, (TParam)p );
            return WeakTyped;
        }

        private static Action<TSource, object, object> ConstructWrappedOpenAction2<TSource, TParam1, TParam2>( MethodInfo method )
        {
            var strongTyped = (Action<TSource, TParam1, TParam2>) Delegate.CreateDelegate( typeof(Action<TSource, TParam1, TParam2>), method );
            void WeakTyped(TSource s, object p1, object p2 ) => strongTyped( s, (TParam1)p1, (TParam2)p2 );
            return WeakTyped;
        }

        private static Func<TSource, object, TResult> ConstructWrappedOpenFunc1<TSource, TParam1, TResult>( MethodInfo method )
        {
            var strongTyped = (Func<TSource, TParam1, TResult>) Delegate.CreateDelegate( typeof(Func<TSource, TParam1, TResult>), method );
            TResult WeakTyped(TSource s, object p1 ) => strongTyped( s, (TParam1)p1 );
            return WeakTyped;
        }

        private static Func<TSource, object, object, TResult> ConstructWrappedOpenFunc2<TSource, TParam1, TParam2, TResult>( MethodInfo method )
        {
            var strongTyped = (Func<TSource, TParam1, TParam2, TResult>) Delegate.CreateDelegate( typeof(Func<TSource, TParam1, TParam2, TResult>), method );
            TResult WeakTyped(TSource s, object p1, object p2 ) => strongTyped( s, (TParam1)p1, (TParam2)p2 );
            return WeakTyped;
        }

        private static MethodInfo _constructWrappedOpenAction1;
        private static MethodInfo _constructWrappedOpenAction2;
        private static MethodInfo _constructWrappedOpenFunc1;
        private static MethodInfo _constructWrappedOpenFunc2;
    }
}