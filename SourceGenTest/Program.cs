using System.Text.Json.Serialization;
using MyNamespace.AnotherNS;
using UIBindings;
using UIBindings.SourceGen;

namespace MyNamespace
{
    public static class Runner
    {
        static void Main( )
        {
            var test = new ExternalClass<int>.TestClassDeriv();
            test.PropertyChanged += (sender, propertyName) =>
            {
                Console.WriteLine($"Property {propertyName} changed on {sender} from event");
            };
            test.CamelNoUnderscore = "test";

        }
    }

    public partial class TestClass : ObservableObject
    {
        [property: JsonRequired]
        [NotifyPropertyChangedFor(nameof(CustomTypeProp))]
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
                private float  _observableFloat;
            }
        }

    }

    public class CustomType
    {
        public int a;
    }
}