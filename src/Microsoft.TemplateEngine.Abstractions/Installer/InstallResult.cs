// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.TemplatePackage;

namespace Microsoft.TemplateEngine.Abstractions.Installer
{
    /// <summary>
    /// Represents the result of template package installation using <see cref="IInstaller.InstallAsync"/>.
    /// </summary>
    public sealed class InstallResult : InstallerOperationResult
    {
        private InstallResult(InstallRequest request, IManagedTemplatePackage templatePackage, Dictionary<Uri, int>? vulnerabilities = null)
            : base(templatePackage)
        {
            InstallRequest = request;
            Vulnerabilities = vulnerabilities;
        }

        private InstallResult(InstallRequest request, InstallerErrorCode error, string errorMessage, Dictionary<Uri, int>? vulnerabilities)
             : base(error, errorMessage)
        {
            InstallRequest = request;
            Vulnerabilities = vulnerabilities;
        }

        /// <summary>
        /// <see cref="InstallRequest"/> processed by <see cref="IInstaller.InstallAsync"/> operation.
        /// </summary>
        public InstallRequest InstallRequest { get; private set; }

        /// <summary>
        /// Vulnerabilities from installed package.
        /// </summary>
        /// It is a dictionary, as we don't want to add the NuGet Api package to this project.
        public Dictionary<Uri, int>? Vulnerabilities { get; private set; }

        /// <summary>
        /// Creates successful result for the operation.
        /// </summary>
        /// <param name="request">the processed installation request.</param>
        /// <param name="templatePackage">the installed <see cref="IManagedTemplatePackage"/>.</param>
        /// <param name="vulnerabilities">Package vulnerabilities associated with the installation request.</param>
        /// <returns></returns>
        public static InstallResult CreateSuccess(InstallRequest request, IManagedTemplatePackage templatePackage, Dictionary<Uri, int>? vulnerabilities = null)
        {
            return new InstallResult(request, templatePackage, vulnerabilities);
        }

        /// <summary>
        /// Creates failure result for the operation.
        /// </summary>
        /// <param name="request">the processed installation request.</param>
        /// <param name="error">error code, see <see cref="InstallerErrorCode"/> for details.</param>
        /// <param name="localizedFailureMessage">detailed error message.</param>
        /// <param name="vulnerabilities">Package vulnerabilities associated with the installation request. </param>
        /// <returns></returns>
        public static InstallResult CreateFailure(InstallRequest request, InstallerErrorCode error, string localizedFailureMessage, Dictionary<Uri, int>? vulnerabilities = null)
        {
            return new InstallResult(request, error, localizedFailureMessage, vulnerabilities);
        }
    }
}
