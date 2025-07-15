using System;

namespace UIBindings
{
    public class GuidValidator : SimpleConverterOneWayBase<Guid, bool>
    {
        public override bool Convert( Guid value )
        {
            return value != Guid.Empty;
        }
    }
}