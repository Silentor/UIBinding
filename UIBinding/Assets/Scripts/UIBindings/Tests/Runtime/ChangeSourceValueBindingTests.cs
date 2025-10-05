using NUnit.Framework;

namespace UIBindings.Tests.Runtime
{
    public class ChangeSourceValueBindingTests
    {
        private string _testStringValue;
        private int    _testIntValue;

        [Test]
        public void BindingByTypeTest_SimplePath( )
        {
            var testBindingByType = new ValueBinding<string>()
                                 {
                                         BindToType = true,
                                         SourceType = typeof(VolatileSourceObject).AssemblyQualifiedName,
                                         Path       = "ValueString"
                                 };

            //Init without actual source object
            testBindingByType.Init(  );
            testBindingByType.SourceChanged += (o, s) => _testStringValue = s;
            Assert.That( testBindingByType.IsInited, Is.True );
            testBindingByType.ManuallyCheckChanges();
            Assert.That( _testStringValue, Is.Null ); //Because source object is null

            testBindingByType.SourceObject = new VolatileSourceObject(){ValueString = "test1"};
            Assert.That( _testStringValue, Is.Null ); //Because we need to manually check changes

            testBindingByType.ManuallyCheckChanges();
            Assert.That( _testStringValue, Is.Null ); //Because we didn't subscribed yet

            testBindingByType.Subscribe(  );
            testBindingByType.ManuallyCheckChanges();
            Assert.That( _testStringValue, Is.EqualTo( "test1" ) );

            testBindingByType.SourceObject = new VolatileSourceObject(){ValueString = "test2"};
            testBindingByType.ManuallyCheckChanges();
            Assert.That( _testStringValue, Is.EqualTo( "test2" ) );

            testBindingByType.SourceObject = new VolatileSourceObject(){ValueString = "test3"};
            testBindingByType.Unsubscribe();
            testBindingByType.ManuallyCheckChanges();
            Assert.That( _testStringValue, Is.EqualTo( "test2" ) ); //No changes because we unsubscribed

            testBindingByType.Subscribe(  );
            testBindingByType.SourceObject = null;
            testBindingByType.ManuallyCheckChanges();
            Assert.That( _testStringValue, Is.Null ); //Because source object is null
        }

        [Test]
        public void BindingByTypeTest_ComplexPath( )
        {
            var testBindingByType = new ValueBinding<int>()
                                 {
                                         BindToType = true,
                                         SourceType = typeof(VolatileSourceObject).AssemblyQualifiedName,
                                         Path       = "Inner.ValueInt"
                                 };

            //Init without actual source object
            testBindingByType.Init(  );
            testBindingByType.SourceChanged += (o, i) => _testIntValue = i;
            Assert.That( testBindingByType.IsInited, Is.True );
            testBindingByType.ManuallyCheckChanges();
            Assert.That( _testIntValue, Is.EqualTo( 0 ) ); //Because source object is null

            testBindingByType.SourceObject = new VolatileSourceObject(){Inner = new VolatileSourceObject(){ValueInt = 1}};
            Assert.That( _testIntValue, Is.EqualTo( 0 ) ); //Because we need to manually check changes

            testBindingByType.ManuallyCheckChanges();
            Assert.That( _testIntValue, Is.EqualTo( 0 ) ); //Because we didn't subscribed yet
            testBindingByType.Subscribe(  );

            testBindingByType.ManuallyCheckChanges();
            Assert.That( _testIntValue, Is.EqualTo( 1 ) );
             
            testBindingByType.SourceObject = new VolatileSourceObject(){Inner = new VolatileSourceObject(){ValueInt = 2}};
            testBindingByType.ManuallyCheckChanges();
            Assert.That( _testIntValue, Is.EqualTo( 2 ) );

            ((VolatileSourceObject)testBindingByType.SourceObject).Inner.ValueInt = 3;
            testBindingByType.ManuallyCheckChanges();
            Assert.That( _testIntValue, Is.EqualTo( 3 ) );
        }

        public class VolatileSourceObject
        {
            public string ValueString { get; set; }
            public int    ValueInt    { get; set; }

            public VolatileSourceObject Inner { get; set; }
        }
    }
}