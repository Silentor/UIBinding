namespace UIBindings
{
    public interface IInput<in TInput>
    {
        void ProcessSourceToTarget( TInput value );        
    }

    public interface IOutput<in TOutput>
    {
        void ProcessTargetToSource( TOutput value );
    }
}