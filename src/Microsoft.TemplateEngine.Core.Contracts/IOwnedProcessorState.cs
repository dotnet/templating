namespace Microsoft.TemplateEngine.Core.Contracts
{
    public interface IOwnedProcessorState : IProcessorState
    {
        IProcessor Processor { get; }
    }
}
