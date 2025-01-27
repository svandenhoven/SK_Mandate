#!/bin/bash

# Variables
tenantId="your-tenant-id"
subscriptionId="your-subscription-id"
appRegApiName="PurchaseAgentAPI"
appRegClientName="PurchaseAgent"

# Login to Azure
az login

# Set the subscription
az account set --subscription $subscriptionId

# Create the PurchaseAPI app registration
purchaseApiObjectId=$(az ad app create --display-name "$appRegApiName" --query id -o tsv)

# Retrieve the object ID of the PurchaseAPI app registration
purchaseApiAppId=$(az ad app list --filter "id eq '$purchaseApiObjectId'" --query "[0].appId" -o tsv)

# Set the Application ID URI
az ad app update --id $purchaseApiObjectId --identifier-uris "api://$purchaseApiAppId"

echo "PurchaseAPI App Registration created with App ID: $purchaseApiAppId and Application ID URI: api://$purchaseApiAppId"

# Create a service principal for the PurchaseAPI app
az ad sp create --id $purchaseApiAppId

echo "Service principal for PurchaseAPI created."

# Generate UUIDs for scopes
productPricesScopeId=$(python -c 'import uuid; print(str(uuid.uuid4()))')
productPurchaseScopeId=$(python -c 'import uuid; print(str(uuid.uuid4()))')
actAsAgentScopeId=$(python -c 'import uuid; print(str(uuid.uuid4()))')
actAsUserScopeId=$(python -c 'import uuid; print(str(uuid.uuid4()))')

echo "UUIDs generated for scopes: $productPricesScopeId and $productPurchaseScopeId and $actAsAgentScopeId and $actAsUserScopeId"

# Add scopes to the "Expose an API" section
az ad app update --id $purchaseApiObjectId --set api=@- <<EOF
{
  "oauth2PermissionScopes": [
    {
      "adminConsentDescription": "Allows the app to access product prices as the signed-in user.",
      "adminConsentDisplayName": "Access product prices",
      "id": "$productPricesScopeId",
      "isEnabled": true,
      "type": "User",
      "userConsentDescription": "Allows you to access product prices.",
      "userConsentDisplayName": "Access product prices",
      "value": "products.prices"
    },
    {
      "adminConsentDescription": "Allows the app to purchase products as the signed-in user.",
      "adminConsentDisplayName": "Purchase products",
      "id": "$productPurchaseScopeId",
      "isEnabled": true,
      "type": "User",
      "userConsentDescription": "Allows you to purchase products.",
      "userConsentDisplayName": "Purchase products",
      "value": "products.purchase"
    },
    {
      "adminConsentDescription": "This scope indicates that the access token is only allowed to act as agent.",
      "adminConsentDisplayName": "act_as_agent",
      "id": "$actAsAgentScopeId",
      "isEnabled": true,
      "type": "User",
      "userConsentDescription": "This scope indicates that the access token is only allowed to act as agent.",
      "userConsentDisplayName": "act_as_agent",
      "value": "act_as_agent"
    },
    {
      "adminConsentDescription": "This scope indicates that the access token is only to act as user.",
      "adminConsentDisplayName": "act_as_user",
      "id": "$actAsUserScopeId",
      "isEnabled": true,
      "type": "User",
      "userConsentDescription": "This scope indicates that the access token is allowed to act as user.",
      "userConsentDisplayName": "act_as_user",
      "value": "act_as_user"
    }
  ]
}
EOF

echo "Scopes products.prices and products.purchase added to the Expose an API section."


### Client App Registration ############################################################################################################
# Create the PurchaseAgent app registration
purchaseAgentAppId=$(az ad app create --display-name "$appRegClientName" --public-client-redirect-uris "http://localhost" --query appId -o tsv)

# Retrieve the object ID of the PurchaseAgent2 app registration
purchaseAgentObjectId=$(az ad app show --id $purchaseAgentAppId --query "id" -o tsv)

# Set the Application ID URI
az ad app update --id $purchaseAgentObjectId --identifier-uris "api://$purchaseAgentAppId"

echo "PurchaseAgent App Registration created with App ID: $purchaseAgentAppId and Application ID URI: api://$purchaseAgentAppId"

# Add app permissions to the PurchaseAPI scopes
productPricesScopeId=$(az ad app show --id $purchaseApiAppId --query "api.oauth2PermissionScopes[?value=='products.prices'].id" -o tsv)
productPurchaseScopeId=$(az ad app show --id $purchaseApiAppId --query "api.oauth2PermissionScopes[?value=='products.purchase'].id" -o tsv)

echo "Product Prices Scope ID: $productPricesScopeId"
echo "Product Purchase Scope ID: $productPurchaseScopeId"

az ad app permission add --id $purchaseAgentObjectId --api $purchaseApiAppId --api-permissions "$productPricesScopeId=Scope"
az ad app permission add --id $purchaseAgentObjectId --api $purchaseApiAppId --api-permissions "$productPurchaseScopeId=Scope"

# Add User.Read permission
az ad app permission add --id $purchaseAgentObjectId --api 00000003-0000-0000-c000-000000000000 --api-permissions e1fe6dd8-ba31-4d61-89e7-88639da4683d=Scope

# Grant admin consent for the permissions
az ad app permission admin-consent --id $purchaseAgentObjectId

echo "App permissions for products.prices and products.purchase added to PurchaseAgent2 and admin consent granted."

# Generate UUID for scope
accessAsUserScopeId=$(python -c 'import uuid; print(str(uuid.uuid4()))')

echo "UUIDs generated for scope: $accessAsUserScopeId"

# Add scopes to the "Expose an API" section
az ad app update --id $purchaseAgentObjectId --set api=@- <<EOF
{
  "oauth2PermissionScopes": [
    {
      "adminConsentDescription": "Allows the app to be access as a user.",
      "adminConsentDisplayName": "Access access as a user.",
      "id": "$accessAsUserScopeId",
      "isEnabled": true,
      "type": "User",
      "userConsentDescription": "Allows the app to be access as a user.",
      "userConsentDisplayName": "Access access as a user.",
      "value": "access_as_user"
    }
  ]
}
EOF

echo "Scope access_as_user added to the Expose an API section."

# Output  details
echo "$appRegApiName App Registration:"
echo "Client ID API: $purchaseApiAppId"S
echo "Tenant ID: $tenantId"
echo "Scopes: products.prices, products.purchase"
echo ""
echo "$appRegClientName App Registration:"
echo "Client Id: $purchaseAgentAppId"
echo "Client Secret: <Please create a for $purchaseAgentAppId in Azure Portal>"
echo "Tenant ID: $tenantId"
echo "Scopes: api://$purchaseAgentAppId/access_as_user"
echo "APIScopes: api://$purchaseApiAppId/products.prices api://$purchaseApiAppId/products.purchase"