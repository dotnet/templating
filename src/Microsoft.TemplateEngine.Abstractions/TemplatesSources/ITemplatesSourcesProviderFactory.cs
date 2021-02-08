namespace Microsoft.TemplateEngine.Abstractions.TemplatesSources
{
    /// <summary>
    /// Factory resposible for creating <see cref="ITemplatesSourcesProvider"/> or <see cref="IManagedTemplatesSourcesProvider"/>.
    /// This is registered with <see cref="IComponentManager"/> either via <see cref="IComponentManager.Register(System.Type)"/> or
    /// <see cref="ITemplateEngineHost.BuiltInComponents"/>.
    /// </summary>
    public interface ITemplatesSourcesProviderFactory : IIdentifiedComponent
    {
        /// <summary>
        /// This is used for two reasons:
        ///     1) To show user which provider is source of templates(when debug/verbose flag is set)
        ///     2) To allow user to pick specific provider to install to templates to
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Creates new provider with specified enviroment, provider can also implement <see cref="IManagedTemplatesSourcesProvider"/>.
        /// </summary>
        ITemplatesSourcesProvider CreateProvider(IEngineEnvironmentSettings settings);
    }
}
