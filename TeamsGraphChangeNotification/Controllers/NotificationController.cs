// <copyright file="NotificationController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace TeamsGraphChangeNotification.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
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
        private readonly string ContentTypeHeader = "Content-Type";
        private readonly string ContentTypeHeaderValue = "text/plain";

        public NotificationController(
            IOptions<SubscriptionOptions> subscriptionOptions,
            KeyVaultManager keyVaultManager)
        {
            SubscriptionOptions = subscriptionOptions;
            KeyVaultManager = keyVaultManager;
        }

        // POST api/Notification
        [HttpPost]
        public async Task<IActionResult> Post()
        {
            HttpResponseMessage httpResponse = new HttpResponseMessage(HttpStatusCode.OK);

            if (string.IsNullOrEmpty(this.Request.QueryString.Value))
            {
                try
                {
                    string content = string.Empty;
                    Stream requestBody = this.Request.Body;
                    using (StreamReader reader = new StreamReader(requestBody))
                    {
                        content = await reader.ReadToEndAsync().ConfigureAwait(false);
                    }

                    Notification notification = JsonConvert.DeserializeObject<Notification>(content);
                    Decryptor encryptor = new Decryptor();

                    if (!notification.Value.FirstOrDefault().ClientState.Equals(
                        this.SubscriptionOptions.Value.ClientState))
                    {
                        return BadRequest();
                    }

                    if (notification.Value.FirstOrDefault().EncryptedContent != null)
                    {
                        string decryptedpublisherNotification =
                        encryptor.Decrypt(
                            notification.Value.FirstOrDefault().EncryptedContent.Data,
                            notification.Value.FirstOrDefault().EncryptedContent.DataKey,
                            notification.Value.FirstOrDefault().EncryptedContent.DataSignature,
                            await KeyVaultManager.GetDecryptionCertificate().ConfigureAwait(false));

                        Dictionary<string, object> resourceDataObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(decryptedpublisherNotification);

                        Trace.TraceInformation($"Decrypted Notification: {decryptedpublisherNotification}");
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Exception while decrypting notification: {ex}");
                }

                return Ok();
            }

            var encodedString = this.Request.QueryString.Value?.Split('=')[1];
            var decodedString = System.Web.HttpUtility.UrlDecode(encodedString);
            return Content(decodedString);
        }
    }
}
