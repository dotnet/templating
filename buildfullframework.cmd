dotnet build src/Microsoft.TemplateEngine.Edge/project.json -f net451
dotnet build src/Microsoft.TemplateEngine.Orchestrator.RunnableProjects/project.json -f net451
copy src\Microsoft.TemplateEngine.Orchestrator.RunnableProjects\bin\Debug\net451\* src\Microsoft.TemplateEngine.Edge\bin\Debug\net451\ /Y
