// <copyright file="SubscriptionManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace TeamsGraphChangeNotification
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Microsoft.Graph;
    using Models;
    using TeamsGraphChangeNotification.Controllers;

    public class SubscriptionManager : BackgroundService
    {
        private GraphServiceClient _client;
        private readonly TokenManager TokenManager;
        private readonly KeyVaultManager KeyVaultManager;
        private readonly IOptions<SubscriptionOptions> SubscriptionOptions;
        private readonly Dictionary<string, Subscription> TeamsSubscriptions = new Dictionary<string, Subscription>();
        private readonly string NotificationControllerUrl = $"api/{nameof(NotificationController).ToLower().Replace("controller", string.Empty)}";

        public SubscriptionManager(
            TokenManager tokenManager,
            IOptions<SubscriptionOptions> subscriptionOptions,
            KeyVaultManager keyVaultManager)
        {
            this.TokenManager = tokenManager;
            this.SubscriptionOptions = subscriptionOptions;
            this.KeyVaultManager = keyVaultManager;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("SubscriptionManager has been started");

            await this.GetSubscriptions().ConfigureAwait(false);
            await this.CreateSubscriptions().ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(int.Parse(this.SubscriptionOptions.Value.SubscriptionRenewTimeInMinutes)), cancellationToken).ConfigureAwait(false);
                await this.RenewSubscriptions().ConfigureAwait(false);
            }
        }

        private async Task GetSubscriptions()
        {
            try
            {
                IGraphServiceSubscriptionsCollectionPage subscriptionCollectionPage = await this.Client.Subscriptions.Request().GetAsync().ConfigureAwait(false);
                IGraphServiceSubscriptionsCollectionRequest subscriptionsNextpageRequest = subscriptionCollectionPage.NextPageRequest;

                foreach (Subscription subscription in subscriptionCollectionPage.CurrentPage)
                {
                    this.TeamsSubscriptions.TryAdd(subscription.Id, subscription);
                }

                while (subscriptionsNextpageRequest != null)
                {
                    IGraphServiceSubscriptionsCollectionPage subscriptionsNextPage = await subscriptionsNextpageRequest.GetAsync().ConfigureAwait(false);
                    subscriptionsNextpageRequest = subscriptionsNextPage.NextPageRequest;

                    foreach (Subscription subscription in subscriptionsNextPage.CurrentPage)
                    {
                        this.TeamsSubscriptions.TryAdd(subscription.Id, subscription);
                    }
                }

                await this.RenewSubscriptions().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while getting exisiting subscriptions: {ex}");
            }
        }

        private async Task CreateSubscriptions()
        {
            string resources = this.SubscriptionOptions.Value.Resource;
            List<string> resourceList = resources.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList();

            string changeType = this.SubscriptionOptions.Value.ChangeType;
            string clientState = this.SubscriptionOptions.Value.ClientState;
            string notificationUrl = this.SubscriptionOptions.Value.NotificationUrl;
            bool includeResourceData = bool.Parse(this.SubscriptionOptions.Value.IncludeResourceData);
            string encryptionCertificate = includeResourceData ? await this.KeyVaultManager.GetEncryptionCertificate().ConfigureAwait(false) : null;
            string encryptionCertificateId = includeResourceData ? await this.KeyVaultManager.GetEncryptionCertificateId().ConfigureAwait(false) : null;
            int subscriptionExpirationTimeInMinutes = int.Parse(this.SubscriptionOptions.Value.SubscriptionExpirationTimeInMinutes);

            if (subscriptionExpirationTimeInMinutes > 60)
            {
                subscriptionExpirationTimeInMinutes = 60;
            }

            if (!notificationUrl.EndsWith(this.NotificationControllerUrl, StringComparison.InvariantCultureIgnoreCase))
            {
                notificationUrl = new Uri(new Uri(notificationUrl), this.NotificationControllerUrl).AbsoluteUri;
            }

            // Find resources for which subscription does not exist so that we can create new subscriptions for those resources
            List<string> existingSubscriptionResource = new List<Subscription>(this.TeamsSubscriptions.Values).Select(s => s.Resource).ToList();
            List<string> resourcesToCreateSubscriptionsFor = resourceList.Except(existingSubscriptionResource).ToList();

            foreach (string resource in resourcesToCreateSubscriptionsFor)
            {
                Subscription subscription = new Subscription
                {
                    ChangeType = changeType,
                    NotificationUrl = notificationUrl,
                    Resource = resource,
                    ExpirationDateTime = new DateTimeOffset(DateTime.UtcNow.AddMinutes(subscriptionExpirationTimeInMinutes), TimeSpan.Zero),
                    ClientState = clientState,
                    EncryptionCertificate = encryptionCertificate,
                    EncryptionCertificateId = encryptionCertificateId,
                    IncludeResourceData = includeResourceData
                };

                try
                {
                    Subscription teamsSubscription = await this.Client.Subscriptions.Request().AddAsync(subscription).ConfigureAwait(false);

                    if (!this.TeamsSubscriptions.ContainsKey(teamsSubscription.Id))
                    {
                        this.TeamsSubscriptions.TryAdd(teamsSubscription.Id, teamsSubscription);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception while Creating Subscription: {ex}");
                }
            }
        }

        private async Task RenewSubscriptions()
        {
            // Renewing the certificate from key vault every time this is called. You can choose to provide this as a property
            // in the request body. This will help with the certificate renewal process.
            bool includeResourceData = bool.Parse(this.SubscriptionOptions.Value.IncludeResourceData);
            if (includeResourceData)
                await this.KeyVaultManager.GetEncryptionCertificate().ConfigureAwait(false);
            try
            {
                List<string> subscriptionIdsToRenew = new List<string>(this.TeamsSubscriptions.Keys);
                foreach (string subscriptionId in subscriptionIdsToRenew)
                {
                    Subscription teamsSubscription = await Client.Subscriptions[subscriptionId].Request().UpdateAsync(new Subscription
                    {
                        ExpirationDateTime = new DateTimeOffset(DateTime.UtcNow.AddMinutes(int.Parse(this.SubscriptionOptions.Value.SubscriptionExpirationTimeInMinutes)), TimeSpan.Zero)
                    }).ConfigureAwait(false);

                    if (this.TeamsSubscriptions.ContainsKey(teamsSubscription.Id))
                    {
                        this.TeamsSubscriptions[subscriptionId] = teamsSubscription;
                    }
                    else
                    {
                        this.TeamsSubscriptions.TryAdd(teamsSubscription.Id, teamsSubscription);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while Renewing Subscription: {ex}");
            }
        }

        private GraphServiceClient Client
        {
            get
            {
                if (this._client == null)
                {
                    this._client = new GraphServiceClient(new DelegateAuthenticationProvider(async (requestMessage) =>
                    {
                        // get an access token for Graph
                        string token = await this.TokenManager.GetToken().ConfigureAwait(false);

                        requestMessage
                            .Headers
                            .Authorization = new AuthenticationHeaderValue("bearer", token);
                    }));
                }
                return this._client;
            }
        }
    }
}
