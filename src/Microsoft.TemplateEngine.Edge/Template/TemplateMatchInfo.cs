// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Edge.Template
{
    internal class TemplateMatchInfo : ITemplateMatchInfo
    {
        private readonly List<MatchInfo> _matchDisposition = new List<MatchInfo>();
        private readonly List<MatchInfo> _dispositionOfDefaults = new List<MatchInfo>();

        public TemplateMatchInfo(ITemplateInfo info, IReadOnlyList<MatchInfo> matchDispositions)
            : this(info)
        {
            if (matchDispositions != null)
            {
                foreach (MatchInfo disposition in matchDispositions)
                {
                    AddDisposition(disposition);
                }
            }
        }

        public TemplateMatchInfo(ITemplateInfo info)
        {
            Info = info;
            _dispositionOfDefaults = new List<MatchInfo>();
        }

        public ITemplateInfo Info { get; }

        public IReadOnlyList<MatchInfo> MatchDisposition => _matchDisposition;

        IReadOnlyList<MatchInfo> ITemplateMatchInfo.DispositionOfDefaults => _dispositionOfDefaults;

        public bool IsMatch => MatchDisposition.Count > 0 && MatchDisposition.All(x => x.Kind != MatchKind.Mismatch);

        public bool IsPartialMatch => MatchDisposition.Any(x => x.Kind != MatchKind.Mismatch);

        public void AddDisposition(MatchInfo newDisposition)
        {
            _matchDisposition.Add(newDisposition);
        }
    }
}
