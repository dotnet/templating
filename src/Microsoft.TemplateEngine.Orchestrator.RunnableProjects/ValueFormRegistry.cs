// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ConfigModel;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ValueForms;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    internal static class ValueFormRegistry
    {
        private static readonly IReadOnlyDictionary<string, ISerializableValueForm> FormLookup = SetupFormLookup();

        internal static IReadOnlyDictionary<string, IValueForm> AllForms
        {
            get
            {
                return FormLookup.ToDictionary(x => x.Key, x => (IValueForm)x.Value);
            }
        }

        internal static IValueForm GetForm(string name, JObject obj)
        {
            string identifier = obj.ToString("identifier");

            if (!FormLookup.TryGetValue(identifier, out ISerializableValueForm value))
            {
                return FormLookup[IdentityValueForm.FormName].FromJObject(name, obj);
            }

            return value.FromJObject(name, obj);
        }

        private static IReadOnlyDictionary<string, ISerializableValueForm> SetupFormLookup()
        {
            Dictionary<string, ISerializableValueForm> lookup = new Dictionary<string, ISerializableValueForm>(StringComparer.OrdinalIgnoreCase);
            ISerializableValueForm x = new ReplacementValueFormModel();
            lookup[x.Identifier] = x;
            x = new ChainValueFormModel();
            lookup[x.Identifier] = x;
            x = new XmlEncodeValueFormModel();
            lookup[x.Identifier] = x;
            x = new JsonEncodeValueFormModel();
            lookup[x.Identifier] = x;
            x = new IdentityValueForm();
            lookup[x.Identifier] = x;

            x = new DefaultSafeNameValueFormModel();
            lookup[x.Identifier] = x;
            x = new DefaultLowerSafeNameValueFormModel();
            lookup[x.Identifier] = x;
            x = new DefaultSafeNamespaceValueFormModel();
            lookup[x.Identifier] = x;
            x = new DefaultLowerSafeNamespaceValueFormModel();
            lookup[x.Identifier] = x;

            x = new LowerCaseValueFormModel();
            lookup[x.Identifier] = x;
            x = new LowerCaseInvariantValueFormModel();
            lookup[x.Identifier] = x;
            x = new UpperCaseValueFormModel();
            lookup[x.Identifier] = x;
            x = new UpperCaseInvariantValueFormModel();
            lookup[x.Identifier] = x;

            x = new FirstLowerCaseValueFormModel();
            lookup[x.Identifier] = x;
            x = new FirstUpperCaseValueFormModel();
            lookup[x.Identifier] = x;
            x = new FirstLowerCaseInvariantValueFormModel();
            lookup[x.Identifier] = x;
            x = new FirstUpperCaseInvariantValueFormModel();
            lookup[x.Identifier] = x;
            x = new KebabCaseValueFormModel();
            lookup[x.Identifier] = x;
            x = new TitleCaseValueFormModel();
            lookup[x.Identifier] = x;

            return lookup;
        }
    }
}
