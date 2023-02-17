﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.TemplateEngine.Edge {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.TemplateEngine.Edge.LocalizableStrings", typeof(LocalizableStrings).Assembly);
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
        ///   Looks up a localized string similar to Parameter conditions contain cyclic dependency: [{0}] that is preventing deterministic evaluation..
        /// </summary>
        internal static string ConditionEvaluation_Error_CyclicDependency {
            get {
                return ResourceManager.GetString("ConditionEvaluation_Error_CyclicDependency", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to evaluate condition {0} on parameter {1} (condition text: {2}, evaluation error: {3}) - condition might be malformed or referenced parameters do not have default nor explicit values..
        /// </summary>
        internal static string ConditionEvaluation_Error_MismatchedCondition {
            get {
                return ResourceManager.GetString("ConditionEvaluation_Error_MismatchedCondition", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unexpected internal error - unable to perform topological sort of parameter dependencies that do not appear to have a cyclic dependencies..
        /// </summary>
        internal static string ConditionEvaluation_Error_TopologicalSort {
            get {
                return ResourceManager.GetString("ConditionEvaluation_Error_TopologicalSort", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Parameter conditions contain cyclic dependency: [{0}]. With current values of parameters it&apos;s possible to deterministically evaluate parameters - so proceeding further. However template should be reviewed as instantiation with different parameters can lead to error..
        /// </summary>
        internal static string ConditionEvaluation_Warning_CyclicDependency {
            get {
                return ResourceManager.GetString("ConditionEvaluation_Warning_CyclicDependency", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;{0}&apos; should not contain empty items.
        /// </summary>
        internal static string Constaint_Error_ArgumentHasEmptyString {
            get {
                return ResourceManager.GetString("Constaint_Error_ArgumentHasEmptyString", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Argument(s) were not specified. At least one argument should be specified..
        /// </summary>
        internal static string Constraint_Error_ArgumentsNotSpecified {
            get {
                return ResourceManager.GetString("Constraint_Error_ArgumentsNotSpecified", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;{0}&apos; does not contain valid items..
        /// </summary>
        internal static string Constraint_Error_ArrayHasNoObjects {
            get {
                return ResourceManager.GetString("Constraint_Error_ArrayHasNoObjects", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;{0}&apos; is not a valid JSON..
        /// </summary>
        internal static string Constraint_Error_InvalidJson {
            get {
                return ResourceManager.GetString("Constraint_Error_InvalidJson", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;{0}&apos; should be an array of objects..
        /// </summary>
        internal static string Constraint_Error_InvalidJsonArray_Objects {
            get {
                return ResourceManager.GetString("Constraint_Error_InvalidJsonArray_Objects", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;{0}&apos; is not a valid JSON array..
        /// </summary>
        internal static string Constraint_Error_InvalidJsonType_Array {
            get {
                return ResourceManager.GetString("Constraint_Error_InvalidJsonType_Array", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;{0}&apos; is not a valid JSON string or array..
        /// </summary>
        internal static string Constraint_Error_InvalidJsonType_StringOrArray {
            get {
                return ResourceManager.GetString("Constraint_Error_InvalidJsonType_StringOrArray", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;{0}&apos; is not a valid version or version range..
        /// </summary>
        internal static string Constraint_Error_InvalidVersion {
            get {
                return ResourceManager.GetString("Constraint_Error_InvalidVersion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Check the constraint configuration in template.json..
        /// </summary>
        internal static string Constraint_WrongConfigurationCTA {
            get {
                return ResourceManager.GetString("Constraint_WrongConfigurationCTA", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Environment variables.
        /// </summary>
        internal static string EnvironmentVariablesBindSource_Name {
            get {
                return ResourceManager.GetString("EnvironmentVariablesBindSource_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Attempt to pass result of external evaluation of parameters conditions for parameter(s) that do not have appropriate condition set in template (IsEnabled or IsRequired attributes not populated with condition) or a failure to pass the condition results for parameters with condition(s) in template. Offending parameters: {0}..
        /// </summary>
        internal static string EvaluatedInputDataSet_Error_MismatchedConditions {
            get {
                return ResourceManager.GetString("EvaluatedInputDataSet_Error_MismatchedConditions", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Attempt to pass result of external evaluation of parameters conditions for parameter(s) that do not have appropriate condition set in template (IsEnabled or IsRequired attributes not populated with condition) or a failure to pass the condition results for parameters with condition(s) in template. Offending parameter(s): {0}..
        /// </summary>
        internal static string EvaluatedInputParameterData_Error_ConditionsInvalid {
            get {
                return ResourceManager.GetString("EvaluatedInputParameterData_Error_ConditionsInvalid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The folder {0} doesn&apos;t exist..
        /// </summary>
        internal static string FolderInstaller_InstallResult_Error_FolderDoesNotExist {
            get {
                return ResourceManager.GetString("FolderInstaller_InstallResult_Error_FolderDoesNotExist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Check the constraint configuration in template.json..
        /// </summary>
        internal static string Generic_Constraint_WrongConfigurationCTA {
            get {
                return ResourceManager.GetString("Generic_Constraint_WrongConfigurationCTA", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to latest version.
        /// </summary>
        internal static string Generic_LatestVersion {
            get {
                return ResourceManager.GetString("Generic_LatestVersion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to version {0}.
        /// </summary>
        internal static string Generic_Version {
            get {
                return ResourceManager.GetString("Generic_Version", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The package with the same identity is already installed: {0}. Please consider uninstalling the conflicting template..
        /// </summary>
        internal static string GlobalSettingsTemplatePackageProvider_InstallResult_Error_DuplicatedIdentity {
            get {
                return ResourceManager.GetString("GlobalSettingsTemplatePackageProvider_InstallResult_Error_DuplicatedIdentity", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} can be installed by several installers. Specify the installer name to be used..
        /// </summary>
        internal static string GlobalSettingsTemplatePackageProvider_InstallResult_Error_MultipleInstallersCanBeUsed {
            get {
                return ResourceManager.GetString("GlobalSettingsTemplatePackageProvider_InstallResult_Error_MultipleInstallersCanBe" +
                        "Used", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} is already installed..
        /// </summary>
        internal static string GlobalSettingsTemplatePackageProvider_InstallResult_Error_PackageAlreadyInstalled {
            get {
                return ResourceManager.GetString("GlobalSettingsTemplatePackageProvider_InstallResult_Error_PackageAlreadyInstalled" +
                        "", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} cannot be installed..
        /// </summary>
        internal static string GlobalSettingsTemplatePackageProvider_InstallResult_Error_PackageCannotBeInstalled {
            get {
                return ResourceManager.GetString("GlobalSettingsTemplatePackageProvider_InstallResult_Error_PackageCannotBeInstalle" +
                        "d", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} is already installed, it will be replaced with {1}..
        /// </summary>
        internal static string GlobalSettingsTemplatePackagesProvider_Info_PackageAlreadyInstalled {
            get {
                return ResourceManager.GetString("GlobalSettingsTemplatePackagesProvider_Info_PackageAlreadyInstalled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} was successfully uninstalled..
        /// </summary>
        internal static string GlobalSettingsTemplatePackagesProvider_Info_PackageUninstalled {
            get {
                return ResourceManager.GetString("GlobalSettingsTemplatePackagesProvider_Info_PackageUninstalled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;{0}&apos; does not have mandatory property &apos;{1}&apos;..
        /// </summary>
        internal static string HostConstraint_Error_MissingMandatoryProperty {
            get {
                return ResourceManager.GetString("HostConstraint_Error_MissingMandatoryProperty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Running template on {0} (version: {1}) is not supported, supported hosts is/are: {2}..
        /// </summary>
        internal static string HostConstraint_Message_Restricted {
            get {
                return ResourceManager.GetString("HostConstraint_Message_Restricted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Template engine host.
        /// </summary>
        internal static string HostConstraint_Name {
            get {
                return ResourceManager.GetString("HostConstraint_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Host defined parameters.
        /// </summary>
        internal static string HostParametersBindSource_Name {
            get {
                return ResourceManager.GetString("HostParametersBindSource_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to load the NuGet source {0}..
        /// </summary>
        internal static string NuGetApiPackageManager_Error_FailedToLoadSource {
            get {
                return ResourceManager.GetString("NuGetApiPackageManager_Error_FailedToLoadSource", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to load NuGet sources configured for the folder {0}..
        /// </summary>
        internal static string NuGetApiPackageManager_Error_FailedToLoadSources {
            get {
                return ResourceManager.GetString("NuGetApiPackageManager_Error_FailedToLoadSources", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to read package information from NuGet source {0}..
        /// </summary>
        internal static string NuGetApiPackageManager_Error_FailedToReadPackage {
            get {
                return ResourceManager.GetString("NuGetApiPackageManager_Error_FailedToReadPackage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to File {0} already exists..
        /// </summary>
        internal static string NuGetApiPackageManager_Error_FileAlreadyExists {
            get {
                return ResourceManager.GetString("NuGetApiPackageManager_Error_FileAlreadyExists", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No NuGet sources are defined or enabled..
        /// </summary>
        internal static string NuGetApiPackageManager_Error_NoSources {
            get {
                return ResourceManager.GetString("NuGetApiPackageManager_Error_NoSources", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to remove {0} after failed download. Remove the file manually if it exists..
        /// </summary>
        internal static string NuGetApiPackageManager_Warning_FailedToDelete {
            get {
                return ResourceManager.GetString("NuGetApiPackageManager_Warning_FailedToDelete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to download {0} from NuGet feed {1}..
        /// </summary>
        internal static string NuGetApiPackageManager_Warning_FailedToDownload {
            get {
                return ResourceManager.GetString("NuGetApiPackageManager_Warning_FailedToDownload", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to load NuGet source {0}: the source is not valid. It will be skipped in further processing..
        /// </summary>
        internal static string NuGetApiPackageManager_Warning_FailedToLoadSource {
            get {
                return ResourceManager.GetString("NuGetApiPackageManager_Warning_FailedToLoadSource", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} is not found in NuGet feeds {1}..
        /// </summary>
        internal static string NuGetApiPackageManager_Warning_PackageNotFound {
            get {
                return ResourceManager.GetString("NuGetApiPackageManager_Warning_PackageNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to copy package {0} to {1}..
        /// </summary>
        internal static string NuGetInstaller_Error_CopyFailed {
            get {
                return ResourceManager.GetString("NuGetInstaller_Error_CopyFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to read content of package {0}..
        /// </summary>
        internal static string NuGetInstaller_Error_FailedToReadPackage {
            get {
                return ResourceManager.GetString("NuGetInstaller_Error_FailedToReadPackage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to File {0} already exists..
        /// </summary>
        internal static string NuGetInstaller_Error_FileAlreadyExists {
            get {
                return ResourceManager.GetString("NuGetInstaller_Error_FileAlreadyExists", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to download {0} from {1}..
        /// </summary>
        internal static string NuGetInstaller_InstallResut_Error_DownloadFailed {
            get {
                return ResourceManager.GetString("NuGetInstaller_InstallResut_Error_DownloadFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to install the package {0}.
        ///Details: {1}..
        /// </summary>
        internal static string NuGetInstaller_InstallResut_Error_InstallGeneric {
            get {
                return ResourceManager.GetString("NuGetInstaller_InstallResut_Error_InstallGeneric", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The install request {0} cannot be processed by installer {1}..
        /// </summary>
        internal static string NuGetInstaller_InstallResut_Error_InstallRequestNotSupported {
            get {
                return ResourceManager.GetString("NuGetInstaller_InstallResut_Error_InstallRequestNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The NuGet package {0} is invalid..
        /// </summary>
        internal static string NuGetInstaller_InstallResut_Error_InvalidPackage {
            get {
                return ResourceManager.GetString("NuGetInstaller_InstallResut_Error_InvalidPackage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The configured NuGet sources are invalid: {0}..
        /// </summary>
        internal static string NuGetInstaller_InstallResut_Error_InvalidSources {
            get {
                return ResourceManager.GetString("NuGetInstaller_InstallResut_Error_InvalidSources", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No NuGet sources are configured..
        /// </summary>
        internal static string NuGetInstaller_InstallResut_Error_InvalidSources_None {
            get {
                return ResourceManager.GetString("NuGetInstaller_InstallResut_Error_InvalidSources_None", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The operation was cancelled..
        /// </summary>
        internal static string NuGetInstaller_InstallResut_Error_OperationCancelled {
            get {
                return ResourceManager.GetString("NuGetInstaller_InstallResut_Error_OperationCancelled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} was not found in NuGet feeds {1}..
        /// </summary>
        internal static string NuGetInstaller_InstallResut_Error_PackageNotFound {
            get {
                return ResourceManager.GetString("NuGetInstaller_InstallResut_Error_PackageNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The package {0} is not supported by installer {1}..
        /// </summary>
        internal static string NuGetInstaller_InstallResut_Error_PackageNotSupported {
            get {
                return ResourceManager.GetString("NuGetInstaller_InstallResut_Error_PackageNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to uninstall the package {0}.
        ///Details: {1}..
        /// </summary>
        internal static string NuGetInstaller_InstallResut_Error_UninstallGeneric {
            get {
                return ResourceManager.GetString("NuGetInstaller_InstallResut_Error_UninstallGeneric", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to check the update for the package {0}.
        ///Details: {1}..
        /// </summary>
        internal static string NuGetInstaller_InstallResut_Error_UpdateCheckGeneric {
            get {
                return ResourceManager.GetString("NuGetInstaller_InstallResut_Error_UpdateCheckGeneric", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;{0}&apos; is not a valid operating system name. Allowed values are: {1}..
        /// </summary>
        internal static string OSConstraint_Error_InvalidOSName {
            get {
                return ResourceManager.GetString("OSConstraint_Error_InvalidOSName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Running template on {0} is not supported, supported OS is/are: {1}..
        /// </summary>
        internal static string OSConstraint_Message_Restricted {
            get {
                return ResourceManager.GetString("OSConstraint_Message_Restricted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Operating System.
        /// </summary>
        internal static string OSConstraint_Name {
            get {
                return ResourceManager.GetString("OSConstraint_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Template package location {0} is not supported, or doesn&apos;t exist..
        /// </summary>
        internal static string Scanner_Error_TemplatePackageLocationIsNotSupported {
            get {
                return ResourceManager.GetString("Scanner_Error_TemplatePackageLocationIsNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;{0}&apos; is not a valid semver version..
        /// </summary>
        internal static string SdkConstraint_Error_InvalidVersion {
            get {
                return ResourceManager.GetString("SdkConstraint_Error_InvalidVersion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Multiple &apos;ISdkInfoProvider&apos; components provided by host ({0}), therefore &apos;SdkVersionConstraint&apos; cannot be properly initialized..
        /// </summary>
        internal static string SdkConstraint_Error_MismatchedProviders {
            get {
                return ResourceManager.GetString("SdkConstraint_Error_MismatchedProviders", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No &apos;ISdkInfoProvider&apos; component provided by host. &apos;SdkVersionConstraint&apos; cannot be properly initialized..
        /// </summary>
        internal static string SdkConstraint_Error_MissingProvider {
            get {
                return ResourceManager.GetString("SdkConstraint_Error_MissingProvider", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Running template on current .NET SDK version ({0}) is unsupported. Supported version(s): {1}.
        /// </summary>
        internal static string SdkConstraint_Message_Restricted {
            get {
                return ResourceManager.GetString("SdkConstraint_Message_Restricted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to .NET SDK version.
        /// </summary>
        internal static string SdkVersionConstraint_Name {
            get {
                return ResourceManager.GetString("SdkVersionConstraint_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The constraint &apos;{0}&apos; failed to be evaluated for the args &apos;{1}&apos;, details: {2}.
        /// </summary>
        internal static string TemplateConstraintManager_Error_FailedToEvaluate {
            get {
                return ResourceManager.GetString("TemplateConstraintManager_Error_FailedToEvaluate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The constraint &apos;{0}&apos; failed to initialize: {1}.
        /// </summary>
        internal static string TemplateConstraintManager_Error_FailedToInitialize {
            get {
                return ResourceManager.GetString("TemplateConstraintManager_Error_FailedToInitialize", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The constraint &apos;{0}&apos; is unknown..
        /// </summary>
        internal static string TemplateConstraintManager_Error_UnknownType {
            get {
                return ResourceManager.GetString("TemplateConstraintManager_Error_UnknownType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not load template..
        /// </summary>
        internal static string TemplateCreator_TemplateCreationResult_Error_CouldNotLoadTemplate {
            get {
                return ResourceManager.GetString("TemplateCreator_TemplateCreationResult_Error_CouldNotLoadTemplate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to create template.
        ///Details: {0}.
        /// </summary>
        internal static string TemplateCreator_TemplateCreationResult_Error_CreationFailed {
            get {
                return ResourceManager.GetString("TemplateCreator_TemplateCreationResult_Error_CreationFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Destructive changes detected..
        /// </summary>
        internal static string TemplateCreator_TemplateCreationResult_Error_DestructiveChanges {
            get {
                return ResourceManager.GetString("TemplateCreator_TemplateCreationResult_Error_DestructiveChanges", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to create template: the template name is not specified. Template configuration does not configure a default name that can be used when name is not specified. Specify the name for the template when instantiating or configure a default name in the template configuration..
        /// </summary>
        internal static string TemplateCreator_TemplateCreationResult_Error_NoDefaultName {
            get {
                return ResourceManager.GetString("TemplateCreator_TemplateCreationResult_Error_NoDefaultName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to load host data in {0} at {1}..
        /// </summary>
        internal static string TemplateInfo_Warning_FailedToReadHostData {
            get {
                return ResourceManager.GetString("TemplateInfo_Warning_FailedToReadHostData", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to retrieve template packages from provider &apos;{0}&apos;.
        ///Details: {1}.
        /// </summary>
        internal static string TemplatePackageManager_Error_FailedToGetTemplatePackages {
            get {
                return ResourceManager.GetString("TemplatePackageManager_Error_FailedToGetTemplatePackages", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to scan {0}.
        ///Details: {1}.
        /// </summary>
        internal static string TemplatePackageManager_Error_FailedToScan {
            get {
                return ResourceManager.GetString("TemplatePackageManager_Error_FailedToScan", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to store template cache, details: {0}
        ///Template cache will be recreated on the next run..
        /// </summary>
        internal static string TemplatePackageManager_Error_FailedToStoreCache {
            get {
                return ResourceManager.GetString("TemplatePackageManager_Error_FailedToStoreCache", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The template {0} has the identity conflict with an already existing template {1}. The template {1} will be overwritten..
        /// </summary>
        internal static string TemplatePackageManager_Warning_DetectedTemplatesIdentityConflict {
            get {
                return ResourceManager.GetString("TemplatePackageManager_Warning_DetectedTemplatesIdentityConflict", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Multiple &apos;IWorkloadsInfoProvider&apos; components provided by host ({0}), therefore &apos;WorkloadConstraint&apos; cannot be properly initialized..
        /// </summary>
        internal static string WorkloadConstraint_Error_MismatchedProviders {
            get {
                return ResourceManager.GetString("WorkloadConstraint_Error_MismatchedProviders", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No &apos;IWorkloadsInfoProvider&apos; component provided by host. &apos;WorkloadConstraint&apos; cannot be properly initialized..
        /// </summary>
        internal static string WorkloadConstraint_Error_MissingProvider {
            get {
                return ResourceManager.GetString("WorkloadConstraint_Error_MissingProvider", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Running template is not supported - required optional workload(s) not installed. Supported workload(s): {0}. Currently installed optional workloads: {1}.
        /// </summary>
        internal static string WorkloadConstraint_Message_Restricted {
            get {
                return ResourceManager.GetString("WorkloadConstraint_Message_Restricted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Workload.
        /// </summary>
        internal static string WorkloadConstraint_Name {
            get {
                return ResourceManager.GetString("WorkloadConstraint_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;IWorkloadsInfoProvider&apos; component provided by host provided some duplicated workloads (duplicates: {0}). Duplicates will be skipped..
        /// </summary>
        internal static string WorkloadConstraint_Warning_DuplicateWorkloads {
            get {
                return ResourceManager.GetString("WorkloadConstraint_Warning_DuplicateWorkloads", resourceCulture);
            }
        }
    }
}
