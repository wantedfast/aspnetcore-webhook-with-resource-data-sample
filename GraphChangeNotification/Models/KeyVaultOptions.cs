// <copyright file="KeyVaultOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace TeamsGraphChangeNotification.Models
{
    public class KeyVaultOptions
    {
        public string KeyVaultUri { get; set; }
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string CertificateUrl { get; set; }
    }
}
