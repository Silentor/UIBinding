using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UIBindings.Tests.Runtime
{
    public class FunctionValueBindingTests
    {
        private int _targetValue;

        [Test]
        public  void TestSimplePath( )
        {
            _targetValue = 0;
            var testObject = new TestClass( ) { IntValue = 42 };
            var binding = new ValueBinding<int>( );
            binding.SourceChanged += ( sender, value ) => { _targetValue = value; };
            binding.Path  = nameof(TestClass.GetIntValue);
            binding.Init( testObject );
            Assert.That( binding.IsInited, Is.True );
            Assert.That( _targetValue, Is.EqualTo( 0 ) );

            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 0 ) );

            binding.Subscribe(  );
            Assert.That( _targetValue, Is.EqualTo( 0 ) );

            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 42 ) );

            testObject.IntValue = 43;
            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 43 ) );
        }

        [Test]
        public  void TestComplexPath( )
        {
            _targetValue = 0;
            var testObject = new TestClass( ) { IntValue = 42, Inner = new TestClass( ) { IntValue = 100 } };
            var binding = new ValueBinding<int>( );
            binding.SourceChanged += ( sender, value ) => { _targetValue = value; };
            binding.Path  = nameof(TestClass.GetInner) + "." + nameof(TestClass.GetIntValue);
            binding.Init( testObject );
            Assert.That( binding.IsInited, Is.True );
            Assert.That( _targetValue, Is.EqualTo( 0 ) );

            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 0 ) );

            binding.Subscribe(  );
            Assert.That( _targetValue, Is.EqualTo( 0 ) );

            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 100 ) );

            testObject.Inner.IntValue = 101;
            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 101 ) );
        }

        [Test]
        public  void TestSimplePath_SwapSource( )
        {
            _targetValue = 0;
            var testObject = new TestClass( ) { IntValue = 42 };
            var binding = new ValueBinding<int>( );
            binding.SourceChanged += ( sender, value ) => { _targetValue = value; };
            binding.Path  = nameof(TestClass.GetIntValue);
            binding.BindToType = true;
            binding.SourceType = typeof(TestClass).AssemblyQualifiedName;
            Assert.That( binding.IsInited, Is.False );
            binding.Init(  );
            Assert.That( binding.IsInited, Is.True );
            Assert.That( _targetValue, Is.EqualTo( 0 ) );

            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 0 ) );

            binding.Subscribe(  );
            Assert.That( _targetValue, Is.EqualTo( 0 ) );

            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 0 ) );

            binding.SourceObject = testObject;
            Assert.That( _targetValue, Is.EqualTo( 0 ) );

            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 42 ) );

            testObject.IntValue = 43;
            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 43 ) );

            var testObject2 = new TestClass( ) { IntValue = 1000 };
            binding.SourceObject = testObject2;
            Assert.That( _targetValue, Is.EqualTo( 43 ) );
            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 1000 ) );

            binding.SourceObject = null;
            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 0 ) );
        }

        [Test]
        public  void TestComplexPath_SwapSource( )
        {
            _targetValue = 0;
            var testObject = new TestClass( ) { IntValue = 42, Inner = new TestClass( ) { IntValue = 100 } };
            var binding = new ValueBinding<int>( );
            binding.SourceChanged += ( sender, value ) => { _targetValue = value; };
            binding.Path  = nameof(TestClass.GetInner) + "." + nameof(TestClass.GetIntValue);
            binding.BindToType = true;
            binding.SourceType = typeof(TestClass).AssemblyQualifiedName;
            Assert.That( binding.IsInited, Is.False );
            binding.Init(  );
            Assert.That( binding.IsInited, Is.True );
            Assert.That( _targetValue, Is.EqualTo( 0 ) );

            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 0 ) );

            binding.Subscribe(  );
            Assert.That( _targetValue, Is.EqualTo( 0 ) );

            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 0 ) );

            binding.SourceObject = testObject;
            Assert.That( _targetValue, Is.EqualTo( 0 ) );

            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 100 ) );

            testObject.Inner.IntValue = 101;
            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 101 ) );

            var testObject2 = new TestClass( ) { IntValue = 1000, Inner = new TestClass() { IntValue = 5000 } };
            binding.SourceObject = testObject2;
            Assert.That( _targetValue, Is.EqualTo( 101 ) );
            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 5000 ) );

            testObject2.Inner.IntValue = 6000;
            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 6000 ) );

            binding.SourceObject = null;
            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 0 ) );
        }

        [Test]
        public void TestNullSourceReturnsDefaultValue( )
        {
            _targetValue = 42;
            var binding = new ValueBinding<int>( );
            binding.SourceChanged += ( sender, value ) => { _targetValue = value; };
            binding.Path          =  nameof(TestClass.GetIntValue);
            binding.BindToType    =  true;
            binding.SourceType    =  typeof(TestClass).AssemblyQualifiedName;
            binding.Init(  );
            binding.Subscribe(  );
            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 0 ) );
        }

        [Test]
        public void TestNullIntermediateReturnsDefaultValue( )
        {
            _targetValue = 42;
            var testObject = new TestClass( ) { IntValue = 42, Inner = null };
            var binding    = new ValueBinding<int>( );
            binding.SourceChanged += ( sender, value ) => { _targetValue = value; };
            binding.Path          =  nameof(TestClass.GetInner) + "." + nameof(TestClass.GetIntValue);
            binding.Init( testObject );
            binding.Subscribe(  );
            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 0 ) );
        }

        [Test]
        public void TestSimplePathWriteValue( )
        {
            var testObject = new TestClass( ) { IntValue = 42 };
            var binding    = new ValueBinding<int>( );
            binding.Settings.Mode = DataBinding.EMode.TwoWay;
            binding.Path  = nameof(TestClass.GetIntValue);

            //No exception, but error message in log and OneWay mode
            LogAssert.Expect( LogType.Error, new Regex(".*Trying to create two-way binding.*") );
            binding.Init( testObject );
            Assert.That( binding.IsInited, Is.True );
            Assert.That( binding.IsTwoWay, Is.False );
        }

        [Test]
        public void TestComplexPathWriteValue_WhenFuncIsInnerAdapter( )
        {
            var testObject = new TestClass( ) { Inner = new TestClass() { IntValue = 100 } };
            var binding    = new ValueBinding<int>( );
            binding.Settings.Mode = DataBinding.EMode.TwoWay;
            binding.Path          = $"{nameof(TestClass.GetInner)}.{nameof(TestClass.IntValue)}";
            binding.Init( testObject );
            binding.Subscribe(  );

            //Just in case check the read
            int targetValue = 0;
            binding.SourceChanged += ( sender, value ) => { targetValue = value; };
            binding.ManuallyCheckChanges();
            Assert.That( targetValue, Is.EqualTo( 100 ) );

            binding.SetValue( 500 );

            // Should work as the function is inner adapter, so it don't actually write value, only provides access to inner object
            Assert.That( testObject.Inner.IntValue, Is.EqualTo( 500 ) );
        }

        public class TestClass
        {
            public int IntValue;
            public TestClass Inner;

            public int GetIntValue( ) => IntValue;
            public TestClass GetInner( ) => Inner;
        }
    }
}