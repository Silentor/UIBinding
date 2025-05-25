using System;
using System.Reflection;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Assertions;
using Object = System.Object;

#if UIBINDINGS_UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif

namespace UIBindings
{
    [Serializable]
    public class CallBinding : Binding
    {
        [SerializeReference]
        public System.Object Param; 

        //Optimized delegates for simple cases
        private Action                          _callVoid;
        private Action<int>                     _callInt;
        private Action<string>                  _callString;
        private Action<bool>                    _callBool;
        private Action<UnityEngine.Object>      _callUObject;
        private Func<Awaitable>                 _callAwaitable;
        private Func<Task>                      _callTask;
        private Func<ValueTask>                 _callValueTask;
#if UIBINDINGS_UNITASK_SUPPORT
        private Func<UniTask>                   _callUniTask;
        private Func<UniTaskVoid>               _callUniTaskVoid;
#endif
        //private AwaitableAction                 _callAwaitable;

        private ECallType _callType;
        private Boolean _isValid;
        private String _hostName;

        public void Awake( MonoBehaviour host )
        {
            if ( !Enabled )   
                return;

            if ( !Source )
            {
                Debug.LogError( $"[{nameof(Binding)}] Source is not assigned at {host.name}", host );
                return;
            }

            if ( String.IsNullOrEmpty( Path ) )
            {
                Debug.LogError( $"[{nameof(Binding)}] Path is not assigned at {host.name}", host );
                return;
            }

            var sourceType = Source.GetType();
            var method   = sourceType.GetMethod( Path );

            if ( method == null )
            {
                Debug.LogError( $"[{nameof(Binding)}] Method {Path} not found in {sourceType.Name}", host );
                return;
            }

            _hostName = host.name;

            var methodParams = method.GetParameters();

            //Setup optimized primitive call
            if ( method.ReturnType == typeof(void) )
            {
                if ( methodParams.Length == 0 )
                {
                    _callVoid = (Action)Delegate.CreateDelegate( typeof(Action), Source, method );
                    _callType = ECallType.Void;
                }
                else if ( methodParams.Length == 1 )
                {
                    if ( methodParams[ 0 ].ParameterType == typeof(int) )
                    {
                        _callInt = (Action<int>)Delegate.CreateDelegate( typeof(Action<int>), Source, method );
                        _callType  = ECallType.Int;
                    }
                    else if ( methodParams[ 0 ].ParameterType == typeof(string) )
                    {
                        _callString = (Action<string>)Delegate.CreateDelegate( typeof(Action<string>), Source, method );
                        _callType     = ECallType.String;
                    }
                    else if ( methodParams[ 0 ].ParameterType == typeof(bool) )
                    {
                        _callBool = (Action<bool>)Delegate.CreateDelegate( typeof(Action<bool>), Source, method );
                        _callType   = ECallType.Bool;
                    }
                    else if ( typeof(UnityEngine.Object).IsAssignableFrom( methodParams[ 0 ].ParameterType )  )
                    {
                        _callUObject = (Action<UnityEngine.Object>)Delegate.CreateDelegate( typeof(Action<UnityEngine.Object>), Source, method );
                        _callType      = ECallType.UObject;
                    }
                }
            }
            else    //Methods with result
            {
                //Most common awaitable types
                if ( method.ReturnType == typeof(Awaitable) )
                {
                    _callAwaitable = (Func<Awaitable>)Delegate.CreateDelegate( typeof(Func<Awaitable>), Source, method );
                    _callType = ECallType.Awaitable;
                }
                else if ( method.ReturnType == typeof(Task) )
                {
                    _callTask = (Func<Task>)Delegate.CreateDelegate( typeof(Func<Task>), Source, method );
                    _callType = ECallType.Task;
                }
                else if ( method.ReturnType == typeof(ValueTask) )
                {
                    _callValueTask = (Func<ValueTask>)Delegate.CreateDelegate( typeof(Func<ValueTask>), Source, method );
                    _callType = ECallType.ValueTask;
                }
#if UIBINDINGS_UNITASK_SUPPORT
                else if( method.ReturnType == typeof(UniTask) )
                {
                    _callUniTask = (Func<UniTask>)Delegate.CreateDelegate( typeof(Func<UniTask>), Source, method );
                    _callType    = ECallType.UniTask;
                }
                else if( method.ReturnType == typeof(UniTaskVoid) )
                {
                    _callUniTaskVoid = (Func<UniTaskVoid>)Delegate.CreateDelegate( typeof(Func<UniTaskVoid>), Source, method );
                    _callType = ECallType.UniTaskVoid;
                }
#endif
            }

            Assert.IsTrue( _callType != ECallType.Unknown, $"[{nameof(CallBinding)}] Method {Path} has unsupported signature at {_hostName}" );
            

            _isValid = true;
        }

        public void Call( )
        {
            if( !Enabled || !_isValid )
                return;

            if ( _callType == ECallType.Void )
                _callVoid();
            else if ( _callType == ECallType.Awaitable || _callType == ECallType.Task || _callType == ECallType.ValueTask )
            {
                ProcessAwaitableCall( );
                //_callAwaitable();
            }
#if UIBINDINGS_UNITASK_SUPPORT
            else if ( _callType == ECallType.UniTask )
            {
                ProcessAwaitableCall( );
            }            
            else if( _callType == ECallType.UniTaskVoid )
            {
                _callUniTaskVoid().Forget();
            }
#endif
        }

        private AwaitableAction GetAwaitableWrapper( MethodInfo method )
        {
            var awaitableType = method.ReturnType;
            var getAwaiterMethod = awaitableType.GetMethod( "GetAwaiter" );
            if ( getAwaiterMethod == null )
                return default;

            AwaitableAction result;
            result.AwaitableType = awaitableType;
            result.GetAwaitableMethod = (Func<Object>)Delegate.CreateDelegate( typeof(Func<Object>), Source, method );
            result.AwaiterType = getAwaiterMethod.ReturnType;
            result.GetAwaiterMethod = (Func<Object, Object>)Delegate.CreateDelegate( typeof(Func<Object, Object>), getAwaiterMethod );
            var isCompletedProperty = result.AwaiterType.GetProperty( "IsCompleted" );
            if ( isCompletedProperty == null || isCompletedProperty.PropertyType != typeof(bool) || isCompletedProperty.GetMethod == null )
                return default;
            result.IsCompletedProperty = (Func<Object, Boolean>)Delegate.CreateDelegate( typeof(Func<Object, Boolean>), isCompletedProperty.GetMethod );
            var getResultMethod = result.AwaiterType.GetMethod( "GetResult" );
            if( getResultMethod == null )
                return default;
            result.GetResultMethod = (Action<Object>)Delegate.CreateDelegate( typeof(Action<Object>), getResultMethod );

            return result;
        }

        private async void ProcessAwaitableCall(  )
        {
            switch ( _callType )
            {
                case ECallType.Awaitable:
                    await _callAwaitable();
                    break;

                case ECallType.Task:    
                    await _callTask();
                    break;

                case ECallType.ValueTask:
                    await _callValueTask();
                    break;

#if UIBINDINGS_UNITASK_SUPPORT
                case ECallType.UniTask:
                    await _callUniTask();
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

        public enum ECallType
        {
            Unknown,
            Void,
            Int,
            String,
            Bool,
            UObject,
            Awaitable,
            Task,
            ValueTask,
            UniTask,
            UniTaskVoid,
        }

        public struct AwaitableAction
        {
            public Type AwaitableType;
            public Type AwaiterType;
            public Func<Object> GetAwaitableMethod;              //Closed delegate on Source instance
            public Func<Object, Object> GetAwaiterMethod; //Open delegate on AwaitableType instance
            public Func<Object, Boolean> IsCompletedProperty;    //Open delegate on AwaiterType instance
            public Action<Object> GetResultMethod;               //Open delegate on AwaiterType instance


        }
    }
}