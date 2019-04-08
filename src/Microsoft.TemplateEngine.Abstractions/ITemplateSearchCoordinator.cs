using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Abstractions
{
    public interface ITemplateSearchCoordinator
    {
        Task CoordinateAsync();
    }
}
