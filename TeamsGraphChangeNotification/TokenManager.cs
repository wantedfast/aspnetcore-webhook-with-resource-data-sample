// <copyright file="TokenManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace TeamsGraphChangeNotification
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using Microsoft.Identity.Client;
    using Models;

    public class TokenManager
    {
        private readonly IOptions<SubscriptionOptions> SubscriptionOptions;

        public TokenManager(IOptions<SubscriptionOptions> subscriptionOptions)
        {
            SubscriptionOptions = subscriptionOptions;
        }

        public async Task<string> GetToken()
        {
            string scope = SubscriptionOptions.Value.Scope;
            string clientId = SubscriptionOptions.Value.ClientId;
            string clientSecret = SubscriptionOptions.Value.ClientSecret;
            string tenantIdOrName = SubscriptionOptions.Value.TenantIdOrName;

            string tokenScope = "https://graph.microsoft.com/.default";
            if (scope.Equals("canary", StringComparison.OrdinalIgnoreCase))
            {
                tokenScope = "https://canary.graph.microsoft.com/.default";
            }

            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, tenantIdOrName)
                .WithClientSecret(clientSecret)
                .Build();

            // Use the following for certificates
            // X509Certificate2 certificate = ReadCertificate(config.CertificateName);
            // var app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
            //           .WithAuthority(AzureCloudInstance.AzurePublic, "{tenantID}")
            //           .WithCertificate(config.ClientSecret)
            //           .Build();

            string[] scopes = new string[] { tokenScope };

            AuthenticationResult result = null;
            try
            {
                result = await app.AcquireTokenForClient(scopes)
                                  .ExecuteAsync();
            }
            catch (MsalServiceException ex)
            {
                Trace.TraceError($"Exception while acquireing token {ex}");
                throw;
            }

            return result?.AccessToken;
        }
    }
}
