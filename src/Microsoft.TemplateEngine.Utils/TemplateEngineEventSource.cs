// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;

namespace Microsoft.TemplateEngine.Utils
{
    /// <summary>
    /// This captures information of how various key methods of TemplateEngine ran.
    /// </summary>
    [EventSource(Name = "Microsoft-TemplateEngine")]
    public sealed class TemplateEngineEventSource : EventSource
    {
        private TemplateEngineEventSource() { }

        /// <summary>
        /// Define the singleton instance of the event source.
        /// </summary>
        public static TemplateEngineEventSource Log { get; } = new TemplateEngineEventSource();

        #region CLI Events

        [Event(1, Keywords = Keywords.All)]
        public void New3Command_RunStart()
        {
            WriteEvent(1);
        }

        [Event(2, Keywords = Keywords.All)]
        public void New3Command_RunStop()
        {
            WriteEvent(2);
        }

        [Event(3, Keywords = Keywords.All)]
        public void New3Command_ExecuteStart()
        {
            WriteEvent(3);
        }

        [Event(4, Keywords = Keywords.All)]
        public void New3Command_ExecuteStop()
        {
            WriteEvent(4);
        }

        [Event(5, Keywords = Keywords.All)]
        public void ParseArgsStart()
        {
            WriteEvent(5);
        }

        [Event(6, Keywords = Keywords.All)]
        public void ParseArgsStop()
        {
            WriteEvent(6);
        }

        [Event(7, Keywords = Keywords.All)]
        public void CoordinateInvocationOrAcquisitionAsyncStart()
        {
            WriteEvent(7);
        }

        [Event(8, Keywords = Keywords.All)]
        public void CoordinateInvocationOrAcquisitionAsyncStop()
        {
            WriteEvent(8);
        }

        #endregion

        #region EdgeEvents

        [Event(101, Keywords = Keywords.All)]
        public void TemplateCreator_InstantiateStart()
        {
            WriteEvent(101);
        }

        [Event(102, Keywords = Keywords.All)]
        public void TemplateCreator_InstantiateStop()
        {
            WriteEvent(102);
        }

        [Event(103, Keywords = Keywords.All)]
        public void SettingsLoader_LoadTemplateStart(string identity)
        {
            WriteEvent(103, identity);
        }

        [Event(104, Keywords = Keywords.All)]
        public void SettingsLoader_LoadTemplateStop(bool success)
        {
            WriteEvent(104, success);
        }

        [Event(105, Keywords = Keywords.All)]
        public void SettingsLoader_EnsureLoadedStart()
        {
            WriteEvent(105);
        }

        [Event(106, Keywords = Keywords.All)]
        public void SettingsLoader_EnsureLoadedStop()
        {
            WriteEvent(106);
        }

        [Event(107, Keywords = Keywords.All)]
        public void GlobalSettingsProvider_GetPackagesStart()
        {
            WriteEvent(107);
        }

        [Event(108, Keywords = Keywords.All)]
        public void GlobalSettingsProvider_GetPackagesStop()
        {
            WriteEvent(108);
        }

        [Event(109, Keywords = Keywords.All)]
        public void Scanner_ScanStart(string path)
        {
            WriteEvent(109, path);
        }

        [Event(110, Keywords = Keywords.All)]
        public void Scanner_ScanStop()
        {
            WriteEvent(110);
        }

        [Event(111, Keywords = Keywords.All)]
        public void SettingsLoader_RebuildCacheStart(bool force)
        {
            WriteEvent(111, force ? 1 : 0);
        }

        [Event(112, Keywords = Keywords.All)]
        public void SettingsLoader_RebuildCacheStop(bool rebuilt)
        {
            WriteEvent(112, rebuilt);
        }

        [Event(113, Keywords = Keywords.All)]
        public void SettingsLoader_TemplateCacheParsingStart()
        {
            WriteEvent(113);
        }

        [Event(114, Keywords = Keywords.All)]
        public void SettingsLoader_TemplateCacheParsingStop()
        {
            WriteEvent(114);
        }

        [Event(116, Keywords = Keywords.All)]
        public void NugetApiManager_GetPackageMetadataAsyncStart(string source)
        {
            WriteEvent(116, source);
        }

        [Event(117, Keywords = Keywords.All)]
        public void NugetApiManager_GetPackageMetadataAsyncStop(int count)
        {
            WriteEvent(117, count);
        }

        #endregion

        public static class Keywords
        {
            public const EventKeywords All = (EventKeywords)0x1;
            public const EventKeywords PerformanceLog = (EventKeywords)0x2;
        }

    }
}
