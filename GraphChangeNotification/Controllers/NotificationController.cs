// <copyright file="NotificationController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace TeamsGraphChangeNotification.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using Models;
    using Newtonsoft.Json;

    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly KeyVaultManager KeyVaultManager;
        private readonly IOptions<SubscriptionOptions> SubscriptionOptions;

        public NotificationController(
            IOptions<SubscriptionOptions> subscriptionOptions,
            KeyVaultManager keyVaultManager)
        {
            this.SubscriptionOptions = subscriptionOptions;
            this.KeyVaultManager = keyVaultManager;
        }

        // POST api/Notification
        [HttpPost]
        public async Task<IActionResult> Post([FromQuery]string validationToken = null)
        {
            if (string.IsNullOrEmpty(validationToken))
            {
                try
                {
                    string content = string.Empty;
                    Stream requestBody = Request.Body;

                    using (StreamReader reader = new StreamReader(requestBody))
                    {
                        content = await reader.ReadToEndAsync().ConfigureAwait(false);
                    }

                    Notification notification = JsonConvert.DeserializeObject<Notification>(content);

                    if (!notification.Value.FirstOrDefault().ClientState.Equals(
                        this.SubscriptionOptions.Value.ClientState))
                    {
                        return BadRequest();
                    }

                    if (notification.ValidationTokens != null && notification.ValidationTokens.Any())
                    { // we're getting notifications with resource data and we should validate tokens and decrypt data
                        TokenValidator tokenValidator = new TokenValidator(this.SubscriptionOptions.Value.TenantId, new[] { this.SubscriptionOptions.Value.ClientId });
                        bool areValidationTokensValid = (await Task.WhenAll(
                            notification.ValidationTokens.Select(x => tokenValidator.ValidateToken(x))).ConfigureAwait(false))
                            .Aggregate((x, y) => x && y);
                        if (areValidationTokensValid)
                        {
                            foreach (PublisherNotification notificationItem in notification.Value.Where(x => x.EncryptedContent != null))
                            {
                                string decryptedpublisherNotification =
                                Decryptor.Decrypt(
                                    notificationItem.EncryptedContent.Data,
                                    notificationItem.EncryptedContent.DataKey,
                                    notificationItem.EncryptedContent.DataSignature,
                                    await this.KeyVaultManager.GetDecryptionCertificate().ConfigureAwait(false));

                                Dictionary<string, object> resourceDataObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(decryptedpublisherNotification);
                                Console.WriteLine($"Decrypted Notification: {decryptedpublisherNotification}");
                            }
                            return Ok();
                        }
                        else
                        {
                            return Unauthorized("Token Validation failed");
                        } 
                    }
                    else
                    { // we're getting notifications without data and should call back the graph or tokens are missing and we shouldn't attempt to decrypt data
                        foreach (PublisherNotification notificationItem in notification.Value)
                            Console.WriteLine($"received notification for {notificationItem.Resource}");
                        return Ok();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception while decrypting notification: {ex}");
                    return Problem();
                }
            }
            else
            {
                return Content(validationToken);
            }
        }
    }
}
