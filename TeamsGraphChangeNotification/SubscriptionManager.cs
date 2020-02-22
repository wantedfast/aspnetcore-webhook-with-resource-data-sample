// <copyright file="SubscriptionManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace TeamsGraphChangeNotification
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Security.Policy;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Microsoft.Graph;
    using Models;
    using Newtonsoft.Json;
    using TeamsGraphChangeNotification.Controllers;

    public class SubscriptionManager : BackgroundService
    {
        private readonly TokenManager TokenManager;
        private readonly KeyVaultManager KeyVaultManager;
        private readonly IOptions<SubscriptionOptions> SubscriptionOptions;
        private Subscription TeamsSubscription;
        private readonly string notificationControllerUrl = $"api/{nameof(NotificationController).ToLower().Replace("controller", string.Empty)}";

        public SubscriptionManager(
            TokenManager tokenManager,
            IOptions<SubscriptionOptions> subscriptionOptions,
            KeyVaultManager keyVaultManager)
        {
            TokenManager = tokenManager;
            SubscriptionOptions = subscriptionOptions;
            KeyVaultManager = keyVaultManager;
        }

        public async Task CreateSubscription()
        {
            string resource = SubscriptionOptions.Value.Resource;
            string changeType = SubscriptionOptions.Value.ChangeType;
            string clientState = SubscriptionOptions.Value.ClientState;
            string notificationUrl = SubscriptionOptions.Value.NotificationUrl;
            if (!notificationUrl.EndsWith(notificationControllerUrl, StringComparison.InvariantCultureIgnoreCase))
                notificationUrl = new Uri(new Uri(notificationUrl), notificationControllerUrl).AbsoluteUri;
            string encryptionCertificate = await KeyVaultManager.GetEncryptionCertificate().ConfigureAwait(false);
            string encryptionCertificateId = await KeyVaultManager.GetEncryptionCertificateId().ConfigureAwait(false);
            bool includeProperties = bool.Parse(SubscriptionOptions.Value.IncludeProperties);

            var subscription = new Subscription
            {
                ChangeType = changeType,
                NotificationUrl = notificationUrl,
                Resource = resource,
                ExpirationDateTime = new DateTimeOffset(DateTime.UtcNow.AddMinutes(int.Parse(SubscriptionOptions.Value.SubscriptionExpirationTimeInMinutes)), TimeSpan.Zero),
                ClientState = clientState,
                EncryptionCertificate = encryptionCertificate,
                EncryptionCertificateId = encryptionCertificateId,
                IncludeProperties = includeProperties
            };

            try
            {
                TeamsSubscription = await Client.Subscriptions.Request().AddAsync(subscription).ConfigureAwait(false);
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
            try
            {
                TeamsSubscription = await Client.Subscriptions[TeamsSubscription.Id].Request().UpdateAsync(new Subscription
                {
                    ExpirationDateTime = new DateTimeOffset(DateTime.UtcNow.AddMinutes(int.Parse(SubscriptionOptions.Value.SubscriptionExpirationTimeInMinutes)), TimeSpan.Zero)
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Exception while Renewing Subscription: {ex}");
            }
        }
        private GraphServiceClient _client;
        private GraphServiceClient Client
        {
            get
            {
                if (_client == null)
                    _client = new GraphServiceClient(new DelegateAuthenticationProvider(async (requestMessage) =>
                    {

                    // get an access token for Graph
                    var token = await TokenManager.GetToken().ConfigureAwait(false);

                        requestMessage
                            .Headers
                            .Authorization = new AuthenticationHeaderValue("bearer", token);

                    }));
                return _client;
            }
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Trace.TraceInformation("SubscriptionManager has been started");
            await CreateSubscription().ConfigureAwait(false);
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(int.Parse(SubscriptionOptions.Value.SubscriptionRenewTimeInMinutes)*60*1000).ConfigureAwait(false);
                await RenewSubscription().ConfigureAwait(false);
            }
        }
    }
}
