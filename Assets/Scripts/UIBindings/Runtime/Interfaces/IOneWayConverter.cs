namespace UIBindings
{
    public interface IOneWayConverter<TOutput>
    {
        bool TryGetValueFromSource( out TOutput value );
    }
}