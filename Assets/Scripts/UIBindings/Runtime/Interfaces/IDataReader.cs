namespace UIBindings
{
    public interface IDataReader<TOutput>
    {
        /// <summary>
        /// Try to get _new_ value from the source.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        bool TryGetValue( out TOutput value );
    }
}