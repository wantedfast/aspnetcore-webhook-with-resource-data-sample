# Sample Application - Microsoft Graph Change Notifications

![.NET Core](https://github.com/microsoftgraph/csharp-webhook-with-resource-data/workflows/.NET%20Core/badge.svg?branch=master)

**This sample is now archived and replaced by [the main webhook sample](https://github.com/microsoftgraph/aspnetcore-webhooks-sample)**

Subscribe for [Microsoft Graph change notifications](https://docs.microsoft.com/graph/webhooks) to be notified when your user's data changes so you don't have to poll for changes.

This ASP.NET core sample shows how to start getting notifications from Microsoft Graph. Microsoft Graph provides a unified API endpoint to access data from Microsoft 365.

## Use this sample application to receive Change Notifications for Microsoft Graph

### How the sample application works

The sample is configured to do the following:

- On start up, create a subscription to receive Change Notifications from Microsoft Graph.
- Periodically (on a scheduled timer) extend the subscription.
- Once a Change Notification is received with encrypted data, decrypt it and print it to console. (only with supported resources)

To do the above tasks, the app will:

- Fetch token for Microsoft Graph to create the subscription.
- Read a certificate from Azure Key Vault for encryption/decryption. (only with supported resources)

## Setting up the sample

1. Get an Azure AD appid, and give it the right permissions.
2. Create a certificate in Azure Key Vault. (You don't need to use Azure Key Vault to use webhooks, but that's how this sample stores its certificates)
3. Connect Key Vault to your Azure AD appid
4. Update appsettings.json with information from the previous steps

### Register the app

- **Step 1**: Go to [Azure Portal](https://portal.azure.com/).

- **Step 2**: Create AAD Application
    - Click on the navigation icon in the Azure Portal, click on "All services"; on the next screen search for "Azure Active Directory" and then click on "Azure Active Directory"

    ![AadAppCreate1](docs/ad1.png)

    ![AadAppCreate2](docs/ad2.png)

- Click on "App registrations",  then click on "+ New registration"; Fill in the required details as shown below and click "Register"

    ![AadAppCreate3](docs/ad3.png)

    ![AadAppCreate4](docs/ad4.png)

- Go to the Application, click "Certificates & secrets", then click "+ New client secret", Add a description and then click "Add" and save the secret somewhere, you will need to add this to the configuration.

    ![AadAppCreate5](docs/ad5.png)

- Select the **API permissions** page. Click **Add a permission**, then select **Microsoft Graph**, **Application permissions**, **ChannelMessage.Read.All**. Click **Add permissions**.

*Note: for other resources you might need to select different permissions as documented [here](https://docs.microsoft.com/graph/api/subscription-post-subscriptions?view=graph-rest-beta&tabs=http#permissions)*

### Setting up Azure Key Vault

*Note: you are only required to follow these steps if the resource you are subscribing to supports including resouces data in notifications and if you set `includeResourceData` to `true` in the configuration. In any other case, you can skip these steps. Refer to [the documentation](https://docs.microsoft.com/graph/api/resources/webhooks?view=graph-rest-beta) for a complete list of resources that support or not including resources data.*  

- **Step 1**: Go to [Azure Portal](https://portal.azure.com/).

- **Step 2**: Create a Resource Group
    - Click on the navigation icon in the Azure Portal, click on "Resource groups"; on the next screen click "+ Add"

    ![ResourceGroupCreate1](docs/rg1.png)

    ![ResourceGroupCreate2](docs/rg2.png)

- Fill in the details as shown below and click "Review + create"; on the next screen click "Create"

    ![ResourceGroupCreate3](docs/rg3.png)

    ![ResourceGroupCreate4](docs/rg4.png)

- **Step 3**: Create Azure Key Vault
    - Go to the resource group created in the step above, and click "+ Add", on the next screen search for "Key Vault" and hit the return key and then click "Create"

    ![KeyVaultCreate1](docs/kv1.png)

    ![KeyVaultCreate2](docs/kv2.png)

    ![KeyVaultCreate3](docs/kv3.png)

- Fill in the required details as shown below and click "Access plolicy", then click "+ Add Access Policy"
    ![KeyVaultCreate4](docs/kv4.png)

    ![KeyVaultCreate5](docs/kv5.png)

- Fill in the required details and click "Select", then click "Add" and then click "Create"

    ![KeyVaultCreate6](docs/kv6.png)

    ![KeyVaultCreate7](docs/kv7.png)

- **Step 4**: Create Self-Signed certificate
    - Go to the Key Vault and click "Certificates", then click "+ Generate/Import"; Fill in the details as shown below and click "Create"

    ![KeyVaultCreate8](docs/kv8.png)

    ![KeyVaultCreate9](docs/kv9.png)

### Connect Key Vault to your Azure AD appid

*Note: you are only required to follow these steps if the resource you are subscribing to supports including resouces data in notifications and if you set `includeResourceData` to `true` in the configuration. In any other case, you can skip these steps. Refer to [the documentation](https://docs.microsoft.com/graph/api/resources/webhooks?view=graph-rest-beta) for a complete list of resources that support or not including resources data.*  

1. Go to Access policies under Settings. Click Add Access Policy.
1. Under Secret Permissions, select Get and List.
1. Under Certificate Permissions, select Get and List.
1. Under Select principal, select your appid.
1. Click Add to finish your access policy. Wait for your access policy to deploy.

### Update appsettings.json

- **Step 1**: Open the sample in Visual Studio and then open appsettings.json file to update the following settings:
    - **Mandatory settings under SubscriptonSettigs section**:
        - **ClientID**: Client Id of the AAD Application used to create the Change Notification subscription
        - **ClientSecret**: Client Secret of the AAD Application used to create the Change Notification subscription
        - **TenantId**: Tenant Id for which the Change Notification subscription needs to be created (Can be found on the application registration page)
        - **NotificationUrl**: The HTTPS Notification URL. (if you are debugging locally you can use (ngrok)[https://ngrok.com/] by typing `ngrok http 5000 -host-header=rewrite` in a separate console and use the generated URL eg. https://3a5348f1.ngrok.io)

    - **Mandatory settings under KeyVaultSettings section** (only if `includeResourceData` is set to `true`):
        - **KeyVaultUri**: Azure Key Vault Uri of Azure Key Vault created in the section "Create Azure Key Vault" above
        - **TenantId**: Tenant Id of the application created in the section "Create AAD Application for Key Vault Access" above
        - **ClientId**: Client Id of the application created in the section "Create AAD Application for Key Vault Access" above
        - **ClientSecret**: Client Secret of the application created in the section "Create AAD Application for Key Vault Access" above
        - **CertificateUrl**: CertificateUrl of the certificate secret created in the section "Create AAD Application for Key Vault Access" above (e.g. https://changenotificationsample.vault.azure.net/secrets/ChangeNotificationSampleCertificate)

    - **Optional settings under SubscriptionSettings section**:
        - **ChangeType**: CSV; possible values created, updated, deleted
        - **Resource**: resource to create subscription for (e.g. teams/allMessages)
        - **ClientState**: Some cryptographic string used to validate the Change Notifications
        - **IncludeResourceData**: true or false
        - **SubscriptionExpirationTimeInMinutes**: Subscription expiration time in minutes, max 60 minutes
        - **SubscriptionRenewTimeInMinutes**: Subscription renew time in minutes, max 60 minutes

- **Step 2**: In the Solution Explorer, right click on the "GraphChangeNotification" project and select "Set as StartUp Project" and click start (or play button)

- **Step 3**: Open the Microsoft Teams client and send a message for the resource to which the subscription is created. The message will be received, decrypted and printed on the console.  
    *Note: if you are subscribing to other resources, peform the corresponding action in the UI or via the API you are subscribing to.*

## Troubleshooting

If you run into errors or problems with the sample, see the [troubleshooting doc](TROUBLESHOOTING.md).

## Contributing

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Questions and comments

We'd love to get your feedback about the Microsoft Graph ASP.NET Webhooks sample. You can send your questions and suggestions to us in the Issues section of this repository.

Questions about Microsoft Graph in general should be posted to [Stack Overflow](https://stackoverflow.com/questions/tagged/MicrosoftGraph). Make sure that your questions or comments are tagged with `MicrosoftGraph`.

If you have a feature suggestion, please post your idea on our [User Voice](https://microsoftgraph.uservoice.com/) page, and vote for your suggestions there.

## Additional resources

- [Microsoft Graph Node.js Webhooks sample](https://github.com/microsoftgraph/nodejs-webhooks-rest-sample)
- [Working with Webhooks in Microsoft Graph](https://docs.microsoft.com/graph/webhooks)
- [Subscription resource](https://developer.microsoft.com/graph/docs/api-reference/v1.0/resources/subscription)
- [Microsoft Graph developer site](https://developer.microsoft.com/graph/)

## Copyright

Copyright (c) 2019 Microsoft. All rights reserved.
