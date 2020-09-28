using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    public interface ISymbolModel2 : ISymbolModel
    {
        string FileRename { get; set; }
    }
}
