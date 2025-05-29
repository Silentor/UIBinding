using System;

namespace UIBindings
{
    public class StringValidator : SimpleConverterOneWayBase<String, bool>
    {
        public override bool Convert( String value )
        {
            return !String.IsNullOrEmpty( value );
        }
    }
}