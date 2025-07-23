using System;
using UIBindings;
using UIBindings.SourceGen;

namespace MyNamespace.NS2
{
    public class ExternalClass<T>
    {
        public class TestClass : ObservableObject
        {
            [property: Obsolete("Use Obs2 instead", false)]
            //[NotifyPropertyChangedFor(nameof(Obs2))]
            [NotifyPropertyChangedFor("Obs2")]
            [ObservableProperty]
            private int _observableField;

            [ObservableProperty]
            private System.String _obs2, _obs3;
        }
    }
}