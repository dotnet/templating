using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JsonModelGenerator
{
    /*
        Syntax guide:
        Lines starting with '#' indicate a using directive that should be included
        Lines starting with '@' indicate a comment that should be ignored during code generation
        All other lines must be of the form [property name][one or more whitespace characters][type] (ex. "Foo  string")
        -   Dictionary<string, T> types are indicated with '{}' (ex. "Foo  int{}" results in a property called "Foo" with type Dictionary<string, int>)
        -   List<T> types are indicated with '[]' (ex. "Foo  int[]" results in a property called "Foo" with type List<int>)
        -   Dictionary and/or list types can be chained (ex. "Foo  int[]{}" results in a property called "Foo" with type Dictionary<string, List<int>>)

Example.jm:
#System 
#System.Collections.Generic
@This is a comment
Condition                       string 
Include                         string[] 
CopyOnly                        string[] 
Exclude                         string[] 
Rename                          string{} 

    */
    class Program
    {
        private static readonly IReadOnlyDictionary<string, string> BuiltInTypes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "int", "Int" },
            { "string", "String" },
            { "Guid", "Guid" },
            { "DateTime?", "NullableDateTime" },
            { "bool", "Bool" }
        };

        private static readonly Regex SystemNamespaceDefinedStructMatcher = new Regex(@"(Guid|DateTime)(\?)?(?=[$[{])", RegexOptions.Compiled);

        public static string GenerateCode(string sourceFilePath, string @namespace)
        {
            string className = Path.GetFileNameWithoutExtension(sourceFilePath);
            string[] lines = File.ReadAllLines(sourceFilePath);
            HashSet<string> usings = new HashSet<string>(StringComparer.Ordinal);
            List<string> properties = new List<string>();
            List<string> builderLines = new List<string>();

            for (int i = 0; i < lines.Length; ++i)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                if (lines[i][0] == '#')
                {
                    usings.Add($"using {lines[i].Substring(1).Trim()};");
                }
                else if (lines[i][0] == '@')
                {
                    continue;
                }
                else
                {
                    GenerateProperty(lines, i, usings, properties, className, builderLines);
                }
            }

            usings.Add("using Microsoft.TemplateEngine.Utils.Json;");
            usings.Add("using Microsoft.TemplateEngine.Abstractions.Json;");

            return GenerateResultFile(usings, @namespace, className, properties, builderLines);
        }

        private static void GenerateProperty(string[] lines, int i, HashSet<string> usings, List<string> properties, string className, List<string> builderLines)
        {
            lines[i] = lines[i].Trim();
            int whitespaceIndex = lines[i].IndexOfAny(" \t".ToCharArray());
            string propertyName = lines[i].Substring(0, whitespaceIndex);
            string compactifiedType = lines[i].Substring(whitespaceIndex).Trim();

            if (compactifiedType.EndsWith("{}", StringComparison.Ordinal) || compactifiedType.EndsWith("[]", StringComparison.Ordinal))
            {
                usings.Add("using System.Collections.Generic;");
                (string property, string builderLine) = GeneratePropertyForArrayOrDictionary(className, compactifiedType, propertyName, usings);
                properties.Add(property);
                builderLines.Add(builderLine);
            }
            else
            {
                if (SystemNamespaceDefinedStructMatcher.IsMatch(compactifiedType))
                {
                    usings.Add("using System;");
                }

                if (TryExtractFactoryDataFromCompactifiedType(ref compactifiedType, out string mappingsSelectorFunction))
                {
                    builderLines.Add($".Map(p => p.{propertyName}, (p, v) => p.{propertyName} = v, {mappingsSelectorFunction}, \"{propertyName}\")");
                }
                else
                {
                    builderLines.Add($".Map(p => p.{propertyName}, (p, v) => p.{propertyName} = v, \"{propertyName}\")");
                }

                properties.Add($"public {compactifiedType} {propertyName} {{ get; set; }}");
            }
        }

        private static bool TryExtractFactoryDataFromCompactifiedType(ref string compactifiedType, out string mappingsSelectorFunction)
        {
            int dashIndex = compactifiedType.IndexOf('-');

            if (dashIndex < 0)
            {
                mappingsSelectorFunction = null;
                return false;
            }

            mappingsSelectorFunction = compactifiedType.Substring(dashIndex + 1);
            compactifiedType = compactifiedType.Substring(0, dashIndex);
            return true;
        }

        private static (string property, string builderLine) GeneratePropertyForArrayOrDictionary(string owningType, string compactifiedType, string propertyName, HashSet<string> usings)
        {
            const string valueProperty = "Value";
            Stack<bool> isDictionaryType = new Stack<bool>();
            Queue<string> currentType = new Queue<string>();
            Queue<string> constructors = new Queue<string>();
            Stack<string> currentTypeStack = new Stack<string>();
            Stack<string> constructorsStack = new Stack<string>();

            while (true)
            {
                if (compactifiedType.EndsWith("{}", StringComparison.Ordinal))
                {
                    usings.Add("using System;");
                    isDictionaryType.Push(true);
                    currentTypeStack.Push("Dictionary<string, {0}>");
                    constructorsStack.Push("() => new Dictionary<string, {0}>(StringComparer.Ordinal)");
                    compactifiedType = compactifiedType.Substring(0, compactifiedType.Length - 2);
                }
                else if (compactifiedType.EndsWith("[]", StringComparison.Ordinal))
                {
                    isDictionaryType.Push(false);
                    currentTypeStack.Push("List<{0}>");
                    constructorsStack.Push("() => new List<{0}>()");
                    compactifiedType = compactifiedType.Substring(0, compactifiedType.Length - 2);
                }
                else
                {
                    break;
                }
            }

            bool isFactoryBased = TryExtractFactoryDataFromCompactifiedType(ref compactifiedType, out string mappingSelectorFunction);
            string currentTypeName = compactifiedType;
            while (currentTypeStack.Count > 0)
            {
                string nextTypeName = string.Format(currentTypeStack.Pop(), currentTypeName);
                currentType.Enqueue(nextTypeName);
                constructors.Enqueue(string.Format(constructorsStack.Pop(), currentTypeName));
                currentTypeName = nextTypeName;
            }

            string wholeProperty = $"public {string.Format(CultureInfo.InvariantCulture, currentTypeName, compactifiedType)} {propertyName} {{ get; set; }}";

            bool dictionaryType = isDictionaryType.Pop();
            string type = currentType.Dequeue();
            string ctor = string.Format(constructors.Dequeue(), compactifiedType);
            string setTerm = dictionaryType ? "Dictionary" : "List";
            string thisPropertyName = isDictionaryType.Count > 0 ? valueProperty : propertyName;
            string listDirective;

            if (BuiltInTypes.TryGetValue(compactifiedType, out string listType))
            {
                listDirective = $".{setTerm}Of{listType}()";
            }
            else
            {
                string mapValue = isFactoryBased
                    ? $", {mappingSelectorFunction}"
                    : string.Empty;

                if (isDictionaryType.Count > 0)
                {
                    listDirective = $".{setTerm}<{type}, {{0}}>(b => b.Map(p => p.{valueProperty}, (p, v) => p.{valueProperty} = v{mapValue}, \"{valueProperty}\"))";
                }
                else
                {
                    listDirective = $".{setTerm}<{owningType}, {compactifiedType}>(b => b.Map(p => p.{valueProperty}, (p, v) => p.{valueProperty} = v{mapValue}, \"{valueProperty}\"))";
                }
            }

            listDirective = string.Format(listDirective, compactifiedType);
            string line = $"{listDirective}.Map(p => p.{thisPropertyName}, (p, v) => p.{thisPropertyName} = v, {ctor}, \"{propertyName}\")";
            int maxBuilderId = isDictionaryType.Count;

            while (isDictionaryType.Count > 0)
            {
                string previousType = type;
                type = currentType.Dequeue();
                dictionaryType = isDictionaryType.Pop();
                ctor = string.Format(constructors.Dequeue(), compactifiedType);
                string builderReference = $"b{maxBuilderId - isDictionaryType.Count}";
                string parentReference = $"p{maxBuilderId - isDictionaryType.Count - 1}";
                string valueReference = $"v{maxBuilderId - isDictionaryType.Count - 1}";
                setTerm = dictionaryType ? "Dictionary" : "List";
                thisPropertyName = isDictionaryType.Count > 0 ? valueProperty : propertyName;

                listDirective = isDictionaryType.Count > 0
                    ? $".{setTerm}<{type}, {previousType}>({builderReference} => {builderReference}{line})"
                    : $".{setTerm}<{owningType}, {previousType}>({builderReference} => {builderReference}{line})";
                listDirective = string.Format(listDirective, compactifiedType);
                line = $"{listDirective}.Map({parentReference} => {parentReference}.{thisPropertyName}, ({parentReference}, {valueReference}) => {parentReference}.{thisPropertyName} = {valueReference}, {ctor}, \"{propertyName}\")";
            }

            return (wholeProperty, line);
        }

        private static string GenerateResultFile(HashSet<string> usings, string @namespace, string className, IReadOnlyList<string> properties, IReadOnlyList<string> builderLines)
        {
            StringBuilder result = new StringBuilder();
            foreach (string @using in usings.OrderBy(x => x, CompareUsings.Default))
            {
                result.AppendLine(@using);
            }

            result.AppendLine();
            result.AppendLine($"namespace {@namespace}");
            result.AppendLine("{");

            result.AppendLine($"    internal class {className} : IJsonSerializable<{className}>");
            result.AppendLine("    {");

            foreach (string propertyDeclaration in properties)
            {
                result.AppendLine($"        {propertyDeclaration}");
                result.AppendLine();
            }

            result.AppendLine($"        public IJsonBuilder<{className}> JsonBuilder {{ get; }} = new JsonBuilder<{className}, {className}>(() => new {className}())");

            foreach (string line in builderLines)
            {
                result.AppendLine($"            {line}");
            }

            result.AppendLine("            ;");

            result.AppendLine("    }");
            result.AppendLine("}");

            return result.ToString();
        }

        static void Main(string[] args)
        {
            string basePath = args[0];

            Dictionary<string, string> searchLocations = new Dictionary<string, string>();

            foreach (string csproj in Directory.EnumerateFiles(basePath, "*.csproj", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(csproj);
                int rootNamespaceElement = text.IndexOf("RootNamespace");
                string detectedNamespace = Path.GetFileName(csproj);
                detectedNamespace = detectedNamespace.Substring(0, detectedNamespace.Length - ".csproj".Length);

                if (rootNamespaceElement != -1)
                {
                    int closeTag = text.IndexOf('<', rootNamespaceElement);
                    detectedNamespace = text.Substring(rootNamespaceElement + "RootNamespace".Length + 1, closeTag - rootNamespaceElement - "RootNamespace".Length - 1);
                }

                searchLocations[Path.GetDirectoryName(csproj)] = detectedNamespace;
            }

            foreach (KeyValuePair<string, string> entry in searchLocations)
            {
                foreach (string path in Directory.EnumerateFiles(entry.Key, "*.jm", SearchOption.AllDirectories))
                {
                    string extNamespace = new FileInfo(path).Directory.FullName.Substring(entry.Key.Length).Trim(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, '.');
                    string ns = string.Join('.', entry.Value, extNamespace).Trim('.');
                    string generated = GenerateCode(path, ns);
                    string targetFile = Path.ChangeExtension(path, ".cs");
                    File.WriteAllText(targetFile, generated);
                }
            }
        }
    }
}
