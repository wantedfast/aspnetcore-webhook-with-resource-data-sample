# Sample Application - Microsoft Teams Graph Change Notifications

## Use this sample application to receive Change Notifications for Microsoft Teams

### How the sample application works

The sample is configured to do the following:
- On start up, create a subscription to receive Change Notifications from Microsoft Teams.
- Periodically (on a scheduled timer) extend the subscription.
- Once an encrypted Change Notification is received, decrypt it and print it to console.

To do the above tasks, the app will:
- Fetch token for Microsoft Graph to create the subscription.
- Read a certificate from Azure Key Vault for encryption/decryption.

## Prerequisites

### Create AAD Application for Key Vault Access

- **Step 1**: Go to [Azure Portal](https://portal.azure.com/).

- **Step 2**: Create AAD Application
    - Click on the navigation icon in the Azure Portal, click on "All services"; on the next screen search for "Azure Active Directory" and then click on "Azure Active Directory"

![AadAppCreate1](ad1.png)

![AadAppCreate2](ad2.png)

    - Click on "App registrations",  then click on "+ New registration"; Fill in the required details as shown below and click "Register"

![AadAppCreate3](ad3.png)

![AadAppCreate4](ad4.png)

    - Go to the Application, click "Certificates & secrets", then click "+ New client secret", Add a description and then click "Add" and save the secret somewhere, you will need to add this to the configuration.

![AadAppCreate5](ad5.png)

### Setting up Azure Key Vault

- **Step 1**: Go to [Azure Portal](https://portal.azure.com/).

- **Step 2**: Create a Resource Group
    - Click on the navigation icon in the Azure Portal, click on "Resource groups"; on the next screen click "+ Add"

![ResourceGroupCreate1](RG1.png)

![ResourceGroupCreate2](RG2.png)

    - Fill in the details as shown below and click "Review + create"; on the next screen click "Create"

![ResourceGroupCreate3](RG3.png)

![ResourceGroupCreate4](RG4.png)

- **Step 3**: Create Azure Key Vault
    - Go to the resource group created in the step above, and click "+ Add", on the next screen search for "Key Vault" and hit the return key and then click "Create"

![KeyVaultCreate1](kv1.png)

![KeyVaultCreate2](kv2.png)

![KeyVaultCreate3](kv3.png)


    - Fill in the required details as shown below and click "Access plolicy", then click "+ Add Access Policy"
![KeyVaultCreate4](kv4.png)

![KeyVaultCreate5](kv5.png)


    - Fill in the required details and click "Select", then click "Add" and then click "Create"

![KeyVaultCreate6](kv6.png)

![KeyVaultCreate7](kv7.png)


- **Step 4**: Create Self-Signed certificate
    - Go to the Key Vault and click "Certificates", then click "+ Generate/Import"; Fill in the details as shown below and click "Create"

![KeyVaultCreate8](kv8.png)

![KeyVaultCreate9](kv9.png)


## Setting up and running the application

### Setting up the sample application

- **Step 1**: Create an Application in AAD and assign the requested permissions, please note the Application ID and the Application Secret

- **Step 2**: Open the sample in Visual Studio and then open appsettings.json file to update the following settings:
    - **Mandatory settings under SubscriptonSettigs section**:
        - **ClientID**: Client Id of the AAD Application used to create the Change Notification subscription
        - **ClientSecret**: Client Secret of the AAD Application used to create the Change Notification subscription
        - **TenantIdOrName**: Tenant Id or Tenant Name for which the Change Notification subscription needs to be created (e.g. contoso.onmicrosoft.com)
        - **NotificationUrl**: The HTTPS Notification URL

  - **Mandatory settings under KeyVaultSettings section**:
        - **ClientId**: Client Id of the application created in the section "Create AAD Application for Key Vault Access" above
        - **ClientSecret**: Client Secret of the application created in the section "Create AAD Application for Key Vault Access" above
        - **CertificateUrl**: CertificateUrl of the certificate secret created in the section "Create AAD Application for Key Vault Access" above (e.g. https://changenotificationsample.vault.azure.net/secrets/ChangeNotificationSampleCertificate)

  - **Optional settings under SubscriptionSettings section**:
        - **ChangeType**: CSV; possible values created, updated, deleted
        - **Scope**: Production or Canary
        - **Resource**: resource to create subscription for (e.g. teams/allMessages)
        - **ClientState**: Some cryptographic string used to validate the Change Notifications
        - **IncludeProperties**: true or false
        - **SubscriptionExpirationTimeInMinutes**: Subscription expiration time in minutes, max 60 minutes 
        - **SubscriptionRenewTimeInMinutes**: Subscription renew time in minutes, max 60 minutes

- **Step 3**: In the Solution Explorer, right click on the "TeamsGraphChangeNotification" project and select "Set as StartUp Project" and click start (or play button)

- **Step 4**: Open the Microsoft Teams client and send a message for the resource to which the subscription is created. The message will be received, decrypted and printed on the console.
