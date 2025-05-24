using System;

namespace UIBindings
{
    public class StringValidator : ConverterOneWayBase<String, bool>
    {
        public override bool Convert( String value )
        {
            return !String.IsNullOrEmpty( value );
        }
    }
}