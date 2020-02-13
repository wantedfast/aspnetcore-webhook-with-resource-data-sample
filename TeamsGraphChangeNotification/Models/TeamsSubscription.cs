// <copyright file="TeamsSubscription.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace TeamsGraphChangeNotification.Models
{
    using System;
    using Newtonsoft.Json;

    public class TeamsSubscription
    {
        [JsonProperty(PropertyName = "@odata.context", Required = Required.Always)]
        public string ODataContext { get; set; }

        [JsonProperty(PropertyName = "id", Required = Required.Always)]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "resource", Required = Required.Always)]
        public string Resource { get; set; }

        [JsonProperty(PropertyName = "applicationId", Required = Required.Always)]
        public string ApplicationId { get; set; }

        [JsonProperty(PropertyName = "changeType", Required = Required.Always)]
        public string ChangeType { get; set; }

        [JsonProperty(PropertyName = "clientState", Required = Required.Always)]
        public string ClientState { get; set; }

        [JsonProperty(PropertyName = "notificationUrl", Required = Required.Always)]
        public string NotificationUrl { get; set; }

        [JsonProperty(PropertyName = "lifecycleNotificationUrl", Required = Required.AllowNull)]
        public string LifecycleNotificationUrl { get; set; }

        [JsonProperty(PropertyName = "expirationDateTime", Required = Required.Always)]
        public DateTime ExpirationDateTime { get; set; }

        [JsonProperty(PropertyName = "creatorId", Required = Required.Always)]
        public string CreatorId { get; set; }

        [JsonProperty(PropertyName = "includeProperties", Required = Required.Always)]
        public bool IncludeProperties { get; set; }

        [JsonProperty(PropertyName = "includeResourceData", Required = Required.Always)]
        public bool IncludeResourceData { get; set; }

        [JsonProperty(PropertyName = "encryptionCertificate", Required = Required.Always)]
        public string EncryptionCertificate { get; set; }

        [JsonProperty(PropertyName = "encryptionCertificateId", Required = Required.Always)]
        public string EncryptionCertificateId { get; set; }
    }
}
