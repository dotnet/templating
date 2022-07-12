// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Utils
{
#pragma warning disable CS0618 // Type or member is obsolete - compatibility
    public class TemplateParameter : ITemplateParameter, IAllowDefaultIfOptionWithoutValue
#pragma warning restore CS0618 // Type or member is obsolete
    {
        [JsonProperty(nameof(Precedence))]
        private readonly TemplateParameterPrecedenceImpl _templateParameterPrecedence;

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="jObject"></param>
        public TemplateParameter(JObject jObject)
        {
            string? name = jObject.ToString(nameof(Name));
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"{nameof(Name)} property should not be null or whitespace", nameof(jObject));
            }

            Name = name!;
            Type = jObject.ToString(nameof(Type)) ?? "parameter";
            DataType = jObject.ToString(nameof(DataType)) ?? "string";
            Description = jObject.ToString(nameof(Description));

            DefaultValue = jObject.ToString(nameof(DefaultValue));
            DefaultIfOptionWithoutValue = jObject.ToString(nameof(DefaultIfOptionWithoutValue));
            DisplayName = jObject.ToString(nameof(DisplayName));
            IsName = jObject.ToBool(nameof(IsName));
            AllowMultipleValues = jObject.ToBool(nameof(AllowMultipleValues));

            if (this.IsChoice())
            {
                Type = "parameter";
                Dictionary<string, ParameterChoice> choices = new Dictionary<string, ParameterChoice>(StringComparer.OrdinalIgnoreCase);
                JObject? cdToken = jObject.Get<JObject>(nameof(Choices));
                if (cdToken != null)
                {
                    foreach (JProperty cdPair in cdToken.Properties())
                    {
                        choices.Add(
                            cdPair.Name.ToString(),
                            new ParameterChoice(
                                cdPair.Value.ToString(nameof(ParameterChoice.DisplayName)),
                                cdPair.Value.ToString(nameof(ParameterChoice.Description))));
                    }
                }
                Choices = choices;
            }

            JToken? precedenceToken;
            TemplateParameterPrecedence precedence = TemplateParameterPrecedence.Default;
            if (jObject.TryGetValue(nameof(Precedence), StringComparison.OrdinalIgnoreCase, out precedenceToken))
            {
                precedence = TemplateParameterPrecedenceImpl.FromJObject(precedenceToken);
            }
            else
            {
#pragma warning disable CS0618 // Type or member is obsolete
                string key = nameof(Priority);
#pragma warning restore CS0618 // Type or member is obsolete
                var legacyPriority = (TemplateParameterPriority)jObject.ToInt32(key);
                precedence = legacyPriority.ToTemplateParameterPrecedence();
            }
            _templateParameterPrecedence = new TemplateParameterPrecedenceImpl(precedence);
        }

        public TemplateParameter(
            string name,
            string type,
            string datatype,
            TemplateParameterPrecedence? precedence = default,
            bool isName = false,
            string? defaultValue = null,
            string? defaultIfOptionWithoutValue = null,
            string? description = null,
            string? displayName = null,
            bool allowMultipleValues = false,
            IReadOnlyDictionary<string, ParameterChoice>? choices = null)
        {
            Name = name;
            Type = type;
            DataType = datatype;
            IsName = isName;
            DefaultValue = defaultValue;
            DefaultIfOptionWithoutValue = defaultIfOptionWithoutValue;
            Description = description;
            DisplayName = displayName;
            AllowMultipleValues = allowMultipleValues;
            _templateParameterPrecedence = new TemplateParameterPrecedenceImpl(precedence ?? TemplateParameterPrecedence.Default);

            if (this.IsChoice())
            {
                Choices = choices ?? new Dictionary<string, ParameterChoice>();
            }
        }

        [Obsolete("Use Description instead.")]
        public string? Documentation => Description;

        [JsonProperty]
        public string Name { get; }

        [JsonIgnore]
        [Obsolete("Use Precedence instead.")]
        public TemplateParameterPriority Priority => Precedence.PrecedenceDefinition.ToTemplateParameterPriority();

        [JsonIgnore]
        public TemplateParameterPrecedence Precedence => _templateParameterPrecedence;

        [JsonProperty]
        public string Type { get; }

        [JsonProperty]
        public bool IsName { get; }

        [JsonProperty]
        public string? DefaultValue { get; }

        [JsonProperty]
        public string DataType { get; set; }

        [JsonProperty]
        public string? DefaultIfOptionWithoutValue { get; set; }

        [JsonProperty]
        public IReadOnlyDictionary<string, ParameterChoice>? Choices { get; }

        [JsonProperty]
        public string? Description { get; }

        [JsonProperty]
        public string? DisplayName { get; }

        [JsonProperty]
        public bool AllowMultipleValues { get; }

        public override string ToString()
        {
            return $"{Name} ({Type})";
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is ITemplateParameter parameter)
            {
                return Equals(parameter);
            }

            return false;
        }

        public override int GetHashCode() => (Name != null ? Name.GetHashCode() : 0);

        public bool Equals(ITemplateParameter other) => !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(other.Name) && Name == other.Name;

        private class TemplateParameterPrecedenceImpl : TemplateParameterPrecedence
        {
            public TemplateParameterPrecedenceImpl(TemplateParameterPrecedence precedence)
                : base(precedence.PrecedenceDefinition, precedence.IsRequiredCondition, precedence.IsEnabledCondition, precedence.IsRequired)
            { }

            [JsonProperty(nameof(PrecedenceDefinition))]
            private PrecedenceDefinition PrecedenceDefinitionAccessor => base.PrecedenceDefinition;

            [JsonProperty(nameof(IsRequiredCondition))]
            private string? IsRequiredConditionAccessor => base.IsRequiredCondition;

            [JsonProperty(nameof(IsEnabledCondition))]
            private string? IsEnabledConditionAccessor => base.IsEnabledCondition;

            [JsonProperty(nameof(IsRequired))]
            private bool IsRequiredAccessor => base.IsRequired;

            public static TemplateParameterPrecedence FromJObject(JToken jObject)
            {
                PrecedenceDefinition precedenceDefinition = (PrecedenceDefinition)jObject.ToInt32(nameof(PrecedenceDefinition));
                string? isRequiredCondition = jObject.ToString(nameof(IsRequiredCondition));
                string? isEnabledCondition = jObject.ToString(nameof(IsEnabledCondition));
                bool isRequired = jObject.ToBool(nameof(IsRequired));

                return new TemplateParameterPrecedence(precedenceDefinition, isRequiredCondition, isEnabledCondition, isRequired);
            }
        }
    }

}
