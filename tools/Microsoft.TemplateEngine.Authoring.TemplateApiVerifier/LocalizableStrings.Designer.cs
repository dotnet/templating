﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.TemplateEngine.Authoring.TemplateApiVerifier {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.TemplateEngine.Authoring.TemplateApiVerifier.LocalizableStrings", typeof(LocalizableStrings).Assembly);
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
        ///   Looks up a localized string similar to The template &quot;{0}&quot; was created successfully..
        /// </summary>
        internal static string CreateSuccessful {
            get {
                return ResourceManager.GetString("CreateSuccessful", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Template config: {0} doesn&apos;t exist. When using &apos;WithInstantiationThroughTemplateCreatorApi&apos; the &apos;TemplatePath&apos; parameter must specify path to the template.json or to the root of template (containing {1})..
        /// </summary>
        internal static string Error_ConfigDoesntExist {
            get {
                return ResourceManager.GetString("Error_ConfigDoesntExist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Template configuration file could not be retrieved from configured mount point..
        /// </summary>
        internal static string Error_ConfigRetrieval {
            get {
                return ResourceManager.GetString("Error_ConfigRetrieval", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;DotnetExecutablePath&apos; parameter must not be specified when using &apos;WithInstantiationThroughTemplateCreatorApi&apos;.
        /// </summary>
        internal static string Error_DotnetPath {
            get {
                return ResourceManager.GetString("Error_DotnetPath", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to install template: {0}, details:{1}..
        /// </summary>
        internal static string Error_InstallFail {
            get {
                return ResourceManager.GetString("Error_InstallFail", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No packages fetched after installation..
        /// </summary>
        internal static string Error_NoPackages {
            get {
                return ResourceManager.GetString("Error_NoPackages", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;TemplateSpecificArgs&apos; parameter must not be specified when using WithInstantiationThroughTemplateCreatorApi. Parameters should be passed via the argument of &apos;WithInstantiationThroughTemplateCreatorApi&apos;.
        /// </summary>
        internal static string Error_TemplateArgsDisalowed {
            get {
                return ResourceManager.GetString("Error_TemplateArgsDisalowed", resourceCulture);
            }
        }
    }
}
