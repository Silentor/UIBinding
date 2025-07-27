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
            // test.PropertyChanged += (sender, propertyName) =>
            // {
            //     Console.WriteLine($"Property {propertyName} changed on {sender} from event");
            // };
            //test.CamelNoUnderscore = "test";
            //test.CamelField = 1;
            //test.ObservableFloat = 42.0f;

        }
    }

    [INotifyPropertyChanged]
    public partial class TestClass<T>  where T : new() 
    {
        //[property: Obsolete("This field is obsolete", false)]
        //[NotifyPropertyChangedFor(nameof(CustomTypeProp))]
        //[ObservableProperty]
        private int _camelField;

        //[property: JsonRequired]
        //[ObservableProperty]
        private System.String camelNoUnderscore, m_theMPrefixField = "bla";

        //[ObservableProperty]
        private CustomType _customTypeProp;
    }

    namespace AnotherNS
    {
        //[INotifyPropertyChanged]
        public partial class ExternalClass<T> where T : struct
        {
            //[INotifyPropertyChanged ]
            public partial class TestClassDeriv : TestClass<T>
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