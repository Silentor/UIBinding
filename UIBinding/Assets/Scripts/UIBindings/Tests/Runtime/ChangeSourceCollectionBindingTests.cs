using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace UIBindings.Tests.Runtime
{
    public class ChangeSourceCollectionBindingTests
    {
        private List<string> _testStringsValue;
        private List<int> _testIntsValue;

        [Test]
        public void BindingByTypeTest_SimplePath( )
        {
            var testBindingByType = new CollectionBinding()
                                 {
                                         BindToType = true,
                                         SourceType = typeof(VolatileSourceObject).AssemblyQualifiedName,
                                         Path       = "ValuesString"
                                 };

            //Init without actual source object
            testBindingByType.Init(  );
            testBindingByType.CollectionChanged += (o, s) => _testStringsValue = s.Cast<string>().ToList();
            testBindingByType.ItemChanged += (sender, i, o) => { _testStringsValue[i] = o as string; };
            Assert.That( testBindingByType.IsInited, Is.True );
            testBindingByType.ManuallyCheckChanges();
            Assert.That( _testStringsValue, Is.Null ); //Because we not subscribed yet

            testBindingByType.SourceObject = new VolatileSourceObject(){ValuesString = new List<string>(){"test1", "test2"}};
            Assert.That( _testStringsValue, Is.Null ); //Because we need to manually check changes

            testBindingByType.ManuallyCheckChanges();
            Assert.That( _testStringsValue, Is.Null ); //Because we didn't subscribed yet

            testBindingByType.Subscribe(  );
            testBindingByType.ManuallyCheckChanges();
            Assert.That( _testStringsValue, Is.EquivalentTo( new []{"test1", "test2"} ) );

            testBindingByType.SourceObject = new VolatileSourceObject(){ValuesString = new List<string>(){"test3", "test4"}};
            testBindingByType.ManuallyCheckChanges();
            Assert.That( _testStringsValue, Is.EquivalentTo( new []{"test3", "test4"} ) );

            testBindingByType.SourceObject = new VolatileSourceObject(){ValuesString = new List<string>(){"test5", "test6"}};
            testBindingByType.Unsubscribe();
            testBindingByType.ManuallyCheckChanges();
            Assert.That( _testStringsValue, Is.EquivalentTo(  new []{"test3", "test4"} ) ); //No changes because we unsubscribed

            testBindingByType.Subscribe(  );
            testBindingByType.SourceObject = null;
            testBindingByType.ManuallyCheckChanges();
            Assert.That( _testStringsValue, Is.Empty ); //Because source object is null, so collection is empty
        }

        [Test]
        public void BindingByTypeTest_ComplexPath( )
        {
            var testBindingByType = new CollectionBinding()
                                 {
                                         BindToType = true,
                                         SourceType = typeof(VolatileSourceObject).AssemblyQualifiedName,
                                         Path       = "Inner.ValuesInt"
                                 };

            //Init without actual source object
            testBindingByType.Init(  );
            testBindingByType.CollectionChanged += (o, i) => _testIntsValue = i.Cast<int>().ToList();
            testBindingByType.ItemChanged += (sender, i, o) => { _testIntsValue[i] = (int)o; };
            Assert.That( testBindingByType.IsInited, Is.True );
            testBindingByType.ManuallyCheckChanges();
            Assert.That( _testIntsValue, Is.Null ); //Field is not inited because we not subscribed yet

            testBindingByType.SourceObject = new VolatileSourceObject(){Inner = new VolatileSourceObject(){ValuesInt = new List<int> {1, 2}}};
            Assert.That( _testIntsValue, Is.Null ); //Because we need to manually check changes

            testBindingByType.ManuallyCheckChanges();
            Assert.That( _testIntsValue, Is.Null ); //Because we didn't subscribed yet
            testBindingByType.Subscribe(  );

            testBindingByType.ManuallyCheckChanges();
            Assert.That( _testIntsValue, Is.EquivalentTo( new[]{1, 2} ) );
             
            testBindingByType.SourceObject = new VolatileSourceObject(){Inner = new VolatileSourceObject(){ValuesInt = new List<int> {3, 4}}};
            testBindingByType.ManuallyCheckChanges();
            Assert.That( _testIntsValue, Is.EquivalentTo( new[]{3, 4} ) );

            ((VolatileSourceObject)testBindingByType.SourceObject).Inner.ValuesInt = new List<int>(){5, 6};
            testBindingByType.ManuallyCheckChanges();
            Assert.That( _testIntsValue, Is.EquivalentTo( new []{5, 6} ) );
        }

        public class VolatileSourceObject
        {
            public List<string> ValuesString { get; set; }
            public List<int> ValuesInt { get; set; }

            public VolatileSourceObject Inner { get; set; }
        }
    }
}