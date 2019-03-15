// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TemplateEngine.Abstractions.Json;

namespace dotnet_new3
{
    internal class JsonDomFactory
#if !NETCOREAPP3_0
        : NewtonsoftJsonDocumentObjectModel
#else
        : SystemTextJsonDocumentObjectModel
#endif
    {
    }
}
