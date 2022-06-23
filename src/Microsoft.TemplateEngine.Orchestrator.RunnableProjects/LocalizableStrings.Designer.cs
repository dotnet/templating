﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class LocalizableStrings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal LocalizableStrings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.TemplateEngine.Orchestrator.RunnableProjects.LocalizableStrings", typeof(LocalizableStrings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Localization file should only contain elements with type &apos;string&apos;. Remove elements that are not strings..
        /// </summary>
        internal static string Authoring_InvalidJsonElementInLocalizationFile {
            get {
                return ResourceManager.GetString("Authoring_InvalidJsonElementInLocalizationFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to In localization file under the post action with id &apos;{1}&apos;, there are localized strings for manual instruction(s) with ids &apos;{0}&apos;. These manual instructions do not exist in the template.json file and should be removed from localization file..
        /// </summary>
        internal static string Authoring_InvalidManualInstructionLocalizationIndex {
            get {
                return ResourceManager.GetString("Authoring_InvalidManualInstructionLocalizationIndex", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Choice parameter {0} is invalid. It allows multiple values (&apos;AllowMultipleValues=true&apos;), while some of the configured choices contain separator characters ({1}). Invalid choices: {2}.
        /// </summary>
        internal static string Authoring_InvalidMultichoiceSymbol {
            get {
                return ResourceManager.GetString("Authoring_InvalidMultichoiceSymbol", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Post action(s) with id(s) &apos;{0}&apos; specified in the localization file do not exist in the template.json file. Remove the localized strings from the localization file..
        /// </summary>
        internal static string Authoring_InvalidPostActionLocalizationIndex {
            get {
                return ResourceManager.GetString("Authoring_InvalidPostActionLocalizationIndex", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;{0}&apos; could not be parsed as a regular expression.
        /// </summary>
        internal static string Authoring_InvalidRegex {
            get {
                return ResourceManager.GetString("Authoring_InvalidRegex", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to One or more postActions have a malformed or missing manualInstructions value..
        /// </summary>
        internal static string Authoring_MalformedPostActionManualInstructions {
            get {
                return ResourceManager.GetString("Authoring_MalformedPostActionManualInstructions", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;id&apos; of the manual instruction &apos;{0}&apos; at index {1} is not unique. Only the first manual instruction that uses this id will be localized..
        /// </summary>
        internal static string Authoring_ManualInstructionIdIsNotUnique {
            get {
                return ResourceManager.GetString("Authoring_ManualInstructionIdIsNotUnique", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Missing &apos;{0}&apos;..
        /// </summary>
        internal static string Authoring_MissingValue {
            get {
                return ResourceManager.GetString("Authoring_MissingValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A non-bool DataType was specified for a regexMatch type symbol.
        /// </summary>
        internal static string Authoring_NonBoolDataTypeForRegexMatch {
            get {
                return ResourceManager.GetString("Authoring_NonBoolDataTypeForRegexMatch", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [{0}]: id of the post action &apos;{1}&apos; at index &apos;{2}&apos; is not unique. Only the first post action that uses this id will be localized..
        /// </summary>
        internal static string Authoring_PostActionIdIsNotUnique {
            get {
                return ResourceManager.GetString("Authoring_PostActionIdIsNotUnique", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [{0}]: Post action at index &apos;{1}&apos; should have an &apos;actionId&apos; to declare the action to be executed..
        /// </summary>
        internal static string Authoring_PostActionMustHaveActionId {
            get {
                return ResourceManager.GetString("Authoring_PostActionMustHaveActionId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Source &apos;{0}&apos; in template does not exist..
        /// </summary>
        internal static string Authoring_SourceDoesNotExist {
            get {
                return ResourceManager.GetString("Authoring_SourceDoesNotExist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Source location &apos;{0}&apos; is outside the specified install source location..
        /// </summary>
        internal static string Authoring_SourceIsOutsideInstallSource {
            get {
                return ResourceManager.GetString("Authoring_SourceIsOutsideInstallSource", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Source must be a directory, but &apos;{0}&apos; is a file..
        /// </summary>
        internal static string Authoring_SourceMustBeDirectory {
            get {
                return ResourceManager.GetString("Authoring_SourceMustBeDirectory", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The template configuration {0} is missing common information:.
        /// </summary>
        internal static string Authoring_TemplateMissingCommonInformation {
            get {
                return ResourceManager.GetString("Authoring_TemplateMissingCommonInformation", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to load template from {0}:.
        /// </summary>
        internal static string Authoring_TemplateNotInstalled {
            get {
                return ResourceManager.GetString("Authoring_TemplateNotInstalled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to load template from {0}.
        ///Details: {1}.
        /// </summary>
        internal static string Authoring_TemplateNotInstalled_Message {
            get {
                return ResourceManager.GetString("Authoring_TemplateNotInstalled_Message", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The template root is outside the specified install source location..
        /// </summary>
        internal static string Authoring_TemplateRootOutsideInstallSource {
            get {
                return ResourceManager.GetString("Authoring_TemplateRootOutsideInstallSource", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to read parse localization file {0}, it will be skipped from further processing..
        /// </summary>
        internal static string LocalizationModelDeserializer_Error_FailedToParse {
            get {
                return ResourceManager.GetString("LocalizationModelDeserializer_Error_FailedToParse", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Variable [{0}] already added with value [{1}]. Cannot add it as implicit variable with value of self..
        /// </summary>
        internal static string RunnableProjectGenerator_CannotAddImplicitChoice {
            get {
                return ResourceManager.GetString("RunnableProjectGenerator_CannotAddImplicitChoice", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Attempt to pass result of external evaluation of parameters conditions for parameter(s) that do not have appropriate condition set in template (IsEnabledCondition or IsRequiredCondition): {0}.
        /// </summary>
        internal static string RunnableProjectGenerator_Error_ExternalConditionMismatch {
            get {
                return ResourceManager.GetString("RunnableProjectGenerator_Error_ExternalConditionMismatch", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Parameter conditions contain cyclic dependency: [{0}] that is preventing deterministic evaluation.
        /// </summary>
        internal static string RunnableProjectGenerator_Error_ParamsCycle {
            get {
                return ResourceManager.GetString("RunnableProjectGenerator_Error_ParamsCycle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unexpected internal error - unable to perform topological sort of parameter dependencies that do not appear to have a cyclic dependencies..
        /// </summary>
        internal static string RunnableProjectGenerator_Error_TopologicalSort {
            get {
                return ResourceManager.GetString("RunnableProjectGenerator_Error_TopologicalSort", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Template configuration must be a file, but {0} is not a file..
        /// </summary>
        internal static string RunnableProjectGenerator_Exception_ConfigShouldBeFile {
            get {
                return ResourceManager.GetString("RunnableProjectGenerator_Exception_ConfigShouldBeFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Template localization configuration must be a file, but {0} is not a file..
        /// </summary>
        internal static string RunnableProjectGenerator_Exception_LocaleConfigShouldBeFile {
            get {
                return ResourceManager.GetString("RunnableProjectGenerator_Exception_LocaleConfigShouldBeFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The template configuration {0} is not valid..
        /// </summary>
        internal static string RunnableProjectGenerator_Exception_TemplateValidationFailed {
            get {
                return ResourceManager.GetString("RunnableProjectGenerator_Exception_TemplateValidationFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The template is not compatible with generator version: allowed versions: {0}, generator version: {1}..
        /// </summary>
        internal static string RunnableProjectGenerator_Exception_TemplateVersionNotSupported {
            get {
                return ResourceManager.GetString("RunnableProjectGenerator_Exception_TemplateVersionNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Localization file {0} is not compatible with base configuration {1}, and will be skipped..
        /// </summary>
        internal static string RunnableProjectGenerator_Warning_LocFileSkipped {
            get {
                return ResourceManager.GetString("RunnableProjectGenerator_Warning_LocFileSkipped", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Parameter conditions contain cyclic dependency: [{0}]. With current values of parameters it&apos;s possible to deterministically evaluate parameters - so proceeding further. However template should be reviewed as instantiation with different parameters can lead to error..
        /// </summary>
        internal static string RunnableProjectGenerator_Warning_ParamsCycle {
            get {
                return ResourceManager.GetString("RunnableProjectGenerator_Warning_ParamsCycle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to load additional configuration file {0}, the file does not exist..
        /// </summary>
        internal static string SimpleConfigModel_AuthoringException_MergeConfiguration_FileNotFound {
            get {
                return ResourceManager.GetString("SimpleConfigModel_AuthoringException_MergeConfiguration_FileNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to load additional configuration file {0}. Additional configuration file names must end with &apos;.{1}&apos;..
        /// </summary>
        internal static string SimpleConfigModel_AuthoringException_MergeConfiguration_InvalidFileName {
            get {
                return ResourceManager.GetString("SimpleConfigModel_AuthoringException_MergeConfiguration_InvalidFileName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;{0}&apos; should contain objects..
        /// </summary>
        internal static string SimpleConfigModel_Error_Constraints_InvalidSyntax {
            get {
                return ResourceManager.GetString("SimpleConfigModel_Error_Constraints_InvalidSyntax", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Constraint definition &apos;{0}&apos; does not contain mandatory property &apos;{1}&apos;..
        /// </summary>
        internal static string SimpleConfigModel_Error_Constraints_MissingType {
            get {
                return ResourceManager.GetString("SimpleConfigModel_Error_Constraints_MissingType", resourceCulture);
            }
        }
    }
}
