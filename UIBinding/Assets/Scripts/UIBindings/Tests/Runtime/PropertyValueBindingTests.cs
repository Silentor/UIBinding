using System;
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
            binding.SourceChanged += ( _, value ) => { _targetValue = value; };
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
            binding.SourceChanged += ( _, value ) => { _targetValue = value; };
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
            binding.SourceChanged += ( _, value ) => { _targetValue = value; };
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
            binding.SourceChanged += ( _, value ) => { _targetValue = value; };
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
            binding.SourceChanged += ( _, value ) => { _targetValue = value; };
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
            binding.SourceChanged += ( _, value ) => { _targetValue = value; };
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
            binding.SourceChanged += ( _, value ) => { targetValue = value; };
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
            binding.SourceChanged += ( _, value ) => { _targetValue = value; };
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
            var testObject = new TestClass();
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
            var testObject = new TestClass();
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

        [Test]
        public void TestNotifyPropertyChangedSupport_Simple( )
        {
            _targetValue = 0;
            var testObject = new DerivedClassWithNotifySupport( 42 );
            var binding    = new ValueBinding<int>( );
            binding.SourceChanged += ( _, value ) => { _targetValue = value; };
            binding.Path          =  nameof(DerivedClassWithNotifySupport.IntValueControlledRead);
            binding.Init( testObject );
            binding.Subscribe(  );
            Assert.That( binding.IsInited, Is.True );
            Assert.That( _targetValue, Is.EqualTo( 0 ) );
            Assert.That( testObject.ReadsCount, Is.EqualTo( 0 ) );

            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 42 ) );
            Assert.That( testObject.ReadsCount, Is.EqualTo( 1 ) );

            // No change and no read counter increment without notify
            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 42 ) );
            Assert.That( testObject.ReadsCount, Is.EqualTo( 1 ) );

            testObject.IntValueControlledRead = 43;
            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 43 ) );
            Assert.That( testObject.ReadsCount, Is.EqualTo( 2 ) );
        }

        [Test]
        public void TestNotifyPropertyChangedSupport_Complex( )
        {
            _targetValue = 0;
            var inner = new DerivedClassWithNotifySupport( 42 );
            var testObject = new DerivedClassWithNotifySupport() { Inner = inner };
            var binding    = new ValueBinding<int>( );
            binding.SourceChanged += ( _, value ) => { _targetValue = value; };
            binding.Path          =  nameof(TestClass.Inner) + "." + nameof(DerivedClassWithNotifySupport.IntValueControlledRead);
            binding.Init( testObject );
            binding.Subscribe(  );
            Assert.That( binding.IsInited, Is.True );
            Assert.That( _targetValue, Is.EqualTo( 0 ) );
            Assert.That( testObject.ReadsCount, Is.EqualTo( 0 ) );
            Assert.That( inner.ReadsCount, Is.EqualTo( 0 ) );

            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 42 ) );
            Assert.That( inner.ReadsCount, Is.EqualTo( 1 ) );

            // No change and no read counter increment without notify
            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 42 ) );
            Assert.That( inner.ReadsCount, Is.EqualTo( 1 ) );

            inner.IntValueControlledRead = 43;
            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 43 ) );
            Assert.That( inner.ReadsCount, Is.EqualTo( 2 ) );
        }

        [Test]
        public void TestPollAndNotifyInPath_IsStillPoll( )
        {
            _targetValue = 0;
            var inner = new DerivedClassWithNotifySupport( 42 );
            var testObject = new TestClass() { Inner = inner };  //Poll type in Path breaks the notify chain
            var binding    = new ValueBinding<int>( );
            binding.SourceChanged += ( _, value ) => { _targetValue = value; };
            binding.Path          =  nameof(TestClass.Inner) + "." + nameof(DerivedClassWithNotifySupport.IntValueControlledRead);
            binding.Init( testObject );
            binding.Subscribe(  );
            Assert.That( binding.IsInited, Is.True );
            Assert.That( _targetValue, Is.EqualTo( 0 ) );
            Assert.That( testObject.ReadsCount, Is.EqualTo( 0 ) );
            Assert.That( inner.ReadsCount, Is.EqualTo( 0 ) );

            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 42 ) );
            Assert.That( inner.ReadsCount, Is.EqualTo( 1 ) );

            // Read counter is incremented on each check because notify chain is broken by poll type
            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 42 ) );
            Assert.That( inner.ReadsCount, Is.EqualTo( 2 ) );

            inner.IntValueControlledRead = 43;
            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 43 ) );
            Assert.That( inner.ReadsCount, Is.EqualTo( 3 ) );
        }

        [Test]
        public void TestChangeSourceFromPollToNotify( )
        {
            _targetValue = 0;
            var testObject = new TestClass( 42);
            var binding    = new ValueBinding<int>( );
            binding.SourceChanged += ( _, value ) => { _targetValue = value; };
            binding.Path          =  nameof(TestClass.IntValueControlledRead);
            binding.Init( testObject );
            binding.Subscribe(  );
            Assert.That( binding.IsInited, Is.True );
            Assert.That( _targetValue, Is.EqualTo( 0 ) );

            // Test poll mode, read count should increase on each check
            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 42 ) );
            Assert.That( testObject.ReadsCount, Is.EqualTo( 1 ) );

            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 42 ) );
            Assert.That( testObject.ReadsCount, Is.EqualTo( 2 ) );

            testObject.IntValueControlledRead = 43;
            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 43 ) );
            Assert.That( testObject.ReadsCount, Is.EqualTo( 3 ) );

            //Change source to notify support, read count should not increase until change
            var testObject2 = new DerivedClassWithNotifySupport( 1000 );
            binding.SourceObject = testObject2;
            Assert.That( _targetValue, Is.EqualTo( 43 ) );

            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 1000 ) );
            Assert.That( testObject2.ReadsCount, Is.EqualTo( 1 ) );

            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 1000 ) );
            Assert.That( testObject2.ReadsCount, Is.EqualTo( 1 ) );

            testObject2.IntValueControlledRead = 1001;
            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 1001 ) );
            Assert.That( testObject2.ReadsCount, Is.EqualTo( 2 ) );

            // Change back to poll mode, read count should increase on each check
            binding.SourceObject = testObject;
            Assert.That( _targetValue, Is.EqualTo( 1001 ) );

            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 43 ) );
            Assert.That( testObject.ReadsCount, Is.EqualTo( 4 ) );

            binding.ManuallyCheckChanges();
            Assert.That( _targetValue, Is.EqualTo( 43 ) );
            Assert.That( testObject.ReadsCount, Is.EqualTo( 5 ) );
        }

        public class TestClass
        {
            public int IntValue { get; set; }

            public TestClass Inner { get; set; }

            public int ReadOnlyIntValue => 123;

            public virtual int IntValueControlledRead
            {
                get
                {
                    ReadsCount++;
                    return _intValueControlledRead;
                }

                set => _intValueControlledRead = value;
            }

            public  int ReadsCount;
            protected int _intValueControlledRead;

            public TestClass( )
            {
            }

            public TestClass(int intValueControlledRead )
            {
                _intValueControlledRead = intValueControlledRead;
            }
        }

        public class DerivedClassWithNotifySupport : TestClass, INotifyPropertyChanged
        {
            public override int IntValueControlledRead
            {
                get => base.IntValueControlledRead;
                set
                {
                    if ( base._intValueControlledRead != value )
                    {
                        base.IntValueControlledRead = value;
                        PropertyChanged?.Invoke( this, nameof(IntValueControlledRead) );
                    }
                }
            }

            public DerivedClassWithNotifySupport( ) : base()
            {
            }

            public DerivedClassWithNotifySupport(int intValueControlledRead ) : base(intValueControlledRead)
            {
            }

            public event Action<object, string> PropertyChanged;
        }
    }
}
