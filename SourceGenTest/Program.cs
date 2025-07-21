
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using MyNamespace.AnotherNS;
using UIBindings;

namespace MyNamespace
{
    public static class Runner
    {
        static void Main( )
        {
            Console.WriteLine( "finish" );
            var test = new ExternalClass<int>.TestClassDeriv();
            test.PropertyChanged += (sender, propertyName) =>
            {
                Console.WriteLine($"Property {propertyName} changed on {sender} from event");
            };
            test.CamelNoUnderscore = "test";

        }
    }

    public partial class TestClass
    {
        [ObservableProperty]
        private int _camelField;

        [ObservableProperty]
        private System.String camelNoUnderscore, m_theMPrefixField = "bla";

        [ObservableProperty]
        private CustomType _customTypeProp;
    }

    namespace AnotherNS
    {
        public partial class ExternalClass<T>
        {

            public partial class TestClassDeriv : TestClass
            {
                [ObservableProperty]
                private float _observableFloat;
            }
        }

    }

    public class CustomType
    {
        public int a;
    }
}