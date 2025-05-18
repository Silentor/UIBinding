namespace UIBindings
{
    public interface IInput<in TInput>
    {
        void ProcessSourceToTarget( TInput value );        
    }

    public interface IInput2<in TInput1, in TInput2> : IInput<TInput1>
    {
        void ProcessSourceToTarget( TInput2 value2 );        
    }
}