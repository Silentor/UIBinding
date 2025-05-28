namespace UIBindings
{
    public  interface IDataReadWriter<TOutput> : IDataReader<TOutput>
    {
        /// <summary>
        /// Set value to the source
        /// </summary>
        /// <param name="value"></param>
        void SetValue( TOutput value );
    }
}