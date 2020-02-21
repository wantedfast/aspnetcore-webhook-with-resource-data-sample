// <copyright file="SubscriptionOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace TeamsGraphChangeNotification.Models
{
    public class SubscriptionOptions
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TenantId { get; set; }
        public string ChangeType { get; set; }
        public string NotificationUrl { get; set; }
        public string Resource { get; set; }
        public string ClientState { get; set; }
        public string EncryptionCertificate { get; set; }
        public string EncryptionCertificateId { get; set; }
        public string IncludeProperties { get; set; }
        public string SubscriptionExpirationTimeInMinutes { get; set; }
        public string SubscriptionRenewTimeInMinutes { get; set; }
    }
}
