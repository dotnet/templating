namespace Microsoft.TemplateEngine.Core.Contracts
{
    public interface IOutputValuesContainerAccessor
    {
        void SetValue(string name, object value);
    }
}
