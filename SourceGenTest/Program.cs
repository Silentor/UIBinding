
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using UIBindings;

namespace MyNamespace
{
    public static class Runner
    {
        static void Main( )
        {
            Console.WriteLine( "finish" );
            var test = new TestClass();
            test.PropertyChanged += (sender, propertyName) =>
            {
                Console.WriteLine($"Property {propertyName} changed on {sender} from event");
            };
            test.Obs3 = "test";

        }
    }

    public partial class TestClass
    {
        [ObservableProperty]
        private int _observableField;

        [ObservableProperty]
        private System.String _obs2, _obs3 = "bla";

        [ObservableProperty]
        private CustomType _customTypeProp;

        partial void OnObs3Changed(String oldValue, [CallerMemberName]String newValue )
        {
            Console.WriteLine( $"changed from {oldValue} to {newValue}" );
        }

        
    }

    public class CustomType
    {
        public int a;
    }
}