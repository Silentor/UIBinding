using System;

namespace UIBindings
{
    public class GuidValidator : ConverterOneWayBase<Guid, bool>
    {
        public override bool Convert( Guid value )
        {
            return value != Guid.Empty;
        }
    }
}