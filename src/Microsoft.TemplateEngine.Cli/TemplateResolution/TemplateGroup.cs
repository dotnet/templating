using Microsoft.TemplateEngine.Cli.HelpAndUsage;
using Microsoft.TemplateEngine.Edge.Template;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.TemplateEngine.Cli.TemplateResolution
{
    /// <summary>
    /// The class represents template group. Templates in single group:<br/>
    /// - should same group identity
    /// - should have different template identity <br/>
    /// - same short name (however different short names are also supported) <br/>
    /// - the templates may have different languages and types <br/>
    /// - the templates should have different precedence value in case same language is used <br/>
    /// - the templates in the group may have different parameters and different choices for parameter symbols defined<br/>
    /// In case the template does not have group identity defined it represents separate template group with single template.
    /// </summary>
    internal class TemplateGroup
    {
        internal TemplateGroup (string groupIdentity, IEnumerable<ITemplateMatchInfo> templates)
        {
            GroupIdentity = groupIdentity;
            if (templates != null)
            {
                Templates = templates.ToList();
            }
            else
            {
                Templates = new List<ITemplateMatchInfo>();
            }
        }
        /// <summary>
        /// Group identity of template group. The value can be empty if the template does not have group identity set
        /// </summary>
        internal string GroupIdentity { get; private set; }

        /// <summary>
        /// Default shortname.
        /// In theory, template group templates can have different short names but they are treated equally
        /// </summary>
        internal string ShortName => Templates.First().Info.ShortName;

        /// <summary>
        /// Returns true when <see cref="GroupIdentity"/> is not <see cref="null"/> or emply
        /// </summary>
        internal bool HasGroupIdentity
        {
            get
            {
                return !string.IsNullOrWhiteSpace(GroupIdentity);
            }
        }

        /// <summary>
        /// Returns true when the template group has single template
        /// </summary>
        internal bool HasSingleTemplate
        {
            get
            {
                return Templates.Count == 1;
            }
        }

        /// <summary>
        /// Returns the enumerator to invokable templates in the group
        /// </summary>
        internal IEnumerable<ITemplateMatchInfo> InvokableTemplates
        {
            get
            {
                return Templates.Where(templates => templates.IsInvokableMatch());
            }
        }

        /// <summary>
        /// Returns the collection of templates in the group
        /// </summary>
        internal IReadOnlyCollection<ITemplateMatchInfo> Templates { get; private set; }

        /// <summary>
        /// Returns the ambiguous <see cref="MatchKind.SingleStartsWith"/> parameters in invokable templates in the template group 
        /// </summary>
        /// <returns>the enumerator for ambiguous <see cref="MatchKind.SingleStartsWith"/> parameters in invokable templates in the template group</returns>
        /// <remarks>The template group is not valid when there are at least one ambiguous <see cref="MatchKind.SingleStartsWith"/> parameters in invokable templates </remarks>
        internal IEnumerable<InvalidParameterInfo> GetAmbiguousSingleStartsWithParameters()
        {
            var invalidParameterList = new List<InvalidParameterInfo>();
            HashSet<string> singleStartsWithParamNames = new HashSet<string>();
            foreach (ITemplateMatchInfo checkTemplate in InvokableTemplates)
            {
                IEnumerable<MatchInfo> singleStartParams = checkTemplate.MatchDisposition.Where(x => x.Location == MatchLocation.OtherParameter && x.Kind == MatchKind.SingleStartsWith);
                foreach (var singleStartParam in singleStartParams)
                {
                    if (!singleStartsWithParamNames.Add(singleStartParam.InputParameterName))
                    {
                        invalidParameterList.Add(new InvalidParameterInfo(
                                                InvalidParameterInfoKind.AmbiguousValue,
                                                singleStartParam.InputParameterFormat,
                                                singleStartParam.ParameterValue,
                                                singleStartParam.InputParameterName));
                    }
                }
            }
            return invalidParameterList.Distinct();
        }

        /// <summary>
        /// Returns the invalid template specific parameters for the template group.
        /// Invalid parameters can have: invalid name, invalid value (determined only for choice parameter symbols), ambiguous value (determined only for choice parameter symbols)
        /// </summary>
        /// <returns>The enumerator for invalid parameters in templates in the template group</returns>
        internal IEnumerable<InvalidParameterInfo> GetInvalidParameterList()
        {
            var invalidParameterList = new List<InvalidParameterInfo>();

            //collect the parameters which have ambiguous value match in all templates in the template group
            IEnumerable<MatchInfo> ambiguousParametersForTemplates = Templates.SelectMany(template => template.MatchDisposition.Where(x => x.Location == MatchLocation.OtherParameter
                                                               && x.Kind == MatchKind.AmbiguousParameterValue)).Distinct(new OrdinalIgnoreCaseMatchInfoComparer());
            foreach (MatchInfo parameter in ambiguousParametersForTemplates)
            {
                invalidParameterList.Add(new InvalidParameterInfo(
                                                InvalidParameterInfoKind.AmbiguousValue,
                                                parameter.InputParameterFormat,
                                                parameter.ParameterValue,
                                                parameter.InputParameterName));
            }

            if (InvokableTemplates.Any())
            {
                //add the parameters that have single starts with match in several invokable templates in template group
                return invalidParameterList.Union(GetAmbiguousSingleStartsWithParameters()).ToList();
            }

            //collect the parameters with invalid names for all templates in the template group
            IEnumerable<MatchInfo> parametersWithInvalidNames = Templates.SelectMany(template => template.MatchDisposition.Where(x => x.Location == MatchLocation.OtherParameter
                                                                && x.Kind == MatchKind.InvalidParameterName)).Distinct(new OrdinalIgnoreCaseMatchInfoComparer());

            foreach (MatchInfo parameter in parametersWithInvalidNames)
            {
                if (Templates.All(
                    template => template.MatchDisposition.Any(x => x.Location == MatchLocation.OtherParameter
                                                                && x.Kind == MatchKind.InvalidParameterName
                                                                && x.InputParameterName.Equals(parameter.InputParameterName, StringComparison.OrdinalIgnoreCase))))
                {
                    invalidParameterList.Add(new InvalidParameterInfo(
                                                InvalidParameterInfoKind.InvalidParameterName,
                                                parameter.InputParameterFormat,
                                                parameter.ParameterValue,
                                                parameter.InputParameterName));
                }
            }

            //if there are templates which have a match for all template specific parameters, only they to be analyzed
            var filteredTemplates = Templates.Where(template => !template.HasInvalidParameterName());
            if (!filteredTemplates.Any())
            {
                filteredTemplates = Templates;
            }

            //collect the choice parameters with invalid values
            IEnumerable<MatchInfo> invalidParameterValuesForTemplates = filteredTemplates.SelectMany(template => template.MatchDisposition.Where(x => x.Location == MatchLocation.OtherParameter
                                                             && x.Kind == MatchKind.InvalidParameterValue)).Distinct(new OrdinalIgnoreCaseMatchInfoComparer());
            foreach (MatchInfo parameter in invalidParameterValuesForTemplates)
            {
                if (filteredTemplates.All(
                   template => template.MatchDisposition.Any(x => x.Location == MatchLocation.OtherParameter
                                                               && x.Kind == MatchKind.InvalidParameterValue
                                                               && x.InputParameterName.Equals(parameter.InputParameterName, StringComparison.OrdinalIgnoreCase))))
                {
                    invalidParameterList.Add(new InvalidParameterInfo(
                                                InvalidParameterInfoKind.InvalidValue,
                                                parameter.InputParameterFormat,
                                                parameter.ParameterValue,
                                                parameter.InputParameterName));
                }
            }

            return invalidParameterList;
        }

        /// <summary>
        /// The method returns the single invokable template with highest precedence
        /// </summary>
        /// <param name="highestPrecedenceTemplate">Contains the invokable template with highest precedence</param>
        /// <param name="useDefaultLanguage">Defines if default language template should be preferred in case of ambiguity</param>
        /// <returns>
        /// <see cref="true"/> when single invokable template with highest precedence can be defined, 
        /// <see cref="false"/> otherwise
        /// </returns>
        internal bool TryGetHighestPrecedenceInvokableTemplate(out ITemplateMatchInfo highestPrecedenceTemplate, bool useDefaultLanguage = false)
        {
            highestPrecedenceTemplate = null;
            if (!InvokableTemplates.Any())
            {
                return false;
            }
            IEnumerable<ITemplateMatchInfo> highestPrecendenceTemplates = GetHighestPrecedenceInvokableTemplates(useDefaultLanguage);
            if (highestPrecendenceTemplates.Count() == 1)
            {
                highestPrecedenceTemplate = highestPrecendenceTemplates.First();
                return true;
            }
            return false;
        }

        /// <summary>
        /// The method returns the invokable templates with highest precedence
        /// </summary>
        /// <param name="useDefaultLanguage">Defines if default language template should be preferred in case of ambiguity</param>
        /// <returns>
        /// the enumerator of invokable templates with highest precedence
        /// </returns>
        internal IEnumerable<ITemplateMatchInfo> GetHighestPrecedenceInvokableTemplates(bool useDefaultLanguage = false)
        {
            IEnumerable<ITemplateMatchInfo> highestPrecedenceTemplates;
            if (!InvokableTemplates.Any())
            {
                return new List<ITemplateMatchInfo>();
            }

            int highestPrecedence = InvokableTemplates.Max(t => t.Info.Precedence);
            highestPrecedenceTemplates = InvokableTemplates.Where(t => t.Info.Precedence == highestPrecedence);

            if (useDefaultLanguage && highestPrecedenceTemplates.Count() > 1)
            {
                IEnumerable<ITemplateMatchInfo> highestPrecedenceTemplatesForDefaultLanguage = highestPrecedenceTemplates.Where(t => t.HasDefaultLanguageMatch());
                if (highestPrecedenceTemplatesForDefaultLanguage.Any())
                {
                    return highestPrecedenceTemplatesForDefaultLanguage;
                }
            }
            return highestPrecedenceTemplates;
        }

    }
}
