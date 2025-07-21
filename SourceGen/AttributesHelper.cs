namespace UIBindings.SourceGen
{
    public static class AttributesHelper
    {
        public const string ObservableProperty = @"
namespace UIBindings
{
    [AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false)]
    public class ObservablePropertyAttribute : System.Attribute
    {
    }
}";
    }}