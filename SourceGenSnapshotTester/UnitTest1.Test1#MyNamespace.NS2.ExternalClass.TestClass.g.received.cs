//HintName: MyNamespace.NS2.ExternalClass.TestClass.g.cs
namespace MyNamespace.NS2
{
    partial class ExternalClass<T>
    {
        [System.CodeDom.Compiler.GeneratedCode("UIBindings.SourceGen", "1.0.0.0")]
        partial class TestClass : UIBindings.INotifyPropertyChanged
        {
            public int ObservableField
            {
                get => _observableField;
                set
                {
                    if (!EqualityComparer<int>.Default.Equals(_observableField, value))
                    {
                        var oldValue = _observableField;
                        OnObservableFieldChanging( oldValue, value );
                        _observableField = value;
                        OnObservableFieldChanged( oldValue, value );
                        OnPropertyChanged( );
                    }
                }
            }
            partial void OnObservableFieldChanging( int oldValue, int newValue );
            partial void OnObservableFieldChanged( int oldValue, int newValue );
            
            public string Obs2
            {
                get => _obs2;
                set
                {
                    if (!EqualityComparer<string>.Default.Equals(_obs2, value))
                    {
                        var oldValue = _obs2;
                        OnObs2Changing( oldValue, value );
                        _obs2 = value;
                        OnObs2Changed( oldValue, value );
                        OnPropertyChanged( );
                    }
                }
            }
            partial void OnObs2Changing( string oldValue, string newValue );
            partial void OnObs2Changed( string oldValue, string newValue );
            
            public string Obs3
            {
                get => _obs3;
                set
                {
                    if (!EqualityComparer<string>.Default.Equals(_obs3, value))
                    {
                        var oldValue = _obs3;
                        OnObs3Changing( oldValue, value );
                        _obs3 = value;
                        OnObs3Changed( oldValue, value );
                        OnPropertyChanged( );
                    }
                }
            }
            partial void OnObs3Changing( string oldValue, string newValue );
            partial void OnObs3Changed( string oldValue, string newValue );
            
        public event Action<System.Object, System.String> PropertyChanged;
        
        protected virtual void OnPropertyChanged( [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null )
        {
            if ( PropertyChanged != null )
            {
                PropertyChanged.Invoke(this, propertyName );
            }
        }
        }
    }
}
