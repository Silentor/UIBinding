namespace UIBindings
{
    public class InvertConverter : ConverterTwoWayBase<bool, bool>
    {
        public override bool Convert(bool value)
        {
            return !value;
        }

        public override bool ConvertBack(bool value )
        {
            return !value;
        }
    }
}