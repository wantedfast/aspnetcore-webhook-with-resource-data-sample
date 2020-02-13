// <copyright file="Notification.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace TeamsGraphChangeNotification.Models
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public class Notification
    {
        [JsonProperty(PropertyName = "value")]
        public List<PublisherNotification> Value { get; set; }

        [JsonProperty(PropertyName = "validationTokens")]
        public List<string> ValidationTokens { get; set; }
    }
}
