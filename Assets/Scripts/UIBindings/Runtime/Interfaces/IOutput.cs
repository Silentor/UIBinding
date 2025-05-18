namespace UIBindings
{
    public interface IOutput<in TOutput>
    {
        void ProcessTargetToSource( TOutput value );
    }
}