using NUnit.Framework;

namespace UIBindings.Tests.Runtime
{
    public class FieldAdapterTests
    { 
        [Test]
        public void TestInitForReadonlyField()
        {
            var roFieldInfo = typeof(TestClass).GetField( nameof(TestClass.ReadonlyStringValue) );
            Assert.IsNotNull( roFieldInfo );
            var declaredTwoWay = true;
            var fieldAdapter = new Adapters.FieldAdapter<TestClass, string>( roFieldInfo, typeof(TestClass), declaredTwoWay, null );
            Assert.That( fieldAdapter.IsTwoWay, Is.False );
        }

        [Test]
        public void TestInitForWritableField()
        {
            var fieldInfo = typeof(TestClass).GetField( nameof(TestClass.IntValue) );
            Assert.IsNotNull( fieldInfo );
            var declaredTwoWay = true;
            var fieldAdapter = new Adapters.FieldAdapter<TestClass, int>( fieldInfo, typeof(TestClass), declaredTwoWay, null );
            Assert.That( fieldAdapter.IsTwoWay, Is.True );

            declaredTwoWay = false;
            fieldAdapter = new Adapters.FieldAdapter<TestClass, int>( fieldInfo, typeof(TestClass), declaredTwoWay, null );
            Assert.That( fieldAdapter.IsTwoWay, Is.False );
        }

        public class TestClass
        {
            public int IntValue;
            public readonly string ReadonlyStringValue = "readonly";
            public TestClass Inner;
        }
    }
}