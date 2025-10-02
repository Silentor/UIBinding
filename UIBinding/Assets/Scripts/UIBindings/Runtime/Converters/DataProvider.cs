using System;

namespace UIBindings
{
    /// <summary>
    /// Item of data binding pipeline. Always has one input <see cref="InputType"/> and possible has many outputs. Always implements at least one <see cref="IDataReader{TOutput}"/>
    /// </summary>
    public abstract class DataProvider
    {
        /// <summary>
        /// Would it be process data in both directions. If true, it must implement al least one <see cref="IDataReadWriter{TOutput}"/>
        /// </summary>
        public abstract bool IsTwoWay { get; }

        /// <summary>
        /// Type of input data that this provider can accept
        /// </summary>
        public abstract Type InputType { get; }

        public override String ToString( )
        {
            return $"{GetType().Name} (Input: {InputType.Name}, IsTwoWay: {IsTwoWay})";
        }
    }
}