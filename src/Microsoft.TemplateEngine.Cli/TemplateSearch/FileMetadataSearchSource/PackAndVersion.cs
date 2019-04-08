namespace Microsoft.TemplateEngine.Cli.TemplateSearch.FileMetadataSearchSource
{
    public class PackAndVersion
    {
        public static PackAndVersion Empty = new PackAndVersion(string.Empty, string.Empty);

        public PackAndVersion(string name, string version)
        {
            Name = name;
            Version = version;
        }

        public string Name { get; }
        public string Version { get; }
    }
}
