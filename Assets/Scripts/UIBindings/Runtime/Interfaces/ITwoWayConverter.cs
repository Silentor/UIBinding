namespace UIBindings
{
    public  interface ITwoWayConverter<TOutput> : IOneWayConverter<TOutput>
    {
        void ProcessTargetToSource( TOutput value );
    }
}