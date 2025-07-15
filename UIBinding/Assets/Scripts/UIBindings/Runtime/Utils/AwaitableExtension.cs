using System;
using UnityEngine;

namespace UIBindings.Runtime.Utils
{
    public static class AwaitableExtension
    {
        public static void Forget(this Awaitable awaitable, Action<Exception> exceptionHandler = null)
        {
            var awaiter = awaitable.GetAwaiter();
            if(!awaiter.IsCompleted)
            {
                awaiter.OnCompleted(HandleLogException);
                return;
            }

            HandleLogException();

            void HandleLogException()
            {
                try
                {
                    awaiter.GetResult();
                }
                catch (OperationCanceledException){}
                catch (Exception ex)
                {
                    if ( exceptionHandler != null )
                        exceptionHandler( ex );
                    else
                        Debug.LogException(ex);
                }
            }
        }

        public static void Forget<TResult>(this Awaitable<TResult> awaitable)
        {
            var awaiter = awaitable.GetAwaiter();
            if(!awaiter.IsCompleted)
            {
                awaiter.OnCompleted(HandleLogException);
                return;
            }

            HandleLogException();

            void HandleLogException()
            {
                try
                {
                    _ = awaiter.GetResult();
                }
                catch (OperationCanceledException){}
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }
}