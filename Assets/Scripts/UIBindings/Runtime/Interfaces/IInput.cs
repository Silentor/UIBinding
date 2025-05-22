namespace UIBindings
{
    public interface IInput<in TInput>
    {
        void ProcessSourceToTarget( TInput value );        
    }
}