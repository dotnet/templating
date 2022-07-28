// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TemplateEngine.Abstractions.Parameters
{
    public enum DataSource
    {
        /// <summary>
        /// Value was not set.
        /// </summary>
        NoSource,

        /// <summary>
        /// Those are values supplied by the host. This usually means value(s) was/were set by user.
        /// </summary>
        Host,

        /// <summary>
        /// Value obtained via <see cref="ITemplateEngineHost.TryGetHostParamDefault"/>.
        /// </summary>
        HostDefault,

        /// <summary>
        /// Value obtained via <see cref="ITemplateEngineHost.OnParameterError"/>.
        /// </summary>
        HostOnError,

        /// <summary>
        /// Value from template - <see cref="ITemplateParameter.DefaultValue"/>.
        /// </summary>
        Default,

        /// <summary>
        /// Value from template - <see cref="ITemplateParameter.DefaultIfOptionWithoutValue"/>.
        /// </summary>
        DefaultIfNoValue,

        /// <summary>
        /// This corresponds to Name implicit parameter value.
        /// </summary>
        NameParameter,
    }
}
