using System;
using System.Collections.Generic;
using System.Reflection;
using UIBindings.Runtime;
using UIBindings.Runtime.Utils;
using UnityEngine;
using System.Threading.Tasks;
using Sisus;

#if UIBINDINGS_UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif

namespace UIBindings.Adapters
{
    /// <summary>
    /// Special path adapter to call some method. Must be the last in the path. Return value is ignored (except async methods).
    /// Parameters are passed via list of SerializableParam in Call() method.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    public class CallMethodAdapter<TSource> : MethodAdapter
    {
        public CallMethodAdapter(MethodInfo method, PathAdapter sourceAdapter, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged ) : base( sourceAdapter, isTwoWayBinding, notifyPropertyChanged )
        {
            MemberName = method.Name;
            SetupDelegates( method );
        }

        public CallMethodAdapter(MethodInfo method, object sourceObject, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged ) : base( sourceObject, isTwoWayBinding, notifyPropertyChanged )
        {
            MemberName = method.Name;
            SetupDelegates( method );
        }

        /// <summary>
        /// Some ugly code to setup delegates for various method signatures
        /// </summary>
        /// <param name="method"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private void SetupDelegates( MethodInfo method )
        {
            var paramz = method.GetParameters();
            _methodParams = paramz;
            if ( method.ReturnType == typeof(void) )     // Sync method
            {
                _awaitType = EAwaitableType.Sync;
                if ( paramz.Length == 0 )
                {
                    _delegateCall = Delegate.CreateDelegate( typeof(Action<TSource>), method );
                    _paramsType = EParamsType.Void;
                }
                else if ( paramz.Length == 1 )
                {
                    var paramType = paramz[0].ParameterType;
                    if ( paramType == typeof(int) )
                    {
                        _delegateCall = Delegate.CreateDelegate( typeof(Action<TSource, int>), method );
                        _paramsType = EParamsType.Int;
                    }
                    else
                    {
                        _delegateCall = DelegateUtils.GetOpenAction<TSource>( method, paramType );
                        _paramsType = EParamsType.Boxed1Param;
                    }
                }
                else if ( paramz.Length == 2 )
                {
                    var paramType1 = paramz[0].ParameterType;
                    var paramType2 = paramz[1].ParameterType;
                    _delegateCall = DelegateUtils.GetOpenAction<TSource>( method, paramType1, paramType2 );
                    _paramsType = EParamsType.Boxed2Params;
                }
                else
                {
                    throw new InvalidOperationException("Incompatible sync method signature. Only 0, 1 or 2 parameters are supported.");
                }
            }
            else                                    // Some task method
            {
                if ( paramz.Length == 0 )
                {
                    if ( method.ReturnType == typeof(Awaitable) )
                    {
                        _delegateCall = Delegate.CreateDelegate( typeof(Func<TSource, Awaitable>), method );
                        _paramsType   = EParamsType.Void;
                        _awaitType    = EAwaitableType.Awaitable;
                    }
                    else if ( method.ReturnType == typeof(Task) )
                    {
                        _delegateCall = Delegate.CreateDelegate( typeof(Func<TSource, Task>), method );
                        _paramsType   = EParamsType.Void;
                        _awaitType    = EAwaitableType.Task;
                    }
                    else if ( method.ReturnType == typeof(ValueTask) )
                    {
                        _delegateCall = Delegate.CreateDelegate( typeof(Func<TSource, ValueTask>), method );
                        _paramsType   = EParamsType.Void;
                        _awaitType    = EAwaitableType.ValueTask;
                    }
#if UIBINDINGS_UNITASK_SUPPORT
                    else if ( method.ReturnType == typeof(UniTask) )
                    {
                        _delegateCall = Delegate.CreateDelegate( typeof(Func<TSource, UniTask>), method );
                        _paramsType   = EParamsType.Void;
                        _awaitType    = EAwaitableType.UniTask;
                    }
                    else if ( method.ReturnType == typeof(UniTaskVoid) )
                    {
                        _delegateCall = Delegate.CreateDelegate( typeof(Func<TSource, UniTaskVoid>), method );
                        _paramsType   = EParamsType.Void;
                        _awaitType    = EAwaitableType.UniTaskVoid;
                    }
#endif
                }
                else if ( paramz.Length == 1 )
                {
                    var paramType = paramz[ 0 ].ParameterType;
                    if ( paramType == typeof(int) )      //Optimized awaitable calls for 1 int param
                    {
                        if ( method.ReturnType == typeof(Awaitable) )
                        {
                            _delegateCall = Delegate.CreateDelegate( typeof(Func<TSource, int, Awaitable>), method );
                            _paramsType   = EParamsType.Int;
                            _awaitType    = EAwaitableType.Awaitable;
                        }
                        else if ( method.ReturnType == typeof(Task) )
                        {
                            _delegateCall = Delegate.CreateDelegate( typeof(Func<TSource, int, Task>), method );
                            _paramsType   = EParamsType.Int;
                            _awaitType    = EAwaitableType.Task;
                        }
                        else if ( method.ReturnType == typeof(ValueTask) )
                        {
                            _delegateCall = Delegate.CreateDelegate( typeof(Func<TSource, int, ValueTask>), method );
                            _paramsType   = EParamsType.Int;
                            _awaitType    = EAwaitableType.ValueTask;
                        }
#if UIBINDINGS_UNITASK_SUPPORT
                        else if ( method.ReturnType == typeof(UniTask) )
                        {
                            _delegateCall = Delegate.CreateDelegate( typeof(Func<TSource, int, UniTask>), method );
                            _paramsType   = EParamsType.Int;
                            _awaitType    = EAwaitableType.UniTask;
                        }
                        else if ( method.ReturnType == typeof(UniTaskVoid) )
                        {
                            _delegateCall = Delegate.CreateDelegate( typeof(Func<TSource, int, UniTaskVoid>), method );
                            _paramsType   = EParamsType.Int;
                            _awaitType    = EAwaitableType.UniTaskVoid;
                        }
#endif
                    }
                    else        // task method with 1 parameter other than int
                    {
                        if ( method.ReturnType == typeof(Awaitable) )
                        {
                            _delegateCall = DelegateUtils.GetOpenFunc<TSource, Awaitable>( method, paramType );
                            _paramsType   = EParamsType.Boxed1Param;
                            _awaitType    = EAwaitableType.Awaitable;
                        }
                        else if ( method.ReturnType == typeof(Task) )
                        {
                            _delegateCall = DelegateUtils.GetOpenFunc<TSource, Task>( method, paramType );
                            _paramsType   = EParamsType.Boxed1Param;
                            _awaitType    = EAwaitableType.Task;
                        }
                        else if ( method.ReturnType == typeof(ValueTask) )
                        {
                            _delegateCall = DelegateUtils.GetOpenFunc<TSource, ValueTask>( method, paramType );
                            _paramsType   = EParamsType.Boxed1Param;
                            _awaitType    = EAwaitableType.ValueTask;
                        }
#if UIBINDINGS_UNITASK_SUPPORT
                        else if ( method.ReturnType == typeof(UniTask) )
                        {
                            _delegateCall = DelegateUtils.GetOpenFunc<TSource, UniTask>( method, paramType );
                            _paramsType   = EParamsType.Boxed1Param;
                            _awaitType    = EAwaitableType.UniTask;
                        }
                        else if ( method.ReturnType == typeof(UniTaskVoid) )
                        {
                            _delegateCall = DelegateUtils.GetOpenFunc<TSource, UniTaskVoid>( method, paramType );
                            _paramsType   = EParamsType.Boxed1Param;
                            _awaitType    = EAwaitableType.UniTaskVoid;
                        }
#endif
                    }
                }
                else if ( paramz.Length == 2 )      // task method with 2 parameters (2 boxed params only)
                {
                    var paramType1 = paramz[ 0 ].ParameterType;
                    var paramType2 = paramz[ 1 ].ParameterType;
                    if ( method.ReturnType == typeof(Awaitable) )
                    {
                        _delegateCall = DelegateUtils.GetOpenFunc<TSource, Awaitable>( method, paramType1, paramType2 );
                        _paramsType   = EParamsType.Boxed2Params;
                        _awaitType    = EAwaitableType.Awaitable;
                    }
                    else if ( method.ReturnType == typeof(Task) )
                    {
                        _delegateCall = DelegateUtils.GetOpenFunc<TSource, Task>( method, paramType1, paramType2 );
                        _paramsType   = EParamsType.Boxed2Params;
                        _awaitType    = EAwaitableType.Task;
                    }
                    else if ( method.ReturnType == typeof(ValueTask) )
                    {
                        _delegateCall = DelegateUtils.GetOpenFunc<TSource, ValueTask>( method, paramType1, paramType2 );
                        _paramsType   = EParamsType.Boxed2Params;
                        _awaitType    = EAwaitableType.ValueTask;
                    }
#if UIBINDINGS_UNITASK_SUPPORT
                    else if ( method.ReturnType == typeof(UniTask) )
                    {
                        _delegateCall = DelegateUtils.GetOpenFunc<TSource, UniTask>( method, paramType1, paramType2 );
                        _paramsType   = EParamsType.Boxed2Params;
                        _awaitType    = EAwaitableType.UniTask;
                    }
                    else if ( method.ReturnType == typeof(UniTaskVoid) )
                    {
                        _delegateCall =
                                DelegateUtils.GetOpenFunc<TSource, UniTaskVoid>( method, paramType1, paramType2 );
                        _paramsType = EParamsType.Boxed2Params;
                        _awaitType  = EAwaitableType.UniTaskVoid;
                    }
#endif
                }
            }

            if( _paramsType == EParamsType.Unknown )
                throw new InvalidOperationException("Incompatible method signature. Only 0, 1 or 2 parameters are supported, return type must be void or some kind of task.");
        }

        public override Type InputType => typeof(TSource);
        public override Type OutputType => typeof(void);

        public override string MemberName { get; }

        public override EResult TryGetValue(out object value )
        {
            // Not actual for method call, use Call() instead
            throw new NotImplementedException();
        }

        public void CallSync( IReadOnlyList<SerializableParam> paramz )
        {
            TSource sourceObject = default;
            if ( SourceObject != null )
            {
                sourceObject = (TSource)SourceObject;
            }
            else if( SourceAdapter is IDataReader<TSource> sourceAdapter )
            {
                _ = sourceAdapter.TryGetValue( out sourceObject );
            }

            if ( _awaitType == EAwaitableType.Sync )
            {
                CallSync( sourceObject, paramz );
            }
            else
            {
                throw new InvalidOperationException("Cannot call async method in sync way. Use CallAsync() instead." );
            }
        }

        public override Awaitable CallAsync( IReadOnlyList<SerializableParam> paramz )
        {
            TSource sourceObject = default;
            if ( SourceObject != null )
            {
                sourceObject = (TSource)SourceObject;
            }
            else if( SourceAdapter is IDataReader<TSource> sourceAdapter )
            {
                _ = sourceAdapter.TryGetValue( out sourceObject );
            }

            if ( _awaitType == EAwaitableType.Sync )
            {
                CallSync( sourceObject, paramz );
                return AwaitableUtility.CompletedAwaitable;
            }
            else if ( _awaitType == EAwaitableType.UniTaskVoid ) //Task, but not awaitable actually
            {
#if UIBINDINGS_UNITASK_SUPPORT
                UniTaskVoid task = default;
                if( _paramsType == EParamsType.Void )
                    task = ((Func<TSource, UniTaskVoid>)_delegateCall)( sourceObject );
                else if( _paramsType == EParamsType.Int )
                    task = ((Func<TSource, int, UniTaskVoid>)_delegateCall)( sourceObject, paramz[0].GetInt() );
                else if( _paramsType == EParamsType.Boxed1Param )
                    task = ((Func<TSource, object, UniTaskVoid>)_delegateCall)( sourceObject, paramz[0].GetBoxedValue( _methodParams[0].ParameterType ) );
                else if( _paramsType == EParamsType.Boxed2Params )
                    task = ((Func<TSource, object, object, UniTaskVoid>)_delegateCall)( sourceObject, paramz[0].GetBoxedValue( _methodParams[0].ParameterType ), paramz[1].GetBoxedValue( _methodParams[1].ParameterType ) );
                task.Forget();
#endif
                return AwaitableUtility.CompletedAwaitable;
            }
            else
            {
                return CallAsync( sourceObject, paramz );
            }
        }

        private void CallSync(TSource sourceObject, IReadOnlyList<SerializableParam> paramz )
        {
            switch ( _paramsType )
            {
                case EParamsType.Void:
                    ((Action<TSource>)_delegateCall)( sourceObject );
                    break;
                case EParamsType.Int:
                    {
                        var p1 = paramz.Count > 0 ? paramz[ 0 ].GetInt() : 0;
                        ((Action<TSource, int>)_delegateCall)( sourceObject, p1 );
                    }
                    break;
                case EParamsType.Boxed1Param:
                    {
                        var p1 = paramz[0].GetBoxedValue( _methodParams[0].ParameterType );
                        ((Action<TSource, object>)_delegateCall)( sourceObject, p1 );
                    }
                    break;
                case EParamsType.Boxed2Params:
                    {
                        var p1 = paramz[0].GetBoxedValue( _methodParams[0].ParameterType );
                        var p2 = paramz[1].GetBoxedValue( _methodParams[1].ParameterType );
                        ((Action<TSource, object, object>)_delegateCall)( sourceObject, p1, p2 );
                    }
                    break;
            }
        }

        private async Awaitable CallAsync(TSource sourceObject, IReadOnlyList<SerializableParam> paramz )
        {
            switch ( _awaitType )
            {
                case EAwaitableType.Awaitable:
                    Awaitable task = null;
                    if( _paramsType == EParamsType.Void )
                        task = ((Func<TSource, Awaitable>)_delegateCall)( sourceObject );
                    else if( _paramsType == EParamsType.Int )
                        task = ((Func<TSource, int, Awaitable>)_delegateCall)( sourceObject, paramz[0].GetInt() );
                    else if( _paramsType == EParamsType.Boxed1Param )
                        task = ((Func<TSource, object, Awaitable>)_delegateCall)( sourceObject, paramz[0].GetBoxedValue( _methodParams[0].ParameterType ) );
                    else if( _paramsType == EParamsType.Boxed2Params )
                        task = ((Func<TSource, object, object, Awaitable>)_delegateCall)( sourceObject, paramz[0].GetBoxedValue( _methodParams[0].ParameterType ), paramz[1].GetBoxedValue( _methodParams[1].ParameterType ) );
                    await task;
                    break;

                case EAwaitableType.Task:
                    Task t = null;
                    if( _paramsType == EParamsType.Void )
                        t = ((Func<TSource, Task>)_delegateCall)( sourceObject );
                    else if( _paramsType == EParamsType.Int )
                        t = ((Func<TSource, int, Task>)_delegateCall)( sourceObject, paramz[0].GetInt() );
                    else if( _paramsType == EParamsType.Boxed1Param )
                        t = ((Func<TSource, object, Task>)_delegateCall)( sourceObject, paramz[0].GetBoxedValue( _methodParams[0].ParameterType ) );
                    else if( _paramsType == EParamsType.Boxed2Params )
                        t = ((Func<TSource, object, object, Task>)_delegateCall)( sourceObject, paramz[0].GetBoxedValue( _methodParams[0].ParameterType ), paramz[1].GetBoxedValue( _methodParams[1].ParameterType ) );
                    await t;
                    break;

                case EAwaitableType.ValueTask:
                    ValueTask vt = default;
                    if( _paramsType == EParamsType.Void )
                        vt = ((Func<TSource, ValueTask>)_delegateCall)( sourceObject );
                    else if( _paramsType == EParamsType.Int )
                        vt = ((Func<TSource, int, ValueTask>)_delegateCall)( sourceObject, paramz[0].GetInt() );
                    else if( _paramsType == EParamsType.Boxed1Param )
                        vt = ((Func<TSource, object, ValueTask>)_delegateCall)( sourceObject, paramz[0].GetBoxedValue( _methodParams[0].ParameterType ) );
                    else if( _paramsType == EParamsType.Boxed2Params )
                        vt = ((Func<TSource, object, object, ValueTask>)_delegateCall)( sourceObject, paramz[0].GetBoxedValue( _methodParams[0].ParameterType ), paramz[1].GetBoxedValue( _methodParams[1].ParameterType ) );
                    await vt;
                    break;

#if UIBINDINGS_UNITASK_SUPPORT
                case EAwaitableType.UniTask:
                    UniTask ut = default;
                    if( _paramsType == EParamsType.Void )
                        ut = ((Func<TSource, UniTask>)_delegateCall)( sourceObject );
                    else if( _paramsType == EParamsType.Int )
                        ut = ((Func<TSource, int, UniTask>)_delegateCall)( sourceObject, paramz[0].GetInt() );
                    else if( _paramsType == EParamsType.Boxed1Param )
                        ut = ((Func<TSource, object, UniTask>)_delegateCall)( sourceObject, paramz[0].GetBoxedValue( _methodParams[0].ParameterType ) );
                    else if( _paramsType == EParamsType.Boxed2Params )
                        ut = ((Func<TSource, object, object, UniTask>)_delegateCall)( sourceObject, paramz[0].GetBoxedValue( _methodParams[0].ParameterType ), paramz[1].GetBoxedValue( _methodParams[1].ParameterType ) );
                    await ut;
                    break;
#endif
            }

            // var awaitable = awaitableMethod.GetAwaitableMethod();
            // var awaiter = awaitableMethod.GetAwaiterMethod( awaitable );
            //
            // //Wait for completion
            // while ( !awaitableMethod.IsCompletedProperty( awaiter ) )
            // {
            //     await Awaitable.NextFrameAsync(  );
            // }
            //
            // //Get result, unwind exception
            // awaitableMethod.GetResultMethod( awaiter );
        }
        

        private ParameterInfo[] _methodParams; 
        private Delegate _delegateCall;
        private EParamsType _paramsType;
        private EAwaitableType _awaitType;

        public enum EParamsType
        {
            Unknown,
            Void,
            Int,
            Boxed1Param,
            Boxed2Params, 
        }

        public enum EAwaitableType
        {
            Sync,
            Awaitable,
            Task,
            ValueTask,
            UniTask,
            UniTaskVoid,
        }

    }
}