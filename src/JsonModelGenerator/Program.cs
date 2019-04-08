using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JsonModelGenerator
{
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
                    usings.Add($"using {lines[i].Substring(1)};");
                }
                else if (lines[i][0] == '@')
                {
                    continue;
                }
                else
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
                        if (Regex.IsMatch(compactifiedType, @"(Guid|DateTime)(\?)?(?=[$\[\{])"))
                        {
                            usings.Add("using System;");
                        }

                        properties.Add($"public {compactifiedType} {propertyName} {{ get; set; }}");
                        builderLines.Add($".Map(p => p.{propertyName})");
                    }
                }
            }

            usings.Add("using Microsoft.TemplateEngine.Utils.Json;");
            usings.Add("using Microsoft.TemplateEngine.Abstractions.Json;");

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

            string listDirective = BuiltInTypes.TryGetValue(compactifiedType, out string listType)
                ? $".{setTerm}Of{listType}()"
                : isDictionaryType.Count > 0
                    ? $".{setTerm}<{type}, {{0}}>(b => b.Map(p => p.{valueProperty}))"
                    : $".{setTerm}<{owningType}, {compactifiedType}>(b => b.Map(p => p.{valueProperty}))";
            listDirective = string.Format(listDirective, compactifiedType);
            string line = $"{listDirective}.Map(p => p.{thisPropertyName}, {ctor})";
            int maxBuilderId = isDictionaryType.Count;

            while (isDictionaryType.Count > 0)
            {
                string previousType = type;
                type = currentType.Dequeue();
                dictionaryType = isDictionaryType.Pop();
                ctor = string.Format(constructors.Dequeue(), compactifiedType);
                string builderReference = $"b{maxBuilderId - isDictionaryType.Count}";
                string parentReference = $"p{maxBuilderId - isDictionaryType.Count - 1}";
                setTerm = dictionaryType ? "Dictionary" : "List";
                thisPropertyName = isDictionaryType.Count > 0 ? valueProperty : propertyName;

                listDirective = isDictionaryType.Count > 0
                    ? $".{setTerm}<{type}, {previousType}>({builderReference} => {builderReference}{line})"
                    : $".{setTerm}<{owningType}, {previousType}>({builderReference} => {builderReference}{line})";
                listDirective = string.Format(listDirective, compactifiedType);
                line = $"{listDirective}.Map({parentReference} => {parentReference}.{thisPropertyName}, {ctor})";
            }

            return (wholeProperty, line);
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
                    string extNamespace = new FileInfo(path).Directory.FullName.Substring(basePath.Length).Trim(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, '.');
                    string ns = string.Join('.', entry.Value, extNamespace).Trim('.');
                    string generated = GenerateCode(path, ns);
                    string targetFile = Path.ChangeExtension(path, ".cs");
                    File.WriteAllText(targetFile, generated);
                }
            }
        }
    }
}
