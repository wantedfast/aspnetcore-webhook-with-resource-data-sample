// <copyright file="TokenManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace TeamsGraphChangeNotification
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using Microsoft.Identity.Client;
    using Models;

    public class TokenManager
    {
        private readonly IOptions<SubscriptionOptions> SubscriptionOptions;

        public TokenManager(IOptions<SubscriptionOptions> subscriptionOptions)
        {
            this.SubscriptionOptions = subscriptionOptions;
        }

        public async Task<string> GetToken()
        {
            string clientId = this.SubscriptionOptions.Value.ClientId;
            string clientSecret = this.SubscriptionOptions.Value.ClientSecret;
            string tenantIdOrName = this.SubscriptionOptions.Value.TenantId;

            string tokenScope = "https://graph.microsoft.com/.default";

            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, tenantIdOrName)
                .WithClientSecret(clientSecret)
                .Build();

            // Use the following for certificates
            // X509Certificate2 certificate = ReadCertificate(config.CertificateName);
            // IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
            //           .WithAuthority(AzureCloudInstance.AzurePublic, "{tenantID}")
            //           .WithCertificate(config.ClientSecret)
            //           .Build();

            string[] scopes = new string[] { tokenScope };

            AuthenticationResult result;
            try
            {
                result = await app.AcquireTokenForClient(scopes)
                                  .ExecuteAsync();
            }
            catch (MsalServiceException ex)
            {
                Console.WriteLine($"Exception while acquireing token {ex}");
                throw;
            }

            return result?.AccessToken;
        }
    }
}
