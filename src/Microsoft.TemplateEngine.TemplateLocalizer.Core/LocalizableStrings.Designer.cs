﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.TemplateEngine.TemplateLocalizer.Core {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.TemplateEngine.TemplateLocalizer.Core.LocalizableStrings", typeof(LocalizableStrings).Assembly);
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
        ///   Looks up a localized string similar to The following element in the template.json will not be included in the localizations because it does not match any of the rules for localizable elements: {0}.
        /// </summary>
        internal static string stringExtractor_log_commandDebugElementExcluded {
            get {
                return ResourceManager.GetString("stringExtractor_log_commandDebugElementExcluded", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Adding into localizable strings: {0}.
        /// </summary>
        internal static string stringExtractor_log_commandElementAdded {
            get {
                return ResourceManager.GetString("stringExtractor_log_commandElementAdded", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The following element in the template.json will be skipped since it was already added to the list of localizable strings: {0}.
        /// </summary>
        internal static string stringExtractor_log_commandElementAlreadyAdded {
            get {
                return ResourceManager.GetString("stringExtractor_log_commandElementAlreadyAdded", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &quot;Failed to read the existing strings from &quot;{0}&quot;.
        /// </summary>
        internal static string stringUpdater_log_commandFailedToReadLocFile {
            get {
                return ResourceManager.GetString("stringUpdater_log_commandFailedToReadLocFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Loading existing localizations from file &quot;{0}&quot;.
        /// </summary>
        internal static string stringUpdater_log_commandLoadingLocFile {
            get {
                return ResourceManager.GetString("stringUpdater_log_commandLoadingLocFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &quot;Opening the following templatestrings.json file for writing: &quot;{0}&quot;.
        /// </summary>
        internal static string stringUpdater_log_commandOpeningTemplatesJson {
            get {
                return ResourceManager.GetString("stringUpdater_log_commandOpeningTemplatesJson", resourceCulture);
            }
        }
    }
}
