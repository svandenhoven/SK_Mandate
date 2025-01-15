# PurchaseAPI and PurchaseAgent Application Setup

## Purchase Agent with Mandate

This application is a Semantic Kernel Agent that can purchase products. There is a plugin and an API to do the purchase. There is a mandate implemented that give constraints to the purchase an agent can do. Below is an sample outcome of UI

```plaintext
Authenticating user...
Getting user mandates...
Initialize plugins...
Creating kernel...
User signed in: admindevsvdh@xvkgz.onmicrosoft.com
Defining agent...
Ready!

> Buy 134 Apples

## Purchase Completed Successfully

### Product Information:
- **Product Name:** Apple
- **Specifications:** Fresh, organic apples
- **Total Quantity:** 134

### Detailed Report for Split Purchases:

#### Manufacturer: King Fruits
1. **First Transaction:**
   - **Quantity:** 100
   - **Unit Price:** $2.06
   - **Discount:** 20%
   - **Total Cost:**
     - Base Price: $206.00
     - After Discount: $164.80
   - **Purchase ID:** 07252913-a97f-48b7-873f-eeaa0c6c12ae

2. **Second Transaction:**
   - **Quantity:** 34
   - **Unit Price:** $2.06
   - **Discount:** 20%
   - **Total Cost:**
     - Base Price: $70.04
     - After Discount: $56.03
   - **Purchase ID:** 7f792180-f1fa-4008-b7af-eac17b2e4053

### Total Cost:
- **Grand Total Cost:** $220.83

### Manufacturer Comparison:
1. **King Fruits:**
   - Total Cost = $220.83 (split into two transactions)
2. **Fruitopia Market:**
   - Total Cost = $302.27
3. **The Juicy Orchard:**
   - Total Cost = $229.53

### Decision-Making Process:
- **Reason for Choosing King Fruits:**
  - Lowest total cost of $220.83 after splitting into two transactions, compared to other manufacturers.
- **Reason for Not Choosing Others:**
  - Fruitopia Market and The Juicy Orchard offered higher total costs: $302.27 and $229.53 respectively.

### Purchase Confirmation:
- **Date of Purchase:** 15/01/2025 09:56
- **Purchase ID for First Transaction:** 07252913-a97f-48b7-873f-eeaa0c6c12ae
- **Purchase ID for Second Transaction:** 7f792180-f1fa-4008-b7af-eac17b2e4053

This detailed report provides transparency and ensures proper record-keeping for auditing purposes. The chosen manufacturer, King Fruits, was selected for providing the lowest total cost while adhering to the unit limit constraints. All relevant purchase details are documented for future reference.
```

## Architecture

The agent is build using Semantic Kernel Agent Framework, see [link](https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/?pivots=programming-language-csharp).

The application consists of two main components:

1. **PurchaseAPI**: This is an API that provides endpoints for accessing product prices and making purchases. It is registered as an Azure AD application with specific scopes (`product.prices` and `product.purchase`) that define the permissions required to access these endpoints.

2. **PurchaseAgent**: This is a client application that interacts with the PurchaseAPI. It is registered as a public client application in Azure AD and is configured with the necessary permissions to access the PurchaseAPI endpoints. The client application uses the OAuth 2.0 authorization code flow to authenticate users and obtain access tokens for calling the PurchaseAPI. Additionally, the PurchaseAgent is a Semantic Kernel Agent that delivers a chat interface in a console app, allowing users to interact with the PurchaseAPI through natural language commands.

### OpenAI or Azure OpenAI integration

The solution with SK used OpenAI integration for:

- **Natural Language Processing**: Utilizes natural language processing to understand and execute user commands in a conversational manner.
- **Conversation Control and Understanding**: OpenAI is used to control and understand the conversation, ensuring that user inputs are correctly interpreted and appropriate responses are generated.
- **Planning and Decision Making**: OpenAI is also used for planning and decision making, enabling the system to make informed decisions based on user inputs and context.

### Semantic Kernel Plugin: PurchasePlugin

The `PurchasePlugin` class is a Semantic Kernel plugin that provides a set of commands for interacting with the PurchaseAPI. It enables users to perform actions such as retrieving product prices and making purchases through natural language commands in a chat interface. The plugin integrates with the PurchaseAgent to facilitate seamless communication with the PurchaseAPI.

#### Key Features

- **Retrieve Product Prices**: Allows users to query the prices of products available in the system.
- **Make Purchases**: Enables users to purchase products by specifying the product details and quantity.
- **Integration with PurchaseAPI**: Communicates with the PurchaseAPI to perform the necessary actions based on user commands.

### Agent Mandate Service

The Agent Mandate Service is responsible for creating mandates that control the actions an agent is allowed to perform. A mandate defines the permissions and constraints for an agent, ensuring that it operates within the specified boundaries. This service is crucial for maintaining security and governance over the actions performed by the agent.

#### Key Features

- **Create Mandates**: Allows to define and create mandates that specify the actions an agent can perform.
- **Control Actions**: Ensures that agents operate within the boundaries defined by their mandates, preventing unauthorized actions.
- **Integration with PurchaseAgent**: Works seamlessly with the PurchaseAgent to enforce mandates and control agent behaviour.

The mandate is defined by the Mandate.cs class. This class Mandate and Condition class. The Mandate contains a list of conditions that will define the boundaries for the agent. A Example condition is:

```text
A agent is not allowed to do a purchase with quantity larger than 100.
```

The mandate is shown below as class diagram.

```plaintext
+----------------------+  
|      Mandate         |  
+----------------------+
| - MandateId: Guid    |
| - Action: string     |
| - GrantedByUserId:   |
|   string             |
| - ValidFrom: DateTime|
| - ValidUntil:        |
|   DateTime?          |
| - Conditions:        |
|   List<Condition>    |
+----------------------+
| + IsValid(): bool    |
| + Message(): string  |
+----------------------+
           |
           |
           v
+------------------------+
|     Condition          |
+------------------------+
| - ConditionId: Guid    |
| - Type: ConditionTypes |
| - Value: decimal       |
| - Unit: ConditionUnits |
+------------------------+
```

## Prerequisites

Before you begin, ensure you have the following installed on your machine:

- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Python](https://www.python.org/downloads/) (for generating UUIDs)
- [Git](https://git-scm.com/downloads) (optional, for cloning the repository)

## Installation Steps

### 1. Clone the Repository

If you haven't already, clone the repository to your local machine:

```bash
git clone https://github.com/your-repo/SK_Mandate.git
cd SK_Mandate
```

### 2. Navigate to the Script Directory

Change to the directory containing the appregistrations.sh script:

```bash
cd c:/Users/sandervd/source/repos/SK_Mandate/sk_agents
```

### 3. Change the variables

Open the file appregistrations.sh. Change the variables for the script

| Variable          | Value                                          |
|-------------------|------------------------------------------------|
| tenantId          | Replace with your Tenant ID                    |
| subscriptionId    | Replace with your Subscription ID              |
| appRegApiName     | PurchaseAgentAPI                               |
| appRegClientName  | PurchaseAgent                                  |

### 4. Make the Script Executable

If you're on a Unix-based system (Linux or macOS), make the script executable:

```bash
chmod +x appregistrations.sh
```

### 5. Run the Script

Execute the script to create the app registrations and set up the necessary permissions:

```bash
./appregistrations.sh
```

For Windows users, you can run the script using Git Bash.

### 6. Add output variables to application configuration

The script will output the values for the created app registration. This will be in a format like

```bash
PurchaseAgentAPI App Registration:
App ID: <value of $purchaseApiAppId>
Tenant ID: <your tenant ID>
Scopes: products.prices, products.purchase

PurchaseAgent App Registration:
Client Id: <value of $purchaseAgentAppId>
Client Secret: <Please create a for $purchaseAgentAppId in Azure Portal>
Tenant ID: <your tenant id>
Scopes: api://<value of $purchaseAgentAppId>/access_as_user
APIScopes: api://<value of $purchaseApiAppId>/products.prices api://<value of $purchaseApiAppId>/products.purchase
```

- Use the output for PurchaseAgentAPI to set the [appsettings.json](./PurchaseAPI/appsettings.json) in PurchaseAPI

- Use the output for PurchaseAgent to set the user secrets for the project sk_agents. This can be done with the following script

```bash
# Navigate to the project directory
cd /path/to/sk_agents

# Set the user secrets
dotnet user-secrets set "AzureOpenAISettings:Endpoint" "https://<your openai endpoint>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAISettings:ChatModelDeployment" "gpt-4o"
dotnet user-secrets set "AzureOpenAISettings:ApiKey" "<your azure openai key>"
dotnet user-secrets set "AzureADSettings:ClientId" "<client id from output of appregistrations.sh>"
dotnet user-secrets set "AzureADSettings:ClientSecret" "<your client secret create for above client id>"
dotnet user-secrets set "AzureADSettings:TenantId" "<your tenant id>"
dotnet user-secrets set "AzureADSettings:Scopes" "<scope from output for PurchaseAgent of appregistrations.sh>"
dotnet user-secrets set "AzureADSettings:APIScopes" "<apiscope from output for PurchaseAgent of appregistrations.sh>"

```

### 7. Verify the Setup

After the script completes, you can verify the app registrations and permissions in the Azure portal:

1. Go to the Azure portal.
1. Navigate to "Azure Active Directory" > "App registrations".
1. Verify that PurchaseAPI and PurchaseAgent are listed.
1. Check the "API permissions" for PurchaseAgent to ensure products.prices, products.purchase, and User.Read are granted.

### Run the application

Open the application [AgentMandate.sln](./AgentMandate.sln) in Visual Studio 2022. Ensure that the projects PurchaseAPI and sk_agent are set as start-up project. This can be done via the properties of the solution. Press F5.

### Troubleshooting

If you encounter any issues during the setup, ensure that:

1. You are logged into the correct Azure account.
1. Your Azure CLI is up to date.
1. You have the necessary permissions to create app registrations and grant admin consent.

For further assistance, refer to the Azure CLI documentation or contact your Azure administrator.

License
This project is licensed under the MIT License.  
