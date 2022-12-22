﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Utils
{
    internal static class ParameterConverter
    {
        /// <summary>
        /// Converts <paramref name="untypedValue"/> to data type defined in <paramref name="parameter"/>.
        /// Supported types are: choice, bool, int, float, hex, string, text.
        /// </summary>
        /// <param name="host">template engine host (for logging purposes).</param>
        /// <param name="parameter">the parameter to convert value to.</param>
        /// <param name="untypedValue">the value to convert.</param>
        /// <param name="valueResolutionError">the error during conversion, if any.</param>
        /// <returns>Converted value.</returns>
        internal static object? ConvertParameterValueToType(
            ITemplateEngineHost host,
            ITemplateParameter parameter,
            string untypedValue,
            out bool valueResolutionError)
        {
            if (untypedValue == null)
            {
                valueResolutionError = false;
                return null;
            }

            if (!string.IsNullOrEmpty(parameter.DataType))
            {
                object? convertedValue = DataTypeSpecifiedConvertLiteral(host, parameter, untypedValue, out valueResolutionError);
                return convertedValue;
            }
            else
            {
                valueResolutionError = false;
                return InferTypeAndConvertLiteral(untypedValue);
            }
        }

        /// <summary>
        /// Converts <paramref name="literal"/> to closest data type.
        /// Supported types are: bool, null, float, int, hex, string/text (in order of attempting).
        /// </summary>
        /// <param name="literal">the string value to convert.</param>
        /// <returns>Converted value.</returns>
        internal static object? InferTypeAndConvertLiteral(string literal)
        {
            if (literal == null)
            {
                return null;
            }

            if (!literal.Contains("\""))
            {
                if (TryResolveBooleanValue(literal, out bool parsedBool))
                {
                    return parsedBool;
                }
                if (TryResolveNullValue(literal, out _))
                {
                    return null;
                }
                if ((literal.Contains(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                    || literal.Contains(CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator))
                    && TryResolveFloatValue(literal, out double parsedFloat))
                {
                    return parsedFloat;
                }
                if (TryResolveIntegerValue(literal, out long parsedInteger))
                {
                    return parsedInteger;
                }
                if (TryResolveHexValue(literal, out long parsedHex))
                {
                    return parsedHex;
                }
            }
            return literal;
        }

        /// <summary>
        /// Tries to convert <paramref name="literal"/> to <paramref name="dataType"/>.
        /// If <paramref name="dataType"/> is null or empty, the type to be inferred. See <see cref="InferTypeAndConvertLiteral(string)"/> for more details.
        /// </summary>
        internal static bool TryConvertLiteralToDatatype(string literal, string? dataType, out object? value)
        {
            value = null;

            if (string.IsNullOrWhiteSpace(dataType))
            {
                value = InferTypeAndConvertLiteral(literal);
                return true;
            }

            if (string.Equals(dataType, "bool", StringComparison.OrdinalIgnoreCase)
                || string.Equals(dataType, "boolean", StringComparison.OrdinalIgnoreCase))
            {
                if (TryResolveBooleanValue(literal, out bool parsedBool))
                {
                    value = parsedBool;
                    return true;
                }
                return false;
            }
            else if (string.Equals(dataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                if (TryResolveFloatValue(literal, out double convertedFloat))
                {
                    value = convertedFloat;
                    return true;
                }
                return false;
            }
            else if (string.Equals(dataType, "int", StringComparison.OrdinalIgnoreCase)
                || string.Equals(dataType, "integer", StringComparison.OrdinalIgnoreCase))
            {
                if (TryResolveIntegerValue(literal, out long convertedInt))
                {
                    value = convertedInt;
                    return true;
                }
                return false;
            }
            else if (string.Equals(dataType, "hex", StringComparison.OrdinalIgnoreCase))
            {
                if (TryResolveHexValue(literal, out long convertedHex))
                {
                    value = convertedHex;
                    return true;
                }
                return false;
            }
            else if (string.Equals(dataType, "text", StringComparison.OrdinalIgnoreCase)
                || string.Equals(dataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                value = literal;
                return true;
            }
            return false;
        }

        /// <summary>
        /// For explicitly data-typed variables, attempt to convert the variable value to the specified type.
        /// Data type names:
        ///     - choice
        ///     - bool
        ///     - float
        ///     - int
        ///     - hex
        ///     - text
        /// The data type names are case insensitive.
        /// </summary>
        /// <returns>Returns the converted value if it can be converted, throw otherwise.</returns>
        private static object? DataTypeSpecifiedConvertLiteral(ITemplateEngineHost host, ITemplateParameter param, string literal, out bool valueResolutionError)
        {
            valueResolutionError = false;

            if (string.Equals(param.DataType, "bool", StringComparison.OrdinalIgnoreCase))
            {
                if (TryResolveBooleanValue(literal, out bool parsedBool))
                {
                    return parsedBool;
                }
                else
                {
                    bool boolVal = false;
                    // Note: if the literal is ever null, it is probably due to a problem in TemplateCreator.Instantiate()
                    // which takes care of making null bool -> true as appropriate.
                    // This else can also happen if there is a value but it can't be converted.
                    string? val;
#pragma warning disable CS0618 // Type or member is obsolete - for backward compatibility
                    while (host.OnParameterError(param, string.Empty, "ParameterValueNotSpecified", out val) && TryResolveBooleanValue(val, out boolVal))
#pragma warning restore CS0618 // Type or member is obsolete
                    {
                    }

                    valueResolutionError = !TryResolveBooleanValue(val, out boolVal);
                    return boolVal;
                }
            }
            else if (param.IsChoice())
            {
                if (param.AllowMultipleValues)
                {
                    List<string> val =
                        literal
                            .TokenizeMultiValueParameter()
                            .Select(t => ResolveChoice(host, t, param))
                            .Where(r => !string.IsNullOrEmpty(r))
                            .Select(r => r!)
                            .ToList();
                    if (val.Count <= 1)
                    {
                        return val.Count == 0 ? string.Empty : val[0];
                    }

                    return new MultiValueParameter(val);
                }
                else
                {
                    string? val = ResolveChoice(host, literal, param);
                    valueResolutionError = val == null;
                    return val;
                }
            }
            else if (string.Equals(param.DataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                if (TryResolveFloatValue(literal, out double convertedFloat))
                {
                    return convertedFloat;
                }
                else
                {
                    string? val;
#pragma warning disable CS0618 // Type or member is obsolete - for backward compatibility
                    while (host.OnParameterError(param, string.Empty, "ValueNotValidMustBeFloat", out val) && (val == null || !TryResolveFloatValue(val, out convertedFloat)))
#pragma warning restore CS0618 // Type or member is obsolete
                    {
                    }

                    valueResolutionError = !TryResolveFloatValue(val, out convertedFloat);
                    return convertedFloat;
                }
            }
            else if (string.Equals(param.DataType, "int", StringComparison.OrdinalIgnoreCase)
                || string.Equals(param.DataType, "integer", StringComparison.OrdinalIgnoreCase))
            {
                if (TryResolveIntegerValue(literal, out long convertedInt))
                {
                    return convertedInt;
                }
                else
                {
                    string? val;
#pragma warning disable CS0618 // Type or member is obsolete - for backward compatibility
                    while (host.OnParameterError(param, string.Empty, "ValueNotValidMustBeInteger", out val) && TryResolveIntegerValue(val, out convertedInt))
#pragma warning restore CS0618 // Type or member is obsolete
                    {
                    }

                    valueResolutionError = !TryResolveIntegerValue(val, out convertedInt);
                    return convertedInt;
                }
            }
            else if (string.Equals(param.DataType, "hex", StringComparison.OrdinalIgnoreCase))
            {
                if (TryResolveHexValue(literal, out long convertedHex))
                {
                    return convertedHex;
                }
                else
                {
                    string? val;
#pragma warning disable CS0618 // Type or member is obsolete - for backward compatibility
                    while (host.OnParameterError(param, string.Empty, "ValueNotValidMustBeHex", out val) && TryResolveHexValue(val, out convertedHex))
#pragma warning restore CS0618 // Type or member is obsolete
                    {
                    }

                    valueResolutionError = !TryResolveHexValue(val, out convertedHex);
                    return convertedHex;
                }
            }
            else if (string.Equals(param.DataType, "text", StringComparison.OrdinalIgnoreCase)
                || string.Equals(param.DataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                // "text" is a valid data type, but doesn't need any special handling.
                return literal;
            }
            else
            {
                return literal;
            }
        }

        private static string? ResolveChoice(ITemplateEngineHost host, string? literal, ITemplateParameter param)
        {
            if (TryResolveChoiceValue(literal, param, out string? match))
            {
                return match;
            }

            //TODO: here we should likely reevaluate once again after the conditions - but that is another posibility for infinite cycle
            if (literal == null && param.Precedence.PrecedenceDefinition != PrecedenceDefinition.Required)
            {
                return param.DefaultValue;
            }

#pragma warning disable CS0618 // Type or member is obsolete - for backward compatibility
            if (
                host.OnParameterError(param, string.Empty, "ValueNotValid:" + string.Join(",", param.Choices!.Keys), out string? val)
                && TryResolveChoiceValue(val, param, out string? match2))
            {
                return match2;
            }
#pragma warning restore CS0618 // Type or member is obsolete

            return literal == string.Empty ? string.Empty : null;
        }

        private static bool TryResolveChoiceValue(string? literal, ITemplateParameter param, out string? match)
        {
            if (literal == null || param.Choices == null)
            {
                match = null;
                return false;
            }

            string? partialMatch = null;

            foreach (string choiceValue in param.Choices.Keys)
            {
                if (string.Equals(choiceValue, literal, StringComparison.OrdinalIgnoreCase))
                {
                    // exact match is good, regardless of partial matches
                    match = choiceValue;
                    return true;
                }
                else if (choiceValue.StartsWith(literal, StringComparison.OrdinalIgnoreCase))
                {
                    if (partialMatch == null)
                    {
                        partialMatch = choiceValue;
                    }
                    else
                    {
                        // multiple partial matches, can't take one.
                        match = null;
                        return false;
                    }
                }
            }

            match = partialMatch;
            return match != null;
        }

        private static bool TryResolveBooleanValue(string? literal, out bool parsed) => bool.TryParse(literal, out parsed);

        private static bool TryResolveNullValue(string? literal, out object? parsed)
        {
            parsed = null;
            if (string.Equals(literal, "null", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        private static bool TryResolveFloatValue(string? literal, out double parsed) => ParserExtensions.DoubleTryParseСurrentOrInvariant(literal, out parsed);

        private static bool TryResolveIntegerValue(string? literal, out long parsed) => long.TryParse(literal, out parsed);

        private static bool TryResolveHexValue(string? literal, out long parsed)
        {
            parsed = default;
            if (literal == null)
            {
                return false;
            }
            if (literal.Length < 3)
            {
                return false;
            }

            if (literal.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                  && long.TryParse(literal.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsed))
            {
                return true;
            }
            return false;
        }
    }
}
