// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Utils;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Edge.Settings.TemplateInfoReaders
{
    internal class TemplateInfoReaderVersion1_0_0_4 : TemplateInfoReaderVersion1_0_0_3
    {
        internal static new TemplateInfo FromJObject(JObject jObject)
        {
            TemplateInfoReaderVersion1_0_0_4 reader = new TemplateInfoReaderVersion1_0_0_4();
            return reader.Read(jObject);
        }

        protected override ICacheTag ReadOneTag(JProperty item)
        {
            Dictionary<string, ParameterChoice> choices = new Dictionary<string, ParameterChoice>(StringComparer.OrdinalIgnoreCase);

            foreach (JProperty choiceObject in item.Value.PropertiesOf("Choices"))
            {
                choices.Add(choiceObject.Name, new ParameterChoice(
                    choiceObject.Value.ToString("DisplayName"),
                    choiceObject.Value.ToString("Description")));
            }

            CacheTag tag = new CacheTag(
                displayName: item.Value.ToString("DisplayName"),
                description: item.Value.ToString("Description"),
                choices,
                item.Value.ToString("DefaultValue"));

            tag.DefaultIfOptionWithoutValue = item.Value.ToString("DefaultIfOptionWithoutValue");
            return tag;
        }

        protected override ICacheParameter ReadOneParameter(JProperty item)
        {
            return new CacheParameter(
                dataType: item.Value.ToString("DataType"),
                defaultValue: item.Value.ToString("DefaultValue"),
                displayName: item.Value.ToString("DisplayName"),
                description: item.Value.ToString("Description"),
                defaultIfOptionWithoutValue: item.Value.ToString("DefaultIfOptionWithoutValue"));
        }
    }
}
