using NUnit.Framework;

namespace UIBindings.Tests.Runtime
{
    public class ChangeSourceCallBindingTests
    {
        private string _testStringValue;
        private int    _testIntValue;

        [Test]
        public void BindingByTypeTest_SimplePath( )
        {
            var testBindingByType = new CallBinding()
                                 {
                                         BindToType = true,
                                         SourceType = typeof(VolatileSourceObject).AssemblyQualifiedName,
                                         Path       = nameof(VolatileSourceObject.Call)
                                 };

            //Init without actual source object
            testBindingByType.Init(  );
            Assert.That( testBindingByType.IsInited, Is.True );
            testBindingByType.Call();
            // Nothing happens because source object is not set

            var sourceObject = new VolatileSourceObject(){ValueInt = 1};
            testBindingByType.SourceObject = sourceObject;
            testBindingByType.Call();
            Assert.That( sourceObject.ValueInt, Is.EqualTo( 2 ) ); // Because call method increments ValueInt

            var sObject2 = new VolatileSourceObject(){ValueInt = 10};
            testBindingByType.SourceObject = sObject2;
            testBindingByType.Call();
            Assert.That( sObject2.ValueInt, Is.EqualTo( 11 ) );
            Assert.That( sourceObject.ValueInt, Is.EqualTo( 2 ) ); // No changes to first object

            testBindingByType.SourceObject = null;
            testBindingByType.Call();
            // Nothing happens because source object is null
            Assert.That( sObject2.ValueInt, Is.EqualTo( 11 ) );
            Assert.That( sourceObject.ValueInt, Is.EqualTo( 2 ) ); // No changes to first object
        }

        [Test]
        public void BindingByTypeTest_ComplexPath( )
        {
            var testBindingByType = new CallBinding()
                                 {
                                         BindToType = true,
                                         SourceType = typeof(VolatileSourceObject).AssemblyQualifiedName,
                                         Path       = "Inner.Call"
                                 };

            //Init without actual source object
            testBindingByType.Init(  );
            Assert.That( testBindingByType.IsInited, Is.True );
            testBindingByType.Call();
            // Nothing happens because source object is not set

            var sourceObject = new VolatileSourceObject(){Inner = new VolatileSourceObject(){ValueInt = 1}};
            testBindingByType.SourceObject = sourceObject;
            testBindingByType.Call();
            Assert.That( sourceObject.Inner.ValueInt, Is.EqualTo( 2 ) ); // Because call method increments ValueInt

            var sObject2 = new VolatileSourceObject(){Inner = new VolatileSourceObject(){ValueInt = 10}};
            testBindingByType.SourceObject = sObject2;
            testBindingByType.Call();
            Assert.That( sObject2.Inner.ValueInt, Is.EqualTo( 11 ) );
            Assert.That( sourceObject.Inner.ValueInt, Is.EqualTo( 2 ) ); // No changes to first object

            testBindingByType.SourceObject = null;
            testBindingByType.Call();
            // Nothing happens because source object is null
            Assert.That( sObject2.Inner.ValueInt, Is.EqualTo( 11 ) );
            Assert.That( sourceObject.Inner.ValueInt, Is.EqualTo( 2 ) ); // No changes to first object
        }

        public class VolatileSourceObject
        {
            public string ValueString { get; set; }
            public int    ValueInt    { get; set; }

            public VolatileSourceObject Inner { get; set; }

            public void Call( )
            {
                ValueInt += 1;
            }
        }
    }
}