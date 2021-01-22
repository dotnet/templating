
namespace Microsoft.TemplateSearch.TemplateDiscovery.PackProviders
{
    public interface IPackInfo
    {
        string Id { get; }
        string Version { get; }
        int TotalDownloads { get; }
    }
}
