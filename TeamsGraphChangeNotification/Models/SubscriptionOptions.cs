// <copyright file="SubscriptionOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace TeamsGraphChangeNotification.Models
{
    public class SubscriptionOptions
    {
        public SubscriptionOptions() { }

        public string Scope { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TenantIdOrName { get; set; }
        public string ChangeType { get; set; }
        public string NotificationUrl { get; set; }
        public string Resource { get; set; }
        public string ClientState { get; set; }
        public string EncryptionCertificate { get; set; }
        public string EncryptionCertificateId { get; set; }
        public string IncludeProperties { get; set; }
        public string SubscriptionExpirationTimeInMinutes { get; set; }
        public string SubscriptionRenewTimeInMinutes { get; set; }
        public string DecryptionCertificate { get; set; }
    }
}
