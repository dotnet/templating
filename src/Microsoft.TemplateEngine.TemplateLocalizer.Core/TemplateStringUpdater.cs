// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.TemplateEngine.TemplateLocalizer.Core
{
    internal static class TemplateStringUpdater
    {
        /// <summary>
        /// Updates the templatestrings.json files for given languages with the provided strings.
        /// </summary>
        /// <param name="strings">Template strings to be included in the templatestrings.json files.</param>
        /// <param name="templateJsonLanguage">The language of the <paramref name="strings"/>.</param>
        /// <param name="supportedLanguages">The list of languages for which templatestrings.json file will be created.</param>
        /// <param name="targetDirectory">The directory that will contain the generated templatestrings.json files.</param>
        /// <param name="logger"><see cref="ILogger"/> to be used for logging.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The task that tracks the status of the async operation.</returns>
        public static async Task UpdateStringsAsync(
            IEnumerable<TemplateString> strings,
            string templateJsonLanguage,
            IEnumerable<string> supportedLanguages,
            string targetDirectory,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(targetDirectory);

            foreach (string language in supportedLanguages)
            {
                string locFilePath = Path.Combine(targetDirectory, language + ".templatestrings.json");
                Dictionary<string, string>? existingStrings = null;

                // Check existing strings only if this is not the original language the template was created in.
                // Because we know that the text in template.json is always more up-to-date than the text in templatestrings.json.
                if (templateJsonLanguage != language)
                {
                    existingStrings = await GetExistingStringsAsync(locFilePath, logger, cancellationToken).ConfigureAwait(false);
                }

                await SaveTemplateStringsFileAsync(strings, existingStrings, locFilePath, logger, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<Dictionary<string, string>> GetExistingStringsAsync(string locFilePath, ILogger logger, CancellationToken cancellationToken)
        {
            try
            {
                logger.LogDebug("Loading existing localizations from file \"{0}\"", locFilePath);
                using FileStream openStream = File.OpenRead(locFilePath);

                JsonSerializerOptions serializerOptions = new()
                {
                    AllowTrailingCommas = true,
                    MaxDepth = 1,
                };

                return await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(openStream, serializerOptions, cancellationToken)
                    .ConfigureAwait(false)
                    ?? new Dictionary<string, string>();
            }
            catch (FileNotFoundException)
            {
                // templatestrings.json file doesn't exist. It will be created from scratch.
                return new();
            }
            catch (Exception)
            {
                logger.LogError("Failed to read the existing strings from \"{0}\"", locFilePath);
                throw;
            }
        }

        private static async Task SaveTemplateStringsFileAsync(
            IEnumerable<TemplateString> templateStrings,
            Dictionary<string, string>? existingStrings,
            string filePath,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            JsonWriterOptions writerOptions = new JsonWriterOptions()
            {
                // Allow unescaped characters in the strings. This allows writing "aren't" instead of "aren\u0027t".
                // This is only considered unsafe in a context where symbols may be interpreted as special characters.
                // For instance, '<' character should be escaped in html documents where this json will be embedded.
                // This safety concern doesn't apply to template engine.
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Indented = true,
            };

            logger.LogDebug("Opening the following templatestrings.json file for writing: \"{0}\"", filePath);
            using FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            using Utf8JsonWriter jsonWriter = new Utf8JsonWriter(fileStream, writerOptions);

            jsonWriter.WriteStartObject();

            foreach (TemplateString templateString in templateStrings)
            {
                string? localizedText = null;
                if (!existingStrings?.TryGetValue(templateString.LocalizationKey, out localizedText) ?? false)
                {
                    // Existing file did not contain a localized version of the string. Use the original value from template.json.
                    localizedText = templateString.Value;
                }

                jsonWriter.WritePropertyName(templateString.LocalizationKey);
                jsonWriter.WriteStringValue(localizedText);

                // A translation and the related comment should be next to each other. Write the comment now before any other text.
                string commentKey = "_" + templateString.LocalizationKey + ".comment";
                if (existingStrings != null && existingStrings.TryGetValue(commentKey, out string? comment))
                {
                    jsonWriter.WritePropertyName(commentKey);
                    jsonWriter.WriteStringValue(comment);
                }
            }

            jsonWriter.WriteEndObject();
            await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
