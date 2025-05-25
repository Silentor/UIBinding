using System.Diagnostics;
using UnityEngine.Assertions;

namespace UIBindings.Runtime.Utils
{
    public static class AssertWithContext
    {
        [Conditional("UNITY_ASSERTIONS")]
        public static void IsTrue(bool condition, string message, UnityEngine.Object context )
        {
            if (!condition)
            {
                if( message != null && message.Length > 0 )
                {
                    message = $"Assertion failed: {message}";
                }
                else
                {
                    message = "Assertion failed: Expected True but actual false";
                }
                UnityEngine.Debug.LogError( message, context );
                throw new AssertionException( "Expected True but actual false", message);
            }
        }
    }
}