using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UIBindings.Runtime;
using UnityEngine;

namespace UIBindings.Tests.Runtime
{
    public class CallMethodTests
    {
        [Test]
        public void TestSimpleSyncMethodCall( )
        {
            var source = new CallMethodSource();

            var callBinding = new CallBinding();
            callBinding.Path = "CallNoParams";
            callBinding.Init( source );
            callBinding.Call();
            Debug.Assert( source.IntValue == 1 );

            callBinding.Path = "CallInt1Param";
            callBinding.Params = new[] { SerializableParam.FromInt( 42 ),  };
            callBinding.Init( source );
            callBinding.Call();
            Debug.Assert( source.IntValue == 42 );

            callBinding.Path   = "CallInt2Params";
            callBinding.Params = new[] { SerializableParam.FromInt( 2 ), SerializableParam.FromInt( 2 ), };
            callBinding.Init( source );
            callBinding.Call();
            Debug.Assert( source.IntValue == 4 );

            callBinding.Path   = "CallString1Param";
            callBinding.Params = new[] { SerializableParam.FromString( "Hello" ), };
            callBinding.Init( source );
            callBinding.Call();
            Debug.Assert( source.StringValue == "Hello" );

            callBinding.Path   = "CallMixedParams";
            callBinding.Params = new[] { SerializableParam.FromFloat( 3.14f ), SerializableParam.FromBool( true ), };
            callBinding.Init( source );
            callBinding.Call();
            Debug.Assert( source.FloatValue == 3.14f );
            Debug.Assert( source.BoolValue == true );
        }

        [Test]
        public async Task TestSimpleAsyncMethodCall( )
        {
            var source = new CallMethodSource();

            var callBinding = new CallBinding();
            callBinding.Path = "CallTaskNoParams";
            callBinding.Init( source );
            await callBinding.Call();
            Debug.Assert( source.IntValue == 1 );

            callBinding.Path = "CallAwaitableFloat1Param";
            callBinding.Params = new[] { SerializableParam.FromFloat( 2.71f ), };
            callBinding.Init( source );
            await callBinding.Call();
            Assert.AreEqual( source.FloatValue, 2.71f );

#if UIBINDINGS_UNITASK_SUPPORT
            callBinding.Path   = "CallUniTaskMixed2Params";
            callBinding.Params = new[] { SerializableParam.FromInt( 123 ), SerializableParam.FromString( "World" ), };
            callBinding.Init( source );
            await callBinding.Call();
            Assert.AreEqual( source.IntValue, 123 );
            Assert.AreEqual( source.StringValue, "World" );
            Debug.Log( "Completed3" );
#endif
        }
    }

    public class CallMethodSource
    {
        public int   IntValue;
        public float FloatValue;
        public string StringValue;
        public Boolean BoolValue;

        public void CallNoParams()
        {
            IntValue += 1;
        }

        public void CallInt1Param( int value )
        {
            IntValue = value;
        }

        public void CallInt2Params( int value1, int value2 )
        {
            IntValue = value1 + value2;
        }

        public void CallString1Param( string value )
        {
            StringValue = value;
        }

        public void CallMixedParams( float value1, bool value2 )
        {
            FloatValue = value1;
            BoolValue = value2;
        }

        public Task CallTaskNoParams()
        {
            return Task.Delay( 1 ).ContinueWith( _ => IntValue += 1 );
        }

        public async Awaitable CallAwaitableFloat1Param( float value )
        {
            await Task.Delay( 1 );     //Awaitable delay need some editor pumping, it's not handy
            FloatValue = value;
        }

#if UIBINDINGS_UNITASK_SUPPORT
        public async UniTask CallUniTaskMixed2Params( int value, string strValue )
        {
            await UniTask.DelayFrame( 1 );
            IntValue = value;
            StringValue = strValue;
        }
#endif


    }
}