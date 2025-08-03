using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
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
                message = String.IsNullOrEmpty( message ) ? "Expected True but actual False" : $"Assertion failed: {message}";
                ThrowAssertionException( message, context );
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void IsFalse(bool condition, string message, UnityEngine.Object context )
        {
            if (condition)
            {
                message = String.IsNullOrEmpty( message ) ? "Expected False but actual True" : $"Assertion failed: {message}";
                ThrowAssertionException( message, context );
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void IsNotNull( UnityEngine.Object value, string message = null, UnityEngine.Object context = null )
        {
            if ( !value )
            {
                message = String.IsNullOrEmpty( message ) ? "Expected not null but actual null" : $"Assertion failed: {message}";
                ThrowAssertionException( message, context );
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void IsNotNull( object value, string message = null, UnityEngine.Object context = null )
        {
            if (value == null)
            {
                message = String.IsNullOrEmpty( message ) ? "Expected not null but actual null" : $"Assertion failed: {message}";
                ThrowAssertionException( message, context );
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void IsNotEmpty( string value, string message, UnityEngine.Object context )
        {
            if (String.IsNullOrEmpty( value ))
            {
                message = String.IsNullOrEmpty( message ) ? "Expected string not empty but actual empty" : $"Assertion failed: {message}";
                ThrowAssertionException( message, context );
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void IsNull( [CanBeNull] object value, string message, UnityEngine.Object context )
        {
            if (value != null)
            {
                message = String.IsNullOrEmpty( message ) ? "Expected null but actual not null" : $"Assertion failed: {message}";
                ThrowAssertionException( message, context );
            }
        }

        [DoesNotReturn]
        private static void ThrowAssertionException( string message, UnityEngine.Object context )
        {
            UnityEngine.Debug.LogAssertion( message, context );
            throw new AssertionException( "Assertion failed", message );
        }
    }
}