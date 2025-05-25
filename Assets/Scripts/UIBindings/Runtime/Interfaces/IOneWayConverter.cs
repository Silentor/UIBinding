namespace UIBindings
{
    public interface IOneWayConverter<out TOutput>
    {
        TOutput GetValueFromSource(  );
    }
}