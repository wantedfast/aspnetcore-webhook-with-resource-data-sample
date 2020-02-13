// <copyright file="SubscriptionManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace TeamsGraphChangeNotification
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Models;
    using Newtonsoft.Json;

    public class SubscriptionManager : IHostedService
    {
        private Timer _timer;
        private readonly TokenManager TokenManager;
        private readonly KeyVaultManager KeyVaultManager;
        private readonly IOptions<SubscriptionOptions> SubscriptionOptions;
        private TeamsSubscription TeamsSubscription = new TeamsSubscription();
        private readonly string Canary = "canary";
        private readonly string HttpPatchVerb = "PATCH";
        private readonly string ContentType = "application/json";
        private readonly string AuthorizationHeader = "Authorization";
        private readonly string GraphSubscriptionUrl = "https://graph.microsoft.com/beta/subscriptions";
        private readonly string CanaryGraphSubscriptionUrl = "https://canary.graph.microsoft.com/beta/subscriptions";

        public SubscriptionManager(
            TokenManager tokenManager,
            IOptions<SubscriptionOptions> subscriptionOptions,
            KeyVaultManager keyVaultManager)
        {
            TokenManager = tokenManager;
            SubscriptionOptions = subscriptionOptions;
            KeyVaultManager = keyVaultManager;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Trace.TraceInformation("SubscriptionManager has been started");
            await CreateSubscription().ConfigureAwait(false);
            _timer = new Timer(
                async state => await RenewSubscription().ConfigureAwait(false),
                null,
                TimeSpan.FromMinutes(29),
                TimeSpan.FromMinutes(29));
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task CreateSubscription()
        {
            string resource = SubscriptionOptions.Value.Resource;
            string changeType = SubscriptionOptions.Value.ChangeType;
            string clientState = SubscriptionOptions.Value.ClientState;
            string notificationUrl = SubscriptionOptions.Value.NotificationUrl;
            string encryptionCertificate = await KeyVaultManager.GetEncryptionCertificate().ConfigureAwait(false);
            string encryptionCertificateId = await KeyVaultManager.GetEncryptionCertificateId().ConfigureAwait(false);
            bool includeProperties = bool.Parse(SubscriptionOptions.Value.IncludeProperties);

            string graphSubscriptionBaseUrl = GetGraphSubscriptionBaseUrl();

            string expirationTime = DateTime.UtcNow.AddMinutes(int.Parse(SubscriptionOptions.Value.SubscriptionExpirationTimeInMinutes)).ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
            string responseString = string.Empty;

            Dictionary<string, string> body = new Dictionary<string, string>
            {
                { "changeType", changeType },
                { "notificationUrl", notificationUrl },
                { "resource", resource },
                { "expirationDateTime", expirationTime },
                { "clientState", clientState },
                { "encryptionCertificate", encryptionCertificate },
                { "encryptionCertificateId", encryptionCertificateId },
                { "includeProperties", includeProperties.ToString() }
            };

            try
            {
                responseString = await this.ExecuteHttpRequest(HttpMethod.Post, graphSubscriptionBaseUrl, body).ConfigureAwait(false);
                TeamsSubscription = JsonConvert.DeserializeObject<TeamsSubscription>(responseString);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Exception while Creating Subscription: {ex}");
            }
        }

        public async Task RenewSubscription()
        {
            // Renewing the certificate from key vault every time this is called. You can choose to provide this as a property
            // in the request body. This will help with the certificate renewal process.
            await KeyVaultManager.GetEncryptionCertificate().ConfigureAwait(false);

            string responseString = string.Empty;
            string expirationTime = DateTime.UtcNow.AddMinutes(int.Parse(SubscriptionOptions.Value.SubscriptionExpirationTimeInMinutes)).ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");

            string graphSubscriptionBaseUrl = GetGraphSubscriptionBaseUrl();
            string graphSubscriptionPatchUrl = $"{graphSubscriptionBaseUrl}/{TeamsSubscription.Id}";

            Dictionary<string, string> body = new Dictionary<string, string>
            {
                // Any of the properties specified in create subscription can be updated/patched
                // Rotating certificate will also be done in the same way
                { "expirationDateTime", expirationTime }
            };

            try
            {
                responseString = await this.ExecuteHttpRequest(new HttpMethod(HttpPatchVerb), graphSubscriptionPatchUrl, body).ConfigureAwait(false);
                TeamsSubscription = JsonConvert.DeserializeObject<TeamsSubscription>(responseString);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Exception while Renewing Subscription: {ex}");
            }
        }

        private string GetGraphSubscriptionBaseUrl()
        {
            string graphSubscriptionBaseUrl = GraphSubscriptionUrl;

            if (SubscriptionOptions.Value.Scope.Equals(Canary, StringComparison.OrdinalIgnoreCase))
            {
                graphSubscriptionBaseUrl = CanaryGraphSubscriptionUrl;
            }

            return graphSubscriptionBaseUrl;
        }

        private async Task<string> ExecuteHttpRequest(HttpMethod httpMethod, string requestUri, Dictionary<string, string> body)
        {
            string responseString = string.Empty;
            string token = await TokenManager.GetToken().ConfigureAwait(false);

            using (HttpClient httpClient = new HttpClient())
            {
                HttpRequestMessage httpRequestMessage = new HttpRequestMessage(httpMethod, requestUri)
                {
                    Content = new StringContent(
                        JsonConvert.SerializeObject(body),
                        Encoding.UTF8,
                        ContentType)
                };

                httpRequestMessage.Headers.TryAddWithoutValidation(AuthorizationHeader, token);

                HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
                responseString = await httpResponseMessage.Content.ReadAsStringAsync();
            }

            return responseString;
        }
    }
}
