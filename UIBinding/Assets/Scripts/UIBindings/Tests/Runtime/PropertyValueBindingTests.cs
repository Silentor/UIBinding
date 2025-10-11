using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UIBindings.Tests.Runtime
{
    public class PropertyValueBindingTests
    {
        private int _targetValue;

        [Test]
        public  void TestSimplePath( )
        {
            _targetValue = 0;
            var testObject = new TestClass( ) { IntValue = 42 };
            var binding = new ValueBinding<int>( );
            binding.SourceChanged += ( sender, value ) => { _targetValue = value; };
            binding.Path  = nameof(TestClass.IntValue);
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
            binding.Path  = nameof(TestClass.Inner) + "." + nameof(TestClass.IntValue);
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
            binding.Path  = nameof(TestClass.IntValue);
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
            binding.Path  = nameof(TestClass.Inner) + "." + nameof(TestClass.IntValue);
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
            binding.Path          =  nameof(TestClass.IntValue);
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
            var binding = new ValueBinding<int>( );
            binding.SourceChanged += ( sender, value ) => { _targetValue = value; };
            binding.Path  = nameof(TestClass.Inner) + "." + nameof(TestClass.IntValue);
            binding.Init( testObject );
            binding.Subscribe(  );
            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 0 ) );
        }

        [Test]
        public void TestSimplePathWriteValue( )
        {
            var testObject = new TestClass( ) { IntValue = 42 };
            var binding = new ValueBinding<int>( );
            binding.Settings.Mode  = DataBinding.EMode.TwoWay;
            binding.Path  = nameof(TestClass.IntValue);
            binding.Init( testObject );
            Assert.That( binding.IsInited, Is.True );
            Assert.That( binding.IsTwoWay, Is.True );
            binding.Subscribe(  );
 
            Assert.That( testObject.IntValue, Is.EqualTo( 42 ) );

            binding.SetValue( 100 );
            Assert.That( testObject.IntValue, Is.EqualTo( 100 ) );

            binding.SetValue( 200 );
            Assert.That( testObject.IntValue, Is.EqualTo( 200 ) );
        }

        [Test]
        public void TestComplexPathWriteValue( )
        {
            var testObject = new TestClass( ) { IntValue = 42, Inner = new TestClass( ) { IntValue = 100 } };
            var binding = new ValueBinding<int>( );
            binding.Settings.Mode  = DataBinding.EMode.TwoWay;
            binding.Path  = nameof(TestClass.Inner) + "." + nameof(TestClass.IntValue);
            binding.Init( testObject );
            Assert.That( binding.IsInited, Is.True );
            Assert.That( binding.IsTwoWay, Is.True );
            binding.Subscribe(  );

            Assert.That( testObject.Inner.IntValue, Is.EqualTo( 100 ) );

            binding.SetValue( 1000 );
            Assert.That( testObject.Inner.IntValue, Is.EqualTo( 1000 ) );

            binding.SetValue( 2000 );
            Assert.That( testObject.Inner.IntValue, Is.EqualTo( 2000 ) );
        }

        [Test]
        public void TestSimplePathWriteToNullSource( )
        {
            var binding = new ValueBinding<int>( );
            binding.Settings.Mode  = DataBinding.EMode.TwoWay;
            binding.Path  = nameof(TestClass.IntValue);
            binding.BindToType = true;
            binding.SourceType = typeof(TestClass).AssemblyQualifiedName;
            binding.Init(  );
            Assert.That( binding.IsInited, Is.True );
            Assert.That( binding.IsTwoWay, Is.True );
            binding.Subscribe(  );
            //No source - no exception
            binding.SetValue( 100 );
        }

        [Test]
        public void TestSetSourceBeforeInit( )
        {
            var testObject = new TestClass( ) { IntValue = 42 };
            var binding    = new ValueBinding<int>( );
            var targetValue = 0;
            binding.SourceChanged += ( sender, value ) => { targetValue = value; };
            binding.Path  = nameof(TestClass.IntValue);
            binding.SourceObject = testObject;
            binding.Init(  );
            Assert.That( binding.IsInited, Is.True );
            binding.Subscribe(  );
            binding.ManuallyCheckChanges();
            Assert.That( targetValue, Is.EqualTo( 42 ) );
        }

        [Test]
        public void TestOneTimeBinding( )
        {
            _targetValue = 0;
            var testObject = new TestClass( ) { IntValue = 42 };
            var binding    = new ValueBinding<int>( );
            binding.Settings.Mode =  DataBinding.EMode.OneTime;
            binding.SourceChanged += ( sender, value ) => { _targetValue = value; };
            binding.Path          =  nameof(TestClass.IntValue);
            binding.Init( testObject );
            binding.Subscribe(  );

            //First time it works
            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 42 ) );

            testObject.IntValue = 100;
            binding.ManuallyCheckChanges();
            //No change after that
            Assert.That( _targetValue, Is.EqualTo( 42 ) );

            // After re-subscribing it works again
            binding.Unsubscribe();
            binding.Subscribe(  );
            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 100 ) );
        }

        [Test]
        public void TestTwoWayBindingOnReadOnlyProperty_Simple( )
        {
            var testObject = new TestClass( ) { };
            var binding    = new ValueBinding<int>( );
            binding.Settings.Mode  = DataBinding.EMode.TwoWay;
            binding.Path  = nameof(TestClass.ReadOnlyIntValue);

            //Will be error messages in log, but no exception
            LogAssert.Expect( LogType.Error, new Regex(".*Trying to create two-way binding.*") );
            binding.Init( testObject );
            Assert.That( binding.IsInited, Is.True );
            Assert.That( binding.IsTwoWay, Is.False );
            binding.Subscribe(  );

            //Read its ok
            binding.ManuallyCheckChanges();
            Assert.That( testObject.ReadOnlyIntValue, Is.EqualTo( 123 ) );

            //Write no ok, but no exception
            LogAssert.Expect( LogType.Error, new Regex(".*Trying to set value to one-way binding.*") );
            binding.SetValue( 100 );
        }

        [Test]
        public void TestTwoWayBindingOnReadOnlyProperty_Complex( )
        {
            var testObject = new TestClass( ) { Inner = new TestClass()  };
            var binding    = new ValueBinding<int>( );
            binding.Settings.Mode = DataBinding.EMode.TwoWay;
            binding.Path          = $"{nameof(TestClass.Inner)}.{nameof(TestClass.ReadOnlyIntValue)}";

            //Will be error messages in log, but no exception
            LogAssert.Expect( LogType.Error, new Regex(".*Trying to create two-way binding.*") );
            binding.Init( testObject );
            Assert.That( binding.IsInited, Is.True );
            Assert.That( binding.IsTwoWay, Is.False );
            binding.Subscribe(  );

            //Read its ok
            binding.ManuallyCheckChanges();
            Assert.That( testObject.Inner.ReadOnlyIntValue, Is.EqualTo( 123 ) );

            //Write no ok, but no exception
            LogAssert.Expect( LogType.Error, new Regex(".*Trying to set value to one-way binding.*") );
            binding.SetValue( 100 );
        }

        [Test]
        public void TestOneWayBindingWriteAttempt_Simple( )
        {
            var testObject = new TestClass( ) { };
            var binding    = new ValueBinding<int>( );
            binding.Settings.Mode = DataBinding.EMode.OneWay;
            binding.Path          = nameof(TestClass.IntValue);

            //Will be error messages in log, but no exception
            binding.Init( testObject );
            Assert.That( binding.IsInited, Is.True );
            Assert.That( binding.IsTwoWay, Is.False );
            binding.Subscribe(  );

            //Read its ok
            binding.ManuallyCheckChanges();
            Assert.That( testObject.ReadOnlyIntValue, Is.EqualTo( 123 ) );

            //Write no ok, but no exception
            LogAssert.Expect( LogType.Error, new Regex(".*Trying to set value to one-way binding.*") );
            binding.SetValue( 100 );
        }

        [Test]
        public void TestOneWayBindingWriteAttempt_Complex( )
        {
            var testObject = new TestClass( ) { Inner = new TestClass()  };
            var binding    = new ValueBinding<int>( );
            binding.Settings.Mode = DataBinding.EMode.OneWay;
            binding.Path          = $"{nameof(TestClass.Inner)}.{nameof(TestClass.IntValue)}";

            //Will be error messages in log, but no exception
            binding.Init( testObject );
            Assert.That( binding.IsInited, Is.True );
            Assert.That( binding.IsTwoWay, Is.False );
            binding.Subscribe(  );

            //Read its ok
            binding.ManuallyCheckChanges();
            Assert.That( testObject.ReadOnlyIntValue, Is.EqualTo( 123 ) );

            //Write no ok, but no exception
            LogAssert.Expect( LogType.Error, new Regex(".*Trying to set value to one-way binding.*") );
            binding.SetValue( 100 );
        }

        public class TestClass
        {
            public int IntValue { get; set; }

            public TestClass Inner { get; set; }

            public int ReadOnlyIntValue => 123;
        }
    }
}