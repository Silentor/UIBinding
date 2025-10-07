

using NUnit.Framework;

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
            var binding    = new ValueBindingRW<int>( );
            binding.Path  = nameof(TestClass.GetIntValue);
            binding.Init( testObject );
            Assert.That( binding.IsInited, Is.True );
            Assert.That( binding.IsTwoWay, Is.False );  // Because func adapter is one-way only
            binding.Subscribe(  );
 
            Assert.That( testObject.IntValue, Is.EqualTo( 42 ) );

            binding.SetValue( 100 );
            Assert.That( testObject.IntValue, Is.EqualTo( 100 ) );

            binding.SetValue( 200 );
            Assert.That( testObject.IntValue, Is.EqualTo( 200 ) );
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