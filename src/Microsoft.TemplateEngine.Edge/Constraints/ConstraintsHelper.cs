// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Edge.Constraints
{
    internal static class ConstraintsHelper
    {
        public static IEnumerable<string> ParseConstraintStrings(this string? args)
        {
            JToken token = ParseConstraintJToken(args);

            if (token.Type == JTokenType.String)
            {
                return new[] { token.Value<string>() ?? throw new ConfigurationException(string.Format(LocalizableStrings.Constaint_Error_ArgumentHasEmptyString, args)) };
            }

            JArray array = token.ToConstraintsJArray(args, true);

            return array.Values<string>().Select(value =>
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ConfigurationException(string.Format(LocalizableStrings.Constaint_Error_ArgumentHasEmptyString, args));
                }

                return value!;
            });
        }

        public static IEnumerable<JObject> ParseConstraintJObjects(this string? args)
        {
            JToken token = ParseConstraintJToken(args);
            JArray array = token.ToConstraintsJArray(args, false);

            return array.Select(value =>
            {
                if (value is not JObject jobj)
                {
                    throw new ConfigurationException(string.Format(LocalizableStrings.Constraint_Error_InvalidJsonArray_Objects, args));
                }

                return jobj;
            });
        }

        private static JToken ParseConstraintJToken(this string? args)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                throw new ConfigurationException(LocalizableStrings.Constraint_Error_ArgumentsNotSpecified);
            }

            JToken? token;
            try
            {
                token = JToken.Parse(args!);
            }
            catch (Exception e)
            {
                throw new ConfigurationException(string.Format(LocalizableStrings.Constraint_Error_InvalidJson, args), e);
            }

            return token;
        }

        private static JArray ToConstraintsJArray(this JToken token, string? args, bool isStringTypeAllowed)
        {
            if (token is not JArray array)
            {
                throw new ConfigurationException(string.Format(
                    isStringTypeAllowed
                        ? LocalizableStrings.Constraint_Error_InvalidJsonType_StringOrArray
                        : LocalizableStrings.Constraint_Error_InvalidJsonType_Array,
                    args));
            }

            if (array.Count == 0)
            {
                throw new ConfigurationException(string.Format(LocalizableStrings.Constraint_Error_ArrayHasNoObjects, args));
            }

            return array;
        }
    }
}
