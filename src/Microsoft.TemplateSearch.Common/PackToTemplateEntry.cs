// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.TemplateSearch.Common
{
    [JsonConverter(typeof(PackToTemplateEntryJsonConverter))]
    public class PackToTemplateEntry
    {
        public PackToTemplateEntry(PackInfo packInfo, List<TemplateIdentificationEntry> templateinfo)
        {
            PackInfo = packInfo;
            TemplateIdentificationEntry = templateinfo;
        }

        public PackInfo PackInfo { get; }

        public IReadOnlyList<TemplateIdentificationEntry> TemplateIdentificationEntry { get; }

        internal class PackToTemplateEntryJsonConverter : JsonConverter
        {
            public override bool CanRead
            {
                get { return false; }
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                PackToTemplateEntry packInfo = value as PackToTemplateEntry;
                if (packInfo == null)
                {
                    return;
                }
                writer.WriteStartObject();
                writer.WritePropertyName(nameof(PackInfo.Version));
                serializer.Serialize(writer, packInfo.PackInfo.Version);
                if (packInfo.PackInfo.TotalDownloads != 0)
                {
                    writer.WritePropertyName(nameof(PackInfo.TotalDownloads));
                    serializer.Serialize(writer, packInfo.PackInfo.TotalDownloads);
                }
                if (packInfo.PackInfo.Verified == true)
                {
                    writer.WritePropertyName(nameof(PackInfo.Verified));
                    serializer.Serialize(writer, packInfo.PackInfo.Verified);
                }
                if (packInfo.PackInfo.Owners != null && packInfo.PackInfo.Owners.Any())
                {
                    writer.WritePropertyName(nameof(PackInfo.Owners));
                    serializer.Serialize(writer, packInfo.PackInfo.Owners);
                }
                writer.WritePropertyName(nameof(PackToTemplateEntry.TemplateIdentificationEntry));
                serializer.Serialize(writer, packInfo.TemplateIdentificationEntry);
                writer.WriteEndObject();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(PackInfo);
            }
        }
    }
}
